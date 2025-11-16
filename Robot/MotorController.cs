using System;
using System.Device.Gpio;
using System.Device.Pwm;
using System.Device.Pwm.Drivers;
using System.Threading;

namespace Motors
{
    public class MotorController
    {
        private int AIN1 = 27;
        private int AIN2 = 17;
        private int PWMA = 18;
        private int BIN1 = 24;
        private int BIN2 = 22;
        private int PWMB = 23;
        private int STBY = 25;

        private int speed = 70; // 0–100
        private GpioController gpio;
        private SoftwarePwmChannel pwmA;
        private SoftwarePwmChannel pwmB;

        public MotorController()
        {
            gpio = new GpioController();
            int[] pins = { AIN1, AIN2, BIN1, BIN2, STBY };
            foreach (var pin in pins)
                gpio.OpenPin(pin, PinMode.Output);

            gpio.Write(STBY, PinValue.High);

            pwmA = new SoftwarePwmChannel(PWMA, 1000, 0.0, false);
            pwmB = new SoftwarePwmChannel(PWMB, 1000, 0.0, false);
            pwmA.Start();
            pwmB.Start();
        }

        public void Forward()
        {
            gpio.Write(AIN1, PinValue.High);
            gpio.Write(AIN2, PinValue.Low);
            gpio.Write(BIN1, PinValue.High);
            gpio.Write(BIN2, PinValue.Low);
            pwmA.DutyCycle = speed / 100.0;
            pwmB.DutyCycle = speed / 100.0;
        }

        public void Backward()
        {
            gpio.Write(AIN1, PinValue.Low);
            gpio.Write(AIN2, PinValue.High);
            gpio.Write(BIN1, PinValue.Low);
            gpio.Write(BIN2, PinValue.High);
            pwmA.DutyCycle = speed / 100.0;
            pwmB.DutyCycle = speed / 100.0;
        }

        public void Left()
        {
            gpio.Write(AIN1, PinValue.Low);
            gpio.Write(AIN2, PinValue.High);
            gpio.Write(BIN1, PinValue.High);
            gpio.Write(BIN2, PinValue.Low);
            pwmA.DutyCycle = speed / 100.0;
            pwmB.DutyCycle = speed / 100.0;
        }

        public void Right()
        {
            gpio.Write(AIN1, PinValue.High);
            gpio.Write(AIN2, PinValue.Low);
            gpio.Write(BIN1, PinValue.Low);
            gpio.Write(BIN2, PinValue.High);
            pwmA.DutyCycle = speed / 100.0;
            pwmB.DutyCycle = speed / 100.0;
        }

        public void Stop()
        {
            pwmA.DutyCycle = 0;
            pwmB.DutyCycle = 0;
        }

        public void Cleanup()
        {
            pwmA.Stop();
            pwmB.Stop();
            gpio.Dispose();
        }
    }
}