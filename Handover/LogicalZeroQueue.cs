/// <summary>
/// An asynchronous, zero-capacity, multi-producer multi-consumer (MPMC) rendezvous queue with support for logical queues.
///
/// <para>
/// A logical queue represents an independent synchronization domain within the
/// <see cref="ZeroQueue{T,TKey}"/>. Send and receive operations associated with this
/// key will only be matched with each other and are isolated from operations on
/// other keys.
/// </para>
/// 
/// <para>
/// <see cref="ZeroQueue{T, TKey}"/> models a synchronous handover point between independent producers
/// and consumers, where no buffering is allowed: a send operation can only complete if a
/// corresponding receive operation with the same key is ready, and vice versa.
/// 

/// Internally, the queue is implemented as a pair of pending "send" and "receive" orders,
/// protected by an asynchronous mutex. A send and a receive are matched atomically, at which
/// point the payload is transferred and both awaiting operations complete successfully.
/// </para>
///
/// More information about the inner workings of this data structure can be found in this blog post:
/// https://dfamonteiro.com/posts/perfect-handover/.
/// </summary>
/// <typeparam name="T">
/// The type of entity being transferred between producers and consumers.
/// </typeparam>
/// <typeparam name="TKey">
/// The type of the key used to select a specific logical queue (or synchronization domain).
/// Send and receive operations are only matched when their keys are equal according to the
/// key's equality semantics.
/// </typeparam>
public class ZeroQueue<T, TKey> where TKey : notnull, IEquatable<TKey>
{
    /// <summary>
    /// Pending send orders.
    /// </summary>
    private List<SendOrder<T, TKey>> SendOrders = new();

    /// <summary>
    /// Pending receive orders.
    /// </summary>
    private List<ReceiveOrder<TKey>> ReceiveOrders = new();

    /// <summary>
    /// Maps the keys of the logical queues to their number of pending send and receive orders.
    /// </summary>
    private readonly Dictionary<TKey, OrderCounters> LogicalQueues = new();

    /// <summary>
    /// Mutex that protects accesses to the <see cref="SendOrders"/>, <see cref="ReceiveOrders"/> and <see cref="LogicalQueues"/> fields.
    /// </summary>
    private readonly SemaphoreSlim QueueLock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Remove order from <see cref="ReceiveOrders"/> and cancel the associated Task, if it exists.
    /// This function must only be called if <see cref="QueueLock"/> is acquired.
    /// </summary>
    private void RemoveReceiveOrder(Guid guid, TKey key)
    {
        for (int i = 0; i < ReceiveOrders.Count; i++)
        {
            if (ReceiveOrders[i].Id == guid && ReceiveOrders[i].Key.Equals(key))
            {
                // Cancel the notification task. Doing this is important to avoid having "zombie" tasks filling our memory.
                ReceiveOrders[i].Notification.SetCanceled(); // No one should be awaiting this Task, so calling SetCancelled() should pose no problem.
                ReceiveOrders.RemoveAt(i);
                LogicalQueues[key].ReceiveOrderCount--;
                return;
            }
        }
        // If no match found, throw exception
        throw new Exception($"No Receive Order with id \"{guid}\" and key \"{key}\" was found.");
    }

    /// <summary>
    /// Remove order from <see cref="SendOrders"/> and cancel the associated Task, if it exists.
    /// This function must only be called if <see cref="QueueLock"/> is acquired.
    /// </summary>
    private void RemoveSendOrder(Guid guid, TKey key)
    {
        for (int i = 0; i < SendOrders.Count; i++)
        {
            if (SendOrders[i].Id == guid && SendOrders[i].Key.Equals(key))
            {
                // Cancel the notification task. Doing this is important to avoid having "zombie" tasks filling our memory.
                SendOrders[i].Notification.SetCanceled(); // No one should be awaiting this Task, so calling SetCancelled() should pose no problem.
                SendOrders.RemoveAt(i);
                LogicalQueues[key].SendOrderCount--;
                return;
            }
        }
        // If no match found, throw exception
        throw new Exception($"No Send Order with id \"{guid}\" and key \"{key}\" was found.");
    }

    /// <summary>
    /// Returns the index of the first send order that matches the reservedReceiverId.
    /// Returns null if no match is found.
    /// </summary>
    private int? FindSendOrderByReservedId(Guid? reservedReceiverId, TKey key)
    {
        for (int i = 0; i < SendOrders.Count; i++)
        {
            if (SendOrders[i].ReservedReceiverId == reservedReceiverId && SendOrders[i].Key.Equals(key))
            {
                return i;
            }
        }
        // Fallback
        return null;
    }

