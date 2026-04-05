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
        Input.SendOrders.Add(new SendOrder
        {
            Id = orderId,
            Panel = panel,
            Notification = notification,
        });

        // If there's a receive order waiting for a new send order, wake it up and remove that send order!
        if (Input.ReceiveOrders.Count > 0)
        {
            Input.ReceiveOrders[0].Notification.SetResult(true);
            Input.ReceiveOrders.RemoveAt(0);
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
                for (int i = 0; i < Input.SendOrders.Count; i++)
                {
                    if (Input.SendOrders[i].Id == orderId)
                    {
                        // Cancel the notification task. Doing this is important to avoid having "zombie" tasks filling our memory.
                        Input.SendOrders[i].Notification.SetCanceled(); 
                        Input.SendOrders.RemoveAt(i);
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
        return new();
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
