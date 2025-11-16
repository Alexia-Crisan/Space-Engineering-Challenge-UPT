using System;
using System.Device.Gpio;

public class MotorController : IDisposable
{
    private readonly int AIN1 = 27;
    private readonly int AIN2 = 17;
    private readonly int PWMA = 4;
    private readonly int BIN1 = 24;
    private readonly int BIN2 = 22;
    private readonly int PWMB = 23;
    private readonly int STBY = 25;

    private readonly GpioController _gpio;
    private readonly SoftwarePwm _pwmA;
    private readonly SoftwarePwm _pwmB;
    private int _speed = 70;
    private int _turnSpeed = 50;

    public MotorController()
    {
        _gpio = new GpioController();
        int[] pins = { AIN1, AIN2, BIN1, BIN2, STBY };
        foreach (var pin in pins)
        {
            _gpio.OpenPin(pin, PinMode.Output);
            _gpio.Write(pin, PinValue.Low);
        }

        _gpio.Write(STBY, PinValue.High);

        _pwmA = new SoftwarePwm(_gpio, PWMA, 1000);
        _pwmB = new SoftwarePwm(_gpio, PWMB, 1000);
        _pwmA.Start();
        _pwmB.Start();
    }

    public void Forward()
    {
        _gpio.Write(AIN1, PinValue.Low);
        _gpio.Write(AIN2, PinValue.High);
        _gpio.Write(BIN1, PinValue.Low);
        _gpio.Write(BIN2, PinValue.High);
        _pwmA.SetDutyCycle(_speed);
        _pwmB.SetDutyCycle(_speed);
    }

    public void Backward()
    {
        _gpio.Write(AIN1, PinValue.High);
        _gpio.Write(AIN2, PinValue.Low);
        _gpio.Write(BIN1, PinValue.High);
        _gpio.Write(BIN2, PinValue.Low);
        _pwmA.SetDutyCycle(_speed);
        _pwmB.SetDutyCycle(_speed);
    }

    public void Left()
    {
        _gpio.Write(AIN1, PinValue.Low);
        _gpio.Write(AIN2, PinValue.High);
        _gpio.Write(BIN1, PinValue.High);
        _gpio.Write(BIN2, PinValue.Low);
        _pwmA.SetDutyCycle(_turnSpeed);
        _pwmB.SetDutyCycle(_turnSpeed);
    }

    public void Right()
    {
        _gpio.Write(AIN1, PinValue.High);
        _gpio.Write(AIN2, PinValue.Low);
        _gpio.Write(BIN1, PinValue.Low);
        _gpio.Write(BIN2, PinValue.High);
        _pwmA.SetDutyCycle(_turnSpeed);
        _pwmB.SetDutyCycle(_turnSpeed);
    }

    public void Stop()
    {
        _pwmA.SetDutyCycle(0);
        _pwmB.SetDutyCycle(0);
        _gpio.Write(AIN1, PinValue.Low);
        _gpio.Write(AIN2, PinValue.Low);
        _gpio.Write(BIN1, PinValue.Low);
        _gpio.Write(BIN2, PinValue.Low);
    }

    public void Cleanup() => Dispose();

    public void Dispose()
    {
        try { _pwmA.Dispose(); } catch { }
        try { _pwmB.Dispose(); } catch { }
        try { _gpio?.Dispose(); } catch { }
    }
}