    /// <summary>
    /// Returns the index of the first send order that matches the logical queue key.
    /// Returns null if no match is found.
    /// </summary>
    private int? FindReceiveOrderByKey(TKey key)
    {
        for (int i = 0; i < ReceiveOrders.Count; i++)
        {
            if (ReceiveOrders[i].Key.Equals(key))
            {
                return i;
            }
        }
        // Fallback
        return null;
    }

    /// <summary>
    /// Throws an exception if <paramref name="key"/> does not exist in  <see cref="LogicalQueues"/>.
    /// </summary>
    private void ValidateKeyExists(TKey key)
    {
        if (!LogicalQueues.ContainsKey(key))
        {
            throw new KeyNotFoundException($"Logical queue with key \"{key}\" does not exist.");
        }
    }

    /// <summary>
    /// Returns the number of pending send orders for the given logical queue key.
    /// </summary>
    public int GetSendOrderCount(TKey key)
    {
        ValidateKeyExists(key);
        return LogicalQueues[key].SendOrderCount;
    }

    /// <summary>
    /// Returns the number of pending receive orders for the given logical queue key.
    /// </summary>
    public int GetReceiveOrderCount(TKey key)
    {
        ValidateKeyExists(key);
        return LogicalQueues[key].ReceiveOrderCount;
    }

    /// <summary>
    /// Creates a new logical queue associated with the specified key.
    /// </summary>
    /// <param name="key">
    /// The key that uniquely identifies the logical queue to be created.
    /// </param>
    public void CreateLogicalQueue(TKey key)
    {
        if (LogicalQueues.ContainsKey(key))
        {
            throw new Exception($"Logical queue with key \"{key}\" already exists.");
        }
        LogicalQueues[key] = new OrderCounters
        {
            ReceiveOrderCount = 0,
            SendOrderCount = 0,
        };
    }

    /// <summary>
    /// Attempts to send a <paramref name="entity"/> object to the next machine.
    /// Will only succeed if the machine on the other side is ready to receive the <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The object to be sent.</param>
    /// <param name="key">The key of the logical queue.</param>
    /// <param name="timeout">Timeout for the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the handover was completed, false if the handover was unsuccessful.</returns>
    public async Task<bool> TrySendAsync(T entity, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ValidateKeyExists(key);

        Guid orderId = Guid.NewGuid();
        Guid? receiverId = null;
        TaskCompletionSource<bool> notification = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await QueueLock.WaitAsync();
        
        // If there's a receive order waiting for a new send order, wake it up and remove that receive order!
        int? matchIndex = FindReceiveOrderByKey(key); // Find first receive order with our key
        if (matchIndex != null)
        {
            receiverId = ReceiveOrders[(int)matchIndex].Id;
            ReceiveOrders[(int)matchIndex].Notification.SetResult(true);
            ReceiveOrders.RemoveAt((int)matchIndex);
            LogicalQueues[key].ReceiveOrderCount--; // Update the receive order count.
        }

        // Add a new send order to the list
        SendOrders.Add(new SendOrder<T, TKey>
        {
            Id = orderId,
            Key = key,
            ReservedReceiverId = receiverId,
            Entity = entity,
            Notification = notification,
        });
        LogicalQueues[key].SendOrderCount++; // Update the send order count.
        QueueLock.Release();

        // By creating this cancelSleep cancellation token source and linking it to our cancellationToken argument, 
        // we're able to to force the cancellation of the sleepTask task.
        CancellationTokenSource cancelSleep = new();
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelSleep.Token);

        // If receiverId != null, we know that we've woken up a receiver task that will be looking for our send order.
        //
        // In this scenario, we need to avoid a potential situation where the sleepTask triggers before the notificationTask,
        // which would lead to the unintended removal of our SendOrder... which the receiver task expects to exist.
        //
        // We avoid this problem by setting the timeout to infinite.
        Task sleepTask = Task.Delay(receiverId == null ? timeout : Timeout.InfiniteTimeSpan, combinedCts.Token);

        // Wait until something happens to one of these two tasks
        await Task.WhenAny(notification.Task, sleepTask);

        // Cancel the sleep task, if it's not already finished
        // We have to do this to avoid having a memory leak (this sleepTask is not garbage-collected when we exit the function).
        cancelSleep.Cancel();

