using System;
using System.Threading;

namespace SensorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Initializing sensors...");

            //using var sensors = new Sensors(10);

            var motor = new MotorController();
            Console.WriteLine("Use arrow keys to control the robot. Press ESC to quit.");

            while (true)
            {
                //sensors.SensorTask();
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            motor.Forward();
                            break;
                        case ConsoleKey.DownArrow:
                            motor.Backward();
                            break;
                        case ConsoleKey.LeftArrow:
                            motor.Left();
                            break;
                        case ConsoleKey.RightArrow:
                            motor.Right();
                            break;
                        case ConsoleKey.Escape:
                            motor.Stop();
                            motor.Cleanup();
                            Console.WriteLine("Exiting...");
                            return;
                        default:
                            motor.Stop();
                            break;
                    }
                }
                else
                {
                    motor.Stop();
                }

                Thread.Sleep(1000);
            }
        }
    }
}
