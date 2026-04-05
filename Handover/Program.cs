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
}

class Machine
{
    /// <summary>
    /// Input of the machine
    /// </summary>
    public Link Input;
    
    /// <summary>
    /// Output of the machine
    /// </summary>
    public Link Output;

    /// <summary>
    /// Mutex that protects accesses to the <see cref="Inputs"/> field.
    /// </summary>
    private readonly SemaphoreSlim InputLock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Attempts to send the panel to the next machine.
    /// </summary>
    /// <returns>true if the handover of the panel is successful, false if a timeout or cancellation is triggered.</returns>
    public async Task<bool> TrySend(Panel panel, TimeSpan timeout, CancellationToken cancellationToken)
    {
        Guid orderId = Guid.NewGuid();
        TaskCompletionSource<bool> notification = new();

        await Output.Receiver.InputLock.WaitAsync();
        // Add a new send order to the list
        Output.SendOrders.Add(new SendOrder
        {
            Id = orderId,
            Panel = panel,
            Notification = notification,
        });

        // If there's a receive order waiting for a new send order, wake it up and remove that send order!
        if (Output.ReceiveOrders.Count > 0)
        {
            Output.ReceiveOrders[0].Notification.SetResult(true);
            Output.ReceiveOrders.RemoveAt(0);
        }
        Output.Receiver.InputLock.Release();

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
            await Output.Receiver.InputLock.WaitAsync(); // By holding the lock we ensure that we have exclusive access to our own notificationTask.
            
            if (notificationTask.IsCompletedSuccessfully) // The notification task was triggered immediately after the sleep task.
            {
                // A receiver task triggered our notification task, removed our send order, and and returned the panel.
                // The panel has been handed over successfully, we're done here!
                Output.Receiver.InputLock.Release();
                return true;
            }
            else
            {
                // The notification task was not triggered, which means that our send order still needs to be cleaned up.
                for (int i = 0; i < Output.SendOrders.Count; i++)
                {
                    if (Output.SendOrders[i].Id == orderId)
                    {
                        // Cancel the notification task. Doing this is important to avoid having "zombie" tasks filling our memory.
                        Output.SendOrders[i].Notification.SetCanceled(); 
                        Output.SendOrders.RemoveAt(i);
                        break;
                    }
                }
                Output.Receiver.InputLock.Release();
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
        Guid orderId = Guid.NewGuid();

        TaskCompletionSource<bool> notification;
        Task<bool> notificationTask;

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
                await Task.WhenAny(notificationTask, sleepTask);
            }

            if (notificationTask.IsCompletedSuccessfully || firstTime)
            {
                firstTime = false;

                // A receiver task triggered our notification task and removed our send order
                await Input.InputLock.WaitAsync();

                if (Input.SendOrders.Count > 0) // There's a SendOrder waiting for us
                {
                    // If there's a send order already waiting there, we can immediately return that send order's panel.
                    var res = (true, Input.SendOrders[0].Panel);

                    Input.SendOrders[0].Notification.SetResult(true); // Notify the sender.
                    Input.SendOrders.RemoveAt(0); // Remove the send order.

                    Input.InputLock.Release();
                    return res;
                }
                else // There's no send order available for us to take, so let's create a receive order and wait for an update
                {
                    notification = new(); // Instantiate a new TaskCompletionSource
                    notificationTask = notification.Task;

                    // Add a new receive order to the list
                    Input.ReceiveOrders.Add(new ReceiveOrder
                    {
                        Id = orderId,
                        Notification = notification
                    });
                    Input.InputLock.Release();
                }
            }
            else // The sleep task completed
            {
                await Input.InputLock.WaitAsync(); // By holding the lock we ensure that we have exclusive access to our own notificationTask.

                // if somehow the notification task was triggered immediately after the sleep task and there's an available SendOrder...
                if (notificationTask.IsCompletedSuccessfully && Input.SendOrders.Count > 0)
                {
                    var res = (true, Input.SendOrders[0].Panel);

                    Input.SendOrders[0].Notification.SetResult(true); // Notify the sender.
                    Input.SendOrders.RemoveAt(0); // Remove the send order.

                    Input.InputLock.Release();
                    return res;
                }
                else // Otherwise, cleanup our pending order (if it's still there) and return false.
                {
                    for (int i = 0; i < Input.ReceiveOrders.Count; i++)
                    {
                        if (Input.ReceiveOrders[i].Id == orderId)
                        {
                            // Cancel the notification task. Doing this is important to avoid having "zombie" tasks filling our memory.
                            Input.ReceiveOrders[i].Notification.SetCanceled(); 
                            Input.ReceiveOrders.RemoveAt(i);
                            break;
                        }
                    }
                    Input.InputLock.Release();
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
