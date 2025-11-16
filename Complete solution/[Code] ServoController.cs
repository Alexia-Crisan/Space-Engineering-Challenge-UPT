using System;
using System.Device.Pwm;
using Iot.Device.ServoMotor;
using System.Threading;
using System.Device.Gpio;

public class ServoControllerWorker : IDisposable
{
    private readonly ServoMotor _hardwareServo;
    private readonly int _pin;

    public ServoControllerWorker(int pin = 18)
    {
        _pin = pin;

        try
        {
            var channel = PwmChannel.Create(0, 0, 50, 0.075); // 50Hz, ~7.5% center
            _hardwareServo = new ServoMotor(channel);
            _hardwareServo.Start();
            Console.WriteLine("[INFO] Servo: hardware PWM initialized.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Hardware PWM unavailable: " + ex.Message);
            throw;
        }
    }

    public void HandleCommand(ServoCommand cmd)
    {
        switch (cmd)
        {
            case ServoCommand.Angle45: SetAngle(45); break;
            case ServoCommand.Angle90: SetAngle(90); break;
        }
    }

    public void SetAngle(int angle)
    {
        angle = Math.Clamp(angle, 0, 180);
        _hardwareServo.WriteAngle(angle);
    }

    public void Cleanup() => Dispose();

    public void Dispose()
    {
        try { _hardwareServo.Stop(); } catch { }
        try { _hardwareServo.Dispose(); } catch { }
    }
}

class ServoProgram
{
    static void Run()
    {
        Console.WriteLine("Servo control (simple)");
        Console.WriteLine("  D = 45°");
        Console.WriteLine("  E = 90°");
        Console.WriteLine("  Q = Quit");

        var servo = new ServoControllerWorker();

        try
        {
            bool running = true;
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.D:
                            servo.SetAngle(45);
                            Console.WriteLine("-> 45°");
                            break;
                        case ConsoleKey.E:
                            servo.SetAngle(90);
                            Console.WriteLine("-> 90°");
                            break;
                        case ConsoleKey.Q:
                            running = false;
                            break;
                    }
                }

                Thread.Sleep(50);
            }
        }
        finally
        {
            servo.Dispose();
            Console.WriteLine("Servo controller exiting.");
        }
    }
}
