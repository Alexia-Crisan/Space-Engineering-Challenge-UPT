using System;
using System.Threading;
using System.Threading.Tasks;
using SensorApp;

public static partial class TaskWorkers
{
    public static async Task SensorTaskAsync(Sensors sensors, CancellationToken token)
    {
        Console.WriteLine("[TASK] Sensor Task started");

        while (!token.IsCancellationRequested)
        {
            sensors.SensorTask();
            await Task.Delay(200, token);
        }
    }
}
