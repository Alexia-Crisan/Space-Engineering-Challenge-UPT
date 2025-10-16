using System;
using System.Threading;

namespace SensorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing sensors...");

            using var sensors = new Sensors(10);

            while (true)
            {
                sensors.SensorTask();
                Thread.Sleep(1000);
            }
        }
    }
}
