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

        Console.WriteLine("Servo moves automatically from 0° to 180° in 45° steps...");

        while(true)
        {
            servo.WriteAngle(angle);
            Console.WriteLine($"→ Angle: {angle}°");

        //     if (angle < 180)
        //         Thread.Sleep(3000);
        }

        servo.Stop();
        Console.WriteLine("Done!");
    }
}