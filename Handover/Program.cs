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
    required public List<SendOrder> SendOrders = new();

    /// <summary>
    /// Pending receive orders
    /// </summary>
    required public List<ReceiveOrder> ReceiveOrders = new();

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
                ReceiveOrders[i].Notification.SetCanceled();
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
                SendOrders[i].Notification.SetCanceled();
                SendOrders.RemoveAt(i);
                break;
            }
        }
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
        TaskCompletionSource<bool> notification = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Output.Lock.WaitAsync();
        // Add a new send order to the list
        Output.SendOrders.Add(new SendOrder
        {
            Id = orderId,
            Panel = panel,
            Notification = notification,
        });

        // If there's a receive order waiting for a new send order, wake it up and remove that receive order!
        if (Output.ReceiveOrders.Count > 0)
        {
            Output.ReceiveOrders[0].Notification.SetResult(true);
            Output.ReceiveOrders.RemoveAt(0);
        }
        Output.Lock.Release();

        Task<bool> notificationTask = notification.Task;
        Task sleepTask = Task.Delay(timeout, cancellationToken);

        // Wait until something happens to one of these two tasks
        await Task.WhenAny(notificationTask, sleepTask);

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

        TaskCompletionSource<bool> notification;
        Task<bool>? notificationTask = null;

        Task sleepTask = Task.Delay(timeout, cancellationToken);

        // You might be wondering why the code below is wrapped in a loop.
        // The reason is that in between the notificationTask being triggered and the InputLock being acquired,
        // it's possible that a random TryReceive() call sneaks in and steals the SendOrder originally meant for this task.
        // If this happens, we need to retry - hence the while(true).

        bool firstTime = true; // For the first time, we want to check immediately if there are any available SendOrders
        while (true)
        {
            if (!firstTime) // Skip if first time
            {
                // Wait until something happens to one of these two tasks
                await Task.WhenAny(notificationTask!, sleepTask);
            }

            if (firstTime || notificationTask!.IsCompletedSuccessfully) // If firstTime is false, notificationTask is guaranteed to be instantiated
            {
                firstTime = false;

                // A sender task triggered our notification task and removed our receive order
                await Input.Lock.WaitAsync();

                if (Input.SendOrders.Count > 0) // There's a SendOrder waiting for us
                {
                    // If there's a send order already waiting there, we can immediately return that send order's panel.
                    var res = (true, Input.SendOrders[0].Panel);

                    Input.SendOrders[0].Notification.SetResult(true); // Notify the sender.
                    Input.SendOrders.RemoveAt(0); // Remove the send order.

                    Input.Lock.Release();
                    return res;
                }
                else // There's no send order available for us to take, so let's create a receive order and wait for an update
                {
                    notification = new(TaskCreationOptions.RunContinuationsAsynchronously); // Instantiate a new TaskCompletionSource
                    notificationTask = notification.Task;

                    // Add a new receive order to the list
                    Input.ReceiveOrders.Add(new ReceiveOrder
                    {
                        Id = orderId,
                        Notification = notification
                    });
                    Input.Lock.Release();
                }
            }
            else // The sleep task completed
            {
                await Input.Lock.WaitAsync(); // By holding the lock we ensure that we have exclusive access to our own notificationTask.

                // if somehow the notification task was triggered immediately after the sleep task and there's an available SendOrder...
                if (notificationTask.IsCompletedSuccessfully && Input.SendOrders.Count > 0)
                {
                    var res = (true, Input.SendOrders[0].Panel);

                    Input.SendOrders[0].Notification.SetResult(true); // Notify the sender.
                    Input.SendOrders.RemoveAt(0); // Remove the send order.

                    Input.Lock.Release();
                    return res;
                }
                else // Otherwise, cleanup our pending order (if it's still there) and return false.
                {
                    Input.RemoveReceiveOrder(orderId);
                    Input.Lock.Release();
                    cancellationToken.ThrowIfCancellationRequested(); // Propagate the cancellation if necessary.
                    return (false, null);
                }
            }
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
