using System;
using System.Threading;
using System.Threading.Tasks;

public static partial class TaskWorkers
{
    public static async Task ServoTask(ServoControllerWorker servo, AsyncCommandQueue<ServoCommand> queue, CancellationToken token)
    {
        Console.WriteLine("[TASK] Servo Task started");

        while (!token.IsCancellationRequested)
        {
            var cmd = await queue.TakeAsync(token);
            servo.HandleCommand(cmd);
        }
    }
}