        if (notification.Task.IsCompletedSuccessfully)
        {
            // A receiver task triggered our notification task, removed our send order, and and returned the entity.
            // The entity has been handed over successfully, we're done here!
            return true;
        }
        else // The sleep task completed
        {
            await QueueLock.WaitAsync(); // By holding the lock we ensure that we have exclusive access to our own notificationTask.

            if (notification.Task.IsCompletedSuccessfully) // The notification task was triggered immediately after the sleep task.
            {
                // A receiver task triggered our notification task, removed our send order, and and returned the entity.
                // The entity has been handed over successfully, we're done here!
                QueueLock.Release();
                return true;
            }
            else
            {
                // The notification task was not triggered, which means that our send order still needs to be cleaned up.
                RemoveSendOrder(orderId, key);
                QueueLock.Release();
                cancellationToken.ThrowIfCancellationRequested(); // Propagate the cancellation if necessary.
                return false;
            }
        }
    }


    /// <summary>
    /// Attempts to receive a <typeparamref name="T"/> object from the previous machine.
    /// Will only succeed if the machine on the other side is ready to send a <typeparamref name="T"/> object.
    /// </summary>
    /// <param name="key">The key of the logical queue.</param>
    /// <param name="timeout">Timeout for the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True &amp; the entity if the handover is successful, false if a timeout or cancellation is triggered.</returns>
    public async Task<(bool Success, T? Entity)> TryReceiveAsync(TKey key, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ValidateKeyExists(key);

        Guid orderId = Guid.NewGuid();
        TaskCompletionSource<bool> notification = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await QueueLock.WaitAsync();
        int? matchIndex = FindSendOrderByReservedId(null, key); // Looking for a SendOrder with a null ReservedReceiverId
        if (matchIndex != null) // There's an unmatched SendOrder waiting for us
        {
            // If there's a send order already waiting there, we can immediately return that send order's object.
            var res = (true, SendOrders[(int)matchIndex].Entity);

            SendOrders[(int)matchIndex].Notification.SetResult(true); // Notify the sender.
            SendOrders.RemoveAt((int)matchIndex); // Remove the send order.
            LogicalQueues[key].SendOrderCount--; // Update the send order count.

            QueueLock.Release();
            return res;
        }
        else // There's no send order available for us to take, so let's create a receive order and wait for an update
        {
            // Add a new receive order to the list
            ReceiveOrders.Add(new ReceiveOrder<TKey>
            {
                Id = orderId,
                Key = key,
                Notification = notification
            });
            LogicalQueues[key].ReceiveOrderCount++; // Update the receive order count.
            QueueLock.Release();
        }

        // By creating this cancelSleep cancellation token source and linking it to our cancellationToken argument, 
        // we're able to to force the cancellation of the sleepTask task.
        CancellationTokenSource cancelSleep = new();
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelSleep.Token);

        Task sleepTask = Task.Delay(timeout, combinedCts.Token);

        // Wait until something happens to one of these two tasks
        await Task.WhenAny(notification.Task, sleepTask);

        // Cancel the sleep task, if it's not already finished
        // We have to do this to avoid having a memory leak (this sleepTask is not garbage-collected when we exit the function).
        cancelSleep.Cancel();

        // By holding the lock we ensure that we have exclusive access to our own notificationTask.
        await QueueLock.WaitAsync();

        if (notification.Task.IsCompletedSuccessfully)
        {
            // We're guaranteed to have a SendOrder reserved for us
            int sendOrderIndex = (int)FindSendOrderByReservedId(orderId, key)!;

            var res = (true, SendOrders[sendOrderIndex].Entity);

            SendOrders[sendOrderIndex].Notification.SetResult(true); // Notify the sender.
            SendOrders.RemoveAt(sendOrderIndex); // Remove the send order.
            LogicalQueues[key].SendOrderCount--; // Update the send order count.

            QueueLock.Release();

            return res;
        }
        else // The sleep task completed
        {
            RemoveReceiveOrder(orderId, key);
            QueueLock.Release();
            cancellationToken.ThrowIfCancellationRequested(); // Propagate the cancellation if necessary.
            return (false, default);
        }
    }
}

/// <summary>
/// Helper class that holds the number of send orders, and the number of receive orders
/// for a given logical queue key.
/// </summary>
class OrderCounters
{
    /// <summary>
    /// Number of pending send orders
    /// </summary>
    public int SendOrderCount;

    /// <summary>
    /// Number of pending receive orders.
    /// </summary>
    public int ReceiveOrderCount;
}