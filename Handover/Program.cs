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
