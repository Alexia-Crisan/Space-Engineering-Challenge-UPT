using System;
using System.Threading;
using System.Threading.Tasks;

public static partial class TaskWorkers
{
    public static async Task MotorTask(MotorController motors, AsyncCommandQueue<MotorCommand> queue, CancellationToken token)
    {
        Console.WriteLine("[TASK] Motor Task started");

        while (!token.IsCancellationRequested)
        {
            var cmd = await queue.TakeAsync(token);

            switch (cmd)
            {
                case MotorCommand.Forward: motors.Forward(); break;
                case MotorCommand.Backward: motors.Backward(); break;
                case MotorCommand.Left: motors.Left(); break;
                case MotorCommand.Right: motors.Right(); break;
                case MotorCommand.Stop: motors.Stop(); break;
            }
        }
    }
}
