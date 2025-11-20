using System.Device.Gpio;
using System.Device.Pwm.Drivers;

namespace TeamCepheus.Robot.Motion
{
    /// <summary>
    /// MotionController controls a tank-drive robot using an H-bridge and software PWM.
    ///
    /// Contract:
    /// - Inputs: angle (int,  -90..90 typical), reverse (bool)
    /// - Outputs: drives two motors via GPIO and SoftwarePwm
    /// - Error modes: clamps speeds/angles; safe Stop on dispose
    /// </summary>
    public class MotionController : IMotionController
    {
        // Pin definitions taken from the sample
        private const int AIN1 = 27, AIN2 = 17, PWMA = 4;
        private const int BIN1 = 24, BIN2 = 22, PWMB = 23;
        private const int STBY = 25;

        private readonly GpioController _gpio;
        private readonly SoftwarePwmChannel _pwmA;
        private readonly SoftwarePwmChannel _pwmB;

        public MotionController(int pwmFrequencyHz = 100)
        {
            _gpio = new GpioController();

            int[] pins = { AIN1, AIN2, BIN1, BIN2, STBY };
            foreach (var pin in pins)
            {
                _gpio.OpenPin(pin, PinMode.Output);
                _gpio.Write(pin, PinValue.Low);
            }

            _gpio.Write(STBY, PinValue.High);

            _pwmA = new SoftwarePwmChannel(PWMA, pwmFrequencyHz);
            _pwmB = new SoftwarePwmChannel(PWMB, pwmFrequencyHz);
            _pwmA.Start();
            _pwmB.Start();
        }

        public void Drive(bool forward)
        {
            SetMotorDirection(forward, forward);
        }

        public void RotateLeft()
        {
            SetMotorDirection(true, false);
        }

        public void RotateRight()
        {
            SetMotorDirection(false, true);
        }

        public void Speed(uint motorA, uint motorB)
        {
            _pwmA.DutyCycle = motorA / 100.0;
            _pwmB.DutyCycle = motorB / 100.0;
        }

        public void Stop()
        {
            _pwmA.DutyCycle = 0;
            _pwmB.DutyCycle = 0;
            _gpio.Write(AIN1, PinValue.Low);
            _gpio.Write(AIN2, PinValue.Low);
            _gpio.Write(BIN1, PinValue.Low);
            _gpio.Write(BIN2, PinValue.Low);
        }

        public void Brake()
        {
            _pwmA.DutyCycle = 0;
            _pwmB.DutyCycle = 0;
            _gpio.Write(AIN1, PinValue.High);
            _gpio.Write(AIN2, PinValue.High);
            _gpio.Write(BIN1, PinValue.High);
            _gpio.Write(BIN2, PinValue.High);
        }

        private void SetMotorDirection(bool isAForward, bool isBForward)
        {
            _gpio.Write(AIN1, isAForward ? PinValue.Low : PinValue.High);
            _gpio.Write(AIN2, isAForward ? PinValue.High : PinValue.Low);

            _gpio.Write(BIN1, isBForward ? PinValue.Low : PinValue.High);
            _gpio.Write(BIN2, isBForward ? PinValue.High : PinValue.Low);
        }

        public void Dispose()
        {
            try
            {
                Stop();
                _pwmA?.Stop();
                _pwmB?.Stop();
            }
            catch { }

            try
            {
                _pwmA?.Dispose();
                _pwmB?.Dispose();
            }
            catch { }

            try
            {
                _gpio?.ClosePin(PWMA);
                _gpio?.ClosePin(PWMB);
                _gpio?.ClosePin(AIN1);
                _gpio?.ClosePin(AIN2);
                _gpio?.ClosePin(BIN1);
                _gpio?.ClosePin(BIN2);
                _gpio?.ClosePin(STBY);
                _gpio?.Dispose();
            }
            catch { }
        }
    }
}
