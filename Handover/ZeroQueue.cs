
namespace Handover;


/// <summary>
/// Represents a Task's intention of sending a Panel
/// </summary>
struct SendOrder<T>
{
    /// <summary>
    /// Unique Id for this order
    /// </summary>
    required public Guid Id;

    /// <summary>
    /// If set, it means that this Send Order is reserved for the Receiver Task whose Id equals ReservedReceiverId.
    /// </summary>
    required public Guid? ReservedReceiverId;

    /// <summary>
    /// The panel to be sent to the next machine.
    /// </summary>
    required public T Entity;

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

class ZeroQueue<T>
{
    /// <summary>
    /// Pending send orders
    /// </summary>
    private List<SendOrder<T>> SendOrders = new();

    /// <summary>
    /// Pending receive orders
    /// </summary>
    private List<ReceiveOrder> ReceiveOrders = new();
    
    /// <summary>
    /// Mutex that protects accesses to the <see cref="SendOrders"/> and <see cref="ReceiveOrders"/> fields.
    /// </summary>
    private readonly SemaphoreSlim QueueLock = new SemaphoreSlim(1, 1);

    
    /// <summary>
    /// Remove order from <see cref="ReceiveOrders"/> and cancel the associated Task, if it exists.
    /// This function must only be called if <see cref="QueueLock"/> is acquired.
    /// </summary>
    private void RemoveReceiveOrder(Guid guid)
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
        // If no match found, throw exception
        throw new Exception($"No Receive Order with id \"{guid}\" was found.");
    }

    /// <summary>
    /// Remove order from <see cref="SendOrders"/> and cancel the associated Task, if it exists.
    /// This function must only be called if <see cref="QueueLock"/> is acquired.
    /// </summary>
    private void RemoveSendOrder(Guid guid)
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
        // If no match found, throw exception
        throw new Exception($"No Send Order with id \"{guid}\" was found.");
    }

    /// <summary>
    /// Returns the index of the first send order that matches the reservedReceiverId.
    /// Returns null if no match is found.
    /// </summary>
    private int? FindSendOrderByReservedId(Guid? reservedReceiverId)
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

    
    /// <summary>
    /// Attempts to send the panel to the next machine.
    /// </summary>
    /// <returns>true if the handover of the panel is successful, false if a timeout or cancellation is triggered.</returns>
    public async Task<bool> TrySendAsync(T panel, TimeSpan timeout, CancellationToken cancellationToken)
    {
        Guid orderId = Guid.NewGuid();
        Guid? receiverId = null;
        TaskCompletionSource<bool> notification = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await QueueLock.WaitAsync();
        // If there's a receive order waiting for a new send order, wake it up and remove that receive order!
        if (ReceiveOrders.Count > 0)
        {
            receiverId = ReceiveOrders[0].Id;
            ReceiveOrders[0].Notification.SetResult(true);
            ReceiveOrders.RemoveAt(0);
        }

        // Add a new send order to the list
        SendOrders.Add(new SendOrder<T>
        {
            Id = orderId,
            ReservedReceiverId = receiverId,
            Entity = panel,
            Notification = notification,
        });
        QueueLock.Release();

        // We're creating this combinedCts so that we can terminate the sleepTask
        CancellationTokenSource cancelSleep = new();
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelSleep.Token);

        Task<bool> notificationTask = notification.Task;
        Task sleepTask = Task.Delay(timeout, combinedCts.Token);

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

        // Cancel the sleep task, if it's not already finished
        // We have to do this to avoid having a memory leak (this sleepTask is not garbage-collected when we exit the function).
        cancelSleep.Cancel();

        if (notificationTask.IsCompletedSuccessfully)
        {
            // A receiver task triggered our notification task, removed our send order, and and returned the panel.
            // The panel has been handed over successfully, we're done here!
            return true;
        }
        else // The sleep task completed
        {
            await QueueLock.WaitAsync(); // By holding the lock we ensure that we have exclusive access to our own notificationTask.
            
            if (notificationTask.IsCompletedSuccessfully) // The notification task was triggered immediately after the sleep task.
            {
                // A receiver task triggered our notification task, removed our send order, and and returned the panel.
                // The panel has been handed over successfully, we're done here!
                QueueLock.Release();
                return true;
            }
            else
            {
                // The notification task was not triggered, which means that our send order still needs to be cleaned up.
                RemoveSendOrder(orderId);
                QueueLock.Release();
                cancellationToken.ThrowIfCancellationRequested(); // Propagate the cancellation if necessary.
                return false;
            }
        }
    }

    
    /// <summary>
    /// Attempts to receive a panel from the previous machine.
    /// </summary>
    /// <returns>true & the panel if the handover is successful, false if a timeout or cancellation is triggered.</returns>
    public async Task<(bool Success, T? Panel)> TryReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        Guid orderId = Guid.NewGuid();
        TaskCompletionSource<bool> notification = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await QueueLock.WaitAsync();
        int? matchIndex = FindSendOrderByReservedId(null);
        if (matchIndex != null) // There's an unmatched SendOrder waiting for us
        {
            // If there's a send order already waiting there, we can immediately return that send order's panel.
            var res = (true, SendOrders[(int)matchIndex].Entity);

            SendOrders[(int)matchIndex].Notification.SetResult(true); // Notify the sender.
            SendOrders.RemoveAt((int)matchIndex); // Remove the send order.

            QueueLock.Release();
            return res;
        }
        else // There's no send order available for us to take, so let's create a receive order and wait for an update
        {
            // Add a new receive order to the list
            ReceiveOrders.Add(new ReceiveOrder
            {
                Id = orderId,
                Notification = notification
            });
            QueueLock.Release();
        }

        // We're creating this combinedCts so that we can terminate the sleepTask
        CancellationTokenSource cancelSleep = new();
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelSleep.Token);

        Task<bool> notificationTask = notification.Task;
        Task sleepTask = Task.Delay(timeout, combinedCts.Token);
        
        // Wait until something happens to one of these two tasks
        await Task.WhenAny(notificationTask, sleepTask);

        // Cancel the sleep task, if it's not already finished
        // We have to do this to avoid having a memory leak (this sleepTask is not garbage-collected when we exit the function).
        cancelSleep.Cancel();

        // By holding the lock we ensure that we have exclusive access to our own notificationTask.
        await QueueLock.WaitAsync(); 

        if (notificationTask.IsCompletedSuccessfully)
        {
            // We're guaranteed to have a SendOrder reserved for us
            int sendOrderIndex = (int)FindSendOrderByReservedId(orderId)!;

            var res = (true, SendOrders[sendOrderIndex].Entity);

            SendOrders[sendOrderIndex].Notification.SetResult(true); // Notify the sender.
            SendOrders.RemoveAt(sendOrderIndex); // Remove the send order.
            QueueLock.Release();

            return res;
        }
        else // The sleep task completed
        {
            RemoveReceiveOrder(orderId);
            QueueLock.Release();
            cancellationToken.ThrowIfCancellationRequested(); // Propagate the cancellation if necessary.
            return (false, default);
        }
    }
}