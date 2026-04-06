using System.Data.Common;

namespace Handover;

class Panel {}

/// <summary>
/// Represents a Task's intention of sending a Panel
/// </summary>
struct SendOrder
{
    /// <summary>
    /// Unique Id for this order
    /// </summary>
    required public Guid Id;

    /// <summary>
    /// If set, it means that only the Receiver Task with this Id can match this order.
    /// </summary>
    required public Guid? ReservedReceiverId;

    /// <summary>
    /// The panel to be sent to the next machine.
    /// </summary>
    required public Panel Panel;

    /// <summary>
    /// Signal mechanism to wake up the Task behind this order once a match is found
    /// </summary>
    required public TaskCompletionSource<bool> Notification;
}

/// <summary>
/// Represents a Task's intention of receiving a Panel
/// </summary>
struct ReceiveOrder
{
    /// <summary>
    /// Unique Id for this order
    /// </summary>
    required public Guid Id;

    /// <summary>
    /// Signal mechanism to wake up the Task behind this order once a match is found
    /// </summary>
    required public TaskCompletionSource<bool> Notification;
}

/// <summary>
/// Links a sender machine to a receiver machine
/// </summary>
class Link
{
    /// <summary>
    /// The source machine
    /// </summary>
    required public Machine Sender;

    /// <summary>
    /// The target machine
    /// </summary>
    required public Machine Receiver;

    /// <summary>
    /// Pending sell orders
    /// </summary>
    public List<SendOrder> SendOrders = new();

    /// <summary>
    /// Pending receive orders
    /// </summary>
    public List<ReceiveOrder> ReceiveOrders = new();

