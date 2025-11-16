using System;
using System.Device.Gpio;
using System.Device.Pwm;
using System.Threading;
using System.Collections.Generic;

class Program
{
    const int AIN1 = 27, AIN2 = 17, PWMA = 4;
    const int BIN1 = 24, BIN2 = 22, PWMB = 23;
    const int STBY = 25;

    static GpioController gpio;
    static SoftwarePwm pwmA;
    static SoftwarePwm pwmB;
    static int speed = 70;         // 0–100 %
    static int turnSpeed = 50;     // slower for turning
    static bool running = true;

    static void Main()
    {
        gpio = new GpioController();

        int[] pins = { AIN1, AIN2, BIN1, BIN2, STBY };
        foreach (var pin in pins)
        {
            gpio.OpenPin(pin, PinMode.Output);
            gpio.Write(pin, PinValue.Low);
        }

        gpio.Write(STBY, PinValue.High);

        pwmA = new SoftwarePwm(gpio, PWMA, 1000); // 1 kHz
        pwmB = new SoftwarePwm(gpio, PWMB, 1000);
        pwmA.Start();
        pwmB.Start();

        Console.WriteLine("Use arrow keys to drive. PageUp/PageDown to adjust speed. Q to quit.");

        var pressed = new HashSet<ConsoleKey>();
        Console.TreatControlCAsInput = true;

        Thread keyReader = new Thread(() =>
        {
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    var key = keyInfo.Key;

                    if ((keyInfo.Modifiers & ConsoleModifiers.Alt) != 0 ||
                        (keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                        continue;

                    if (keyInfo.Key == ConsoleKey.Q)
                    {
                        running = false;
                        break;
                    }

                    if (keyInfo.Key == ConsoleKey.PageUp)
                    {
                        speed = Math.Min(speed + 10, 100);
                        Console.WriteLine($"Speed: {speed}%");
                        continue;
                    }
                    if (keyInfo.Key == ConsoleKey.PageDown)
                    {
                        speed = Math.Max(speed - 10, 10);
                        Console.WriteLine($"Speed: {speed}%");
                        continue;
                    }

                    if (!pressed.Contains(key))
                    {
                        pressed.Add(key);
                        HandleKeyDown(key);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        });

        keyReader.Start();

        // Detect key releases
        while (running)
        {
            Thread.Sleep(50);
            foreach (ConsoleKey key in Enum.GetValues(typeof(ConsoleKey)))
            {
                if (pressed.Contains(key) && !Console.KeyAvailable)
                {
                    if (!IsKeyDown(key))
                    {
                        pressed.Remove(key);
                        HandleKeyUp(key);
                    }
                }
            }
        }

        Stop();
        pwmA.Dispose();
        pwmB.Dispose();
        gpio.Dispose();
    }

    static void HandleKeyDown(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow: Forward(); break;
            case ConsoleKey.DownArrow: Backward(); break;
            case ConsoleKey.LeftArrow: Left(); break;
            case ConsoleKey.RightArrow: Right(); break;
        }
    }

    static void HandleKeyUp(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
                Stop();
                break;
        }
    }

    static bool IsKeyDown(ConsoleKey key)
    {
        // Console input doesn't support direct key state queries cross-platform.
        // Simplify by treating keys as "released" when not read again soon.
        return false;
    }

    static void Forward()
    {
        gpio.Write(AIN1, PinValue.Low);
        gpio.Write(AIN2, PinValue.High);
        gpio.Write(BIN1, PinValue.Low);
        gpio.Write(BIN2, PinValue.High);
        pwmA.SetDutyCycle(speed);
        pwmB.SetDutyCycle(speed);
    }

    static void Backward()
    {
        gpio.Write(AIN1, PinValue.High);
        gpio.Write(AIN2, PinValue.Low);
        gpio.Write(BIN1, PinValue.High);
        gpio.Write(BIN2, PinValue.Low);
        pwmA.SetDutyCycle(speed);
        pwmB.SetDutyCycle(speed);
    }

    static void Left()
    {
        gpio.Write(AIN1, PinValue.Low);
        gpio.Write(AIN2, PinValue.High);
        gpio.Write(BIN1, PinValue.High);
        gpio.Write(BIN2, PinValue.Low);
        pwmA.SetDutyCycle(turnSpeed);
        pwmB.SetDutyCycle(turnSpeed);
    }

    static void Right()
    {
        gpio.Write(AIN1, PinValue.High);
        gpio.Write(AIN2, PinValue.Low);
        gpio.Write(BIN1, PinValue.Low);
        gpio.Write(BIN2, PinValue.High);
        pwmA.SetDutyCycle(turnSpeed);
        pwmB.SetDutyCycle(turnSpeed);
    }

    static void Stop()
    {
        pwmA.SetDutyCycle(0);
        pwmB.SetDutyCycle(0);
        gpio.Write(AIN1, PinValue.Low);
        gpio.Write(AIN2, PinValue.Low);
        gpio.Write(BIN1, PinValue.Low);
        gpio.Write(BIN2, PinValue.Low);
    }
}

/// <summary>
/// Simple software PWM implementation using a background thread.
/// </summary>
class SoftwarePwm : IDisposable
{
    private readonly GpioController gpio;
    private readonly int pin;
    private readonly int frequency;
    private readonly Thread thread;
    private bool running;
    private double duty;

    public SoftwarePwm(GpioController gpio, int pin, int frequency)
    {
        this.gpio = gpio;
        this.pin = pin;
        this.frequency = frequency;
        gpio.OpenPin(pin, PinMode.Output);
        running = true;
        thread = new Thread(Run) { IsBackground = true };
    }

    public void Start() => thread.Start();

    public void SetDutyCycle(int percent)
    {
        duty = Math.Clamp(percent / 100.0, 0, 1);
    }

    private void Run()
    {
        double period = 1000.0 / frequency;
        while (running)
        {
            gpio.Write(pin, PinValue.High);
            Thread.Sleep((int)(period * duty));
            gpio.Write(pin, PinValue.Low);
            Thread.Sleep((int)(period * (1 - duty)));
        }
    }

    public void Dispose()
    {
        running = false;
        thread.Join();
        gpio.Write(pin, PinValue.Low);
        gpio.ClosePin(pin);
    }
}
