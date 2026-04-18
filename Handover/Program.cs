using System.Data.Common;

namespace Handover;

class Panel {}

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
    /// The queue through which the panels are transfered
    /// </summary>
    public ZeroQueue<Panel> Queue = new();
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
    /// Sends the panel to the next machine
    /// </summary>
    public async Task SendAsync(Panel panel)
    {
        if (Output == null)
        {
            throw new NullReferenceException(nameof(Output));
        }
        await Output.Queue.TrySendAsync(panel, Timeout.InfiniteTimeSpan, CancellationToken.None);
    }

    /// <summary>
    /// Receives a panel from the next machine
    /// </summary>
    public async Task<Panel> ReceiveAsync()
    {
        if (Input == null)
        {
            throw new NullReferenceException(nameof(Input));
        }
        return (await Input.Queue.TryReceiveAsync(Timeout.InfiniteTimeSpan, CancellationToken.None)).Panel!;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
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
            taskList.Add(machineB.ReceiveAsync());
            taskList.Add(machineA.SendAsync(new()));
        }

        await Task.WhenAll(taskList);
    }
}
