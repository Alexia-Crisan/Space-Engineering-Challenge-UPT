using System;
using System.Threading;
using System.Threading.Tasks;
using SensorApp;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Robot System (Task-based)");

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var sensors = new Sensors();
        var motors = new MotorController();
        var servo = new ServoControllerWorker();

        var motorQueue = new AsyncCommandQueue<MotorCommand>();
        var servoQueue = new AsyncCommandQueue<ServoCommand>();

        // Start all workers
        var motorTask = TaskWorkers.MotorTask(motors, motorQueue, token);
        var servoTask = TaskWorkers.ServoTask(servo, servoQueue, token);
        var sensorTask = TaskWorkers.SensorTaskAsync(sensors, token);

        Console.WriteLine("All tasks running.");

        // INPUT LOOP (async-friendly)
        while (!token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.UpArrow: motorQueue.Add(MotorCommand.Forward); break;
                    case ConsoleKey.DownArrow: motorQueue.Add(MotorCommand.Backward); break;
                    case ConsoleKey.LeftArrow: motorQueue.Add(MotorCommand.Left); break;
                    case ConsoleKey.RightArrow: motorQueue.Add(MotorCommand.Right); break;

                    case ConsoleKey.D: servoQueue.Add(ServoCommand.Angle45); break;
                    case ConsoleKey.E: servoQueue.Add(ServoCommand.Angle90); break;

                    case ConsoleKey.Q:
                        cts.Cancel();
                        break;
                }
            }

            await Task.Delay(20, token);
        }

        await Task.WhenAll(motorTask, servoTask, sensorTask);
        sensors.Dispose();
        motors.Cleanup();
        servo.Cleanup();

        Console.WriteLine("Shutdown clean.");
    }
}