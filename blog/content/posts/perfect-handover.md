+++ 
draft = true
date = 2026-04-04T00:59:14+01:00
title = "Crafting a Zero-Capacity Multi-Producer-Multi-Consumer queue in async C#"
description = ""
slug = ""
authors = ["Daniel Monteiro"]
tags = ["Programming"]
categories = []
externalLink = ""
series = []
+++

In recent times I have been putting some thought into designing a load generator that simulates queued-based manufacturing processes - think car [assembly lines](https://en.wikipedia.org/wiki/Assembly_line) or [SMT lines](https://en.wikipedia.org/wiki/Surface-mount_technology). A key detail about these types of setups is that they're susceptible to traffic jams: if a station at the end of the line breaks, the entire line is bottlenecked until the problem is fixed.

The intrinsically linear nature of these manufacturing setups creates dependencies between the materials in the line: panel #345436 can only enter the next machine if panel #345437 has finished processing, which in turn needs panel #345438 to clear the conveyor belt... you get the point.

Simulating these lines with a [stateful load generator approach](../stateful-load-generators) will not work because it handles each panel independently - one would need to layer an obscene amount of synchronization code[^1] on top to make the panels behave in a way that somewhat resembles a real-world SMT line. It's just not feasible.

[^1]: Think mutexes, semaphores, etc.

We need a new approach.

## The case for a machine-centered load generator

Instead of having a load generator that is focused on the panels, we should have a load generator that is focused on the machines that handle the panels. Each resource would then be handled by its own dedicated thread which would be tasked with simulating the machine's behaviours: receiving new panels, "processing" them, and sending them to the next machine.[^2] With this approach, each machine can independently enforce its own constraints by refusing to perform any given operation if its internal conditions are not met.

[^2]: You can think of this as a manufacturing simulation-oriented [Actor Model](https://en.wikipedia.org/wiki/Actor_model).

This is a good idea in principle... as long as we figure out how we are going to pass materials. In other words, how should a _handover_ of a panel from resource A to resource B occur?

## The handover problem statement

Given two machines A and B running on independent threads, if the following conditions are met:

1. Machine A wishes to send a material by calling `send()`.
2. Machine B wishes to receive a material by calling `receive()`.

The panel should then be transferred to Machine B, and both `send()` and `receive()` should return `true` when the transfer is completed.

The functions `send()` and `receive()` should also be [Atomic](https://en.wikipedia.org/wiki/Atomicity_(database_systems)), [Thread-safe](https://en.wikipedia.org/wiki/Thread_safety) and able to support timeouts.

### Some reflections

Our handover challenge is fundamentally a synchronization problem involving two independent threads - we should take inspiration from the field of concurrent programming and define a wishlist of properties that our conceptual data structure will need to have:

- Is a zero-capacity queue (i.e. the sender blocks until the receiver is ready to receive).
- Supports multiple producers and multiple consumers (i.e. thread-safe).
- Supports timeouts.
- Is written in async C# (because, well... I need it to be written in async C#).

## The solution: an [order book](https://en.wikipedia.org/wiki/Order_book) protected by a mutex

What does it mean when Machine A calls `send()`? Does it mean that there's a 100% guarantee that he panel will be sent? No. It means that Machine A **_is interested_** in sending the panel, and if there's matching interest from the other side, a trade will happen... hold on, is this a stock market? Our `send()` calls are the equivalent of sell orders and our `receive()` calls represent buy orders!

Whenever there's an available `receive()` order and an available `send()` order, they are matched, removed from the "order book" and the panel is transferred.

The remainder of this blog post will focus on implementing this data structure, which I will name `ZeroQueue`:

### The core data structures

Our `ZeroQueue` class will have three properties: the **Send Orders**, the **Receive Orders**, and a mutex that guards the access to the 2 previously mentioned properties:

```csharp
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
    /// Mutex that protects accesses to the SendOrders and ReceiveOrders fields.
    /// </summary>
    private readonly SemaphoreSlim QueueLock = new SemaphoreSlim(1, 1);

    // ...
}
```

The `SendOrder` and `ReceiveOrder` structs have the following fields:

```csharp
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
    /// If set, it means that this Send Order is reserved
    /// for the Receiver Task whose Id equals ReservedReceiverId.
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
```

```csharp
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
```

These two structs are pretty self-explanatory, with perhaps the exception of the `SendOrder`'s `ReservedReceiverId` field: the reason for this field's existence is to prevent a race condition.[^3]

[^3]: I will explain the race condition later in this blog post.

### Utility methods

To keep the main algorithms as lean as possible, I wrote these small utility functions that help us manipulate `SendOrders` and `ReceiveOrders`:

```csharp
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
        throw new Exception($"No Receive Order with id {guid} was found.");
    }
```

```csharp
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
        throw new Exception($"No Send Order with id {guid} was found.");
    }
```

Please note that in the two tasks above, we're cancelling the `Notification` task completion source to prevent leaking tasks to memory. It's entirely possible that the GC will perform this cleanup for us, but I'm not taking any chances here.

```csharp
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
```

### The **TrySendAsync()** method

The logic behind this method goes as follows:

1. Acquire the `QueueLock` and do the following:
    - If there's a `ReceiveOrder` available, trigger its notification mechanism, take note of its `Id` and remove it from the `ReceiveOrders` list.
    - Create our `SendOrder`.
        - If we found a `ReceiveOrder` in the previous step, set our `ReservedReceiverId` to the `Id` of that `ReceiveOrder`.

```csharp
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
```

2. Wait until either the notification of our `SendOrder` is triggered, or the timeout is triggered.
    - If there was a `ReceiveOrder` available and we triggered its notification **we will disregard the timeout** - the reason for this is that we know that the "receiver task" has been awoken and is expecting our `SendOrder` to be present. We can only guarantee that our `SendOrder` will be there if we ensure that a timeout **will not be triggered** _before_ the "receiver task" has fetched our `SendOrder`. We do this by setting the timeout to infinite.

```csharp
        // We're creating this combinedCts so that we can terminate the sleepTask
        CancellationTokenSource cancelSleep = new();
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelSleep.Token);

        Task<bool> notificationTask = notification.Task;

        // If receiverId != null, we know that we've woken up a receiver task that will be looking for our send order.
        //
        // In this scenario, we need to avoid a potential situation where the sleepTask triggers before the notificationTask,
        // which would lead to the unintended removal of our SendOrder... which the receiver task expects to exist.
        //
        // We avoid this problem by setting the timeout to infinite.
        Task sleepTask = Task.Delay(receiverId == null ? timeout : Timeout.InfiniteTimeSpan, combinedCts.Token);

        // Wait until something happens to one of these two tasks
        await Task.WhenAny(notificationTask, sleepTask);

        // Cancel the sleep task, if it's not already finished
        // We have to do this to avoid having a memory leak (this sleepTask is not garbage-collected when we exit the function).
        cancelSleep.Cancel();
```

3. Check the status of our notification
    - If our notification triggered, return true - the panel was transferred successfully.
    - Otherwise, acquire the `QueueLock` and do the following:
        - Check again if our notification triggered (we perform this check twice to prevent a race condition).
        - If it wasn't triggered, that means that the timeout was triggered instead. Therefore, we should remove our `SendOrder` and return false.

```csharp
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
```

### The **TryReceiveAsync()** method

This method will naturally be the counterpart of the [`TrySendAsync()` method](#the-trysendasync-method). This is how it works:

1. Acquire the `QueueLock` and do the following:
    - If there's a `SendOrder` with `ReservedReceiverId` set to `null` available, trigger its notification mechanism, remove it from the `SendOrders` list and return the panel from the `SendOrder`. We're done here!
    - Otherwise, create our `ReceiveOrder`.

```csharp
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
```

2. Wait until either the notification of our `ReceiveOrder` is triggered, or the timeout is triggered.

```csharp
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
```

3. Acquire the `QueueLock` and do the following:
    - If our notification triggered, that means there's a `SendOrder` reserved for us - we need to find it, trigger its notification mechanism, remove it from the `SendOrders` list and return the panel from the `SendOrder`!
    - Otherwise, that means that the timeout was triggered instead. Therefore, we should remove our `ReceiveOrder` and return false.

```csharp
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
```

## Convicing yourself that the code actually works

Writing multithreaded code is not that hard - what's difficult is making sure that the code actually works under all circumstances! This requires you to think of every possible deadlock scenario, every possible race condition and ensuring that they are all accounted for.[^4]

[^4]: There are formal methods for validating concurrent algorithms, but in this blog post we will be taking a much more informal approach to making sure that our algorithm works.