    /// <summary>
    /// Mutex that protects accesses to <see cref="SendOrders"/> and <see cref="ReceiveOrders"/> field.
    /// </summary>
    public readonly SemaphoreSlim Lock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Remove order from <see cref="ReceiveOrders"/> and cancel the associated Task, if it exists.
    /// This function must only be called if <see cref="Lock"/> is acquired.
    /// </summary>
    public void RemoveReceiveOrder(Guid guid)
    {
        for (int i = 0; i < ReceiveOrders.Count; i++)
        {
            if (ReceiveOrders[i].Id == guid)
            {
                // Cancel the notification task. Doing this is important to avoid having "zombie" tasks filling our memory.
                ReceiveOrders[i].Notification.SetCanceled(); // No one should be awaiting this Task, so calling SetCancelled() should pose no problem.
                ReceiveOrders.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// Remove order from <see cref="SendOrders"/> and cancel the associated Task, if it exists.
    /// This function must only be called if <see cref="Lock"/> is acquired.
    /// </summary>
    public void RemoveSendOrder(Guid guid)
    {
        for (int i = 0; i < SendOrders.Count; i++)
        {
            if (SendOrders[i].Id == guid)
            {
                // Cancel the notification task. Doing this is important to avoid having "zombie" tasks filling our memory.
                SendOrders[i].Notification.SetCanceled(); // No one should be awaiting this Task, so calling SetCancelled() should pose no problem.
                SendOrders.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// Returns the index of the first send order that matches the reservedReceiverId.
    /// Returns null if no match is found.
    /// </summary>
    public int? FindSendOrderByReservedId(Guid? reservedReceiverId)
    {
        for (int i = 0; i < SendOrders.Count; i++)
        {
            if (SendOrders[i].ReservedReceiverId == reservedReceiverId)
            {
                return i;
            }
        }
        // Fallback
        return null;
    }
}

class Machine
{
    /// <summary>
    /// Input of the machine
    /// </summary>
    public Link? Input = null;
    
    /// <summary>
    /// Output of the machine
    /// </summary>
    public Link? Output = null;

    /// <summary>
    /// Attempts to send the panel to the next machine.
    /// </summary>
    /// <returns>true if the handover of the panel is successful, false if a timeout or cancellation is triggered.</returns>
    public async Task<bool> TrySend(Panel panel, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (Output == null)
        {
            throw new NullReferenceException(nameof(Output));
        }

        Guid orderId = Guid.NewGuid();
        Guid? receiverId = null;
        TaskCompletionSource<bool> notification = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Output.Lock.WaitAsync();
        // If there's a receive order waiting for a new send order, wake it up and remove that receive order!
        if (Output.ReceiveOrders.Count > 0)
        {
            receiverId = Output.ReceiveOrders[0].Id;
            Output.ReceiveOrders[0].Notification.SetResult(true);
            Output.ReceiveOrders.RemoveAt(0);
        }

        // Add a new send order to the list
        Output.SendOrders.Add(new SendOrder
        {
            Id = orderId,
            ReservedReceiverId = receiverId,
            Panel = panel,
            Notification = notification,
        });
        Output.Lock.Release();

        Task<bool> notificationTask = notification.Task;
        Task sleepTask = Task.Delay(timeout, cancellationToken);

        if (receiverId != null)
        {
            // We know that we've woken a receiver task that will be looking for our send order,
            // therefore we can can disregard the timeout.
            // By doing this we also avoid a potential situation where the sleepTask triggers before the notificationTask,
            // which would lead to the unintended removal of our SendOrder... which the receiver task expects to exist.
            await notificationTask;
        }
        else
        {
            // Wait until something happens to one of these two tasks
            await Task.WhenAny(notificationTask, sleepTask);
        }

        if (notificationTask.IsCompletedSuccessfully)
        {
            // A receiver task triggered our notification task, removed our send order, and and returned the panel.
            // The panel has been handed over successfully, we're done here!
            return true;
        }
        else // The sleep task completed
        {
            await Output.Lock.WaitAsync(); // By holding the lock we ensure that we have exclusive access to our own notificationTask.
            
            if (notificationTask.IsCompletedSuccessfully) // The notification task was triggered immediately after the sleep task.
            {
                // A receiver task triggered our notification task, removed our send order, and and returned the panel.
                // The panel has been handed over successfully, we're done here!
                Output.Lock.Release();
                return true;
            }
            else
            {
                // The notification task was not triggered, which means that our send order still needs to be cleaned up.
                Output.RemoveSendOrder(orderId);
                Output.Lock.Release();
                cancellationToken.ThrowIfCancellationRequested(); // Propagate the cancellation if necessary.
                return false;
            }
        }
    }

    /// <summary>
    /// Attempts to receive a panel from the previous machine.
    /// </summary>
    /// <returns>true & the panel if the handover is successful, false if a timeout or cancellation is triggered.</returns>
    public async Task<(bool Success, Panel? Panel)> TryReceive(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (Input == null)
        {
            throw new NullReferenceException(nameof(Input));
        }

        Guid orderId = Guid.NewGuid();
        TaskCompletionSource<bool> notification = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Input.Lock.WaitAsync();
        int? matchIndex = Input.FindSendOrderByReservedId(null);
        if (matchIndex != null) // There's an unmatched SendOrder waiting for us
        {
            // If there's a send order already waiting there, we can immediately return that send order's panel.
            var res = (true, Input.SendOrders[(int)matchIndex].Panel);

            Input.SendOrders[(int)matchIndex].Notification.SetResult(true); // Notify the sender.
            Input.SendOrders.RemoveAt((int)matchIndex); // Remove the send order.

            Input.Lock.Release();
            return res;
        }
        else // There's no send order available for us to take, so let's create a receive order and wait for an update
        {
            // Add a new receive order to the list
            Input.ReceiveOrders.Add(new ReceiveOrder
            {
                Id = orderId,
                Notification = notification
            });
            Input.Lock.Release();
        }

        Task sleepTask = Task.Delay(timeout, cancellationToken);
        Task<bool> notificationTask = notification.Task;
        
        // Wait until something happens to one of these two tasks
        await Task.WhenAny(notificationTask, sleepTask);

        // By holding the lock we ensure that we have exclusive access to our own notificationTask.
        await Input.Lock.WaitAsync(); 

        if (notificationTask.IsCompletedSuccessfully)
        {
            // We're guaranteed to have a SendOrder reserved for us
            int sendOrderIndex = (int)Input.FindSendOrderByReservedId(orderId)!;

            var res = (true, Input.SendOrders[sendOrderIndex].Panel);

            Input.SendOrders[sendOrderIndex].Notification.SetResult(true); // Notify the sender.
            Input.SendOrders.RemoveAt(sendOrderIndex); // Remove the send order.
            Input.Lock.Release();

            return res;
        }
        else // The sleep task completed
        {
            Input.RemoveReceiveOrder(orderId);
            Input.Lock.Release();
            cancellationToken.ThrowIfCancellationRequested(); // Propagate the cancellation if necessary.
            return (false, null);
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();
        Machine machineA = new();
        Machine machineB = new();

        Link link = new()
        {
            Sender   = machineA,
            Receiver = machineB,
        };

        machineA.Output = link;
        machineB.Input  = link;

        List<Task> taskList = new();

        for (int i = 0; i < 100000; i++)
        {
            taskList.Add(machineB.TryReceive(Timeout.InfiniteTimeSpan, cts.Token));
            taskList.Add(machineA.TrySend(new(), Timeout.InfiniteTimeSpan, cts.Token));
        }

        await Task.WhenAll(taskList);
    }
}
