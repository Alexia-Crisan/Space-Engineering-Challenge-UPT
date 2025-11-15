using System;
using System.Device.Pwm;
using Iot.Device.ServoMotor;
using System.Threading;

class Program
{
    static void Main()
    {
        using var servo = new ServoMotor(PwmChannel.Create(0, 0));

        servo.Start();

        Console.WriteLine("Servo se mișcă automat de la 0° la 180° din 45° în 45°...");

        for (int angle = 0; angle <= 180; angle += 45)
        {
            servo.WriteAngle(angle);
            Console.WriteLine($"→ Unghi: {angle}°");

            if (angle < 180)
                Thread.Sleep(3000);
        }

        servo.Stop();
        Console.WriteLine("Gata!");
    }
}