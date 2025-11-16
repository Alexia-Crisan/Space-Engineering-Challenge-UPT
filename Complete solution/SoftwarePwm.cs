using System;
using System.Device.Gpio;
using System.Threading;

public class SoftwarePwm : IDisposable
{
    private readonly GpioController gpio;
    private readonly int pin;
    private readonly int frequency;
    private readonly Thread thread;
    private bool running;
    private double duty;

    public SoftwarePwm(GpioController gpio, int pin, int frequency)
    {
        this.gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        this.pin = pin;
        this.frequency = frequency;
        gpio.OpenPin(pin, PinMode.Output);
        running = true;
        thread = new Thread(Run) { IsBackground = true };
    }

    public void Start() => thread.Start();

    public void SetDutyCycle(int percent)
    {
        duty = Math.Clamp(percent / 100.0, 0.0, 1.0);
    }

    private void Run()
    {
        double periodMs = 1000.0 / frequency;
        int periodRounded = Math.Max(1, (int)Math.Round(periodMs));

        while (running)
        {
            int onMs = (int)Math.Round(periodMs * duty);
            int offMs = periodRounded - onMs;

            if (onMs > 0)
            {
                gpio.Write(pin, PinValue.High);
                Thread.Sleep(onMs);
            }
            else
            {
                gpio.Write(pin, PinValue.Low);
            }

            if (offMs > 0)
            {
                gpio.Write(pin, PinValue.Low);
                Thread.Sleep(offMs);
            }
        }
    }

    public void Dispose()
    {
        running = false;
        try { thread.Join(); } catch { }
        try { gpio.Write(pin, PinValue.Low); } catch { }
        try { gpio.ClosePin(pin); } catch { }
    }
}
