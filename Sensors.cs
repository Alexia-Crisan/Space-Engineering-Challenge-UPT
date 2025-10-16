using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Device.Gpio;
using System.Linq;
using System.Threading;
using Iot.Device.DHTxx;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;

namespace SensorApp
{
    public enum SensorState
    {
        Idle,
        Calibrate,
        Calculate
    }

    public struct SensorData
    {
        public float Temperature;
        public float Humidity;
        public float Pressure;
    }

    public class SensorBuffer
    {
        private readonly Queue<float> _buffer;
        private readonly int _maxSize;

        public SensorBuffer(int maxSize)
        {
            _buffer = new Queue<float>();
            _maxSize = maxSize;
        }

        public void Add(float value)
        {
            if (_buffer.Count >= _maxSize)
                _buffer.Dequeue();
            _buffer.Enqueue(value);
        }

        public float Average()
        {
            return _buffer.Count == 0 ? 0.0f : _buffer.Average();
        }
    }

    public class Sensors : IDisposable
    {
        private readonly SensorBuffer _tempBuffer;
        private readonly SensorBuffer _humBuffer;
        private readonly SensorBuffer _pressBuffer;

        private readonly Dht22 _dht22;
        private readonly Bme280 _bme280;
        private SensorState _state;

        public Sensors(int bufferSize = 10)
        {
            _tempBuffer = new SensorBuffer(bufferSize);
            _humBuffer = new SensorBuffer(bufferSize);
            _pressBuffer = new SensorBuffer(bufferSize);
            _state = SensorState.Idle;

            _dht22 = new Dht22(pin: 4, pinNumberingScheme: PinNumberingScheme.Logical);

            var i2cSettings = new I2cConnectionSettings(1, Bme280.DefaultI2cAddress);
            var i2cDevice = I2cDevice.Create(i2cSettings);
            _bme280 = new Bme280(i2cDevice)
            {
                TemperatureSampling = Sampling.LowPower,
                PressureSampling = Sampling.LowPower,
                HumiditySampling = Sampling.LowPower
            };
            _bme280.SetPowerMode(Bmx280PowerMode.Normal);
        }

        public void SensorTask()
        {
            switch (_state)
            {
                case SensorState.Idle:
                    Console.WriteLine("[SENSOR] State: IDLE");
                    _state = SensorState.Calibrate;
                    break;

                case SensorState.Calibrate:
                    Console.WriteLine("[SENSOR] Calibrating...");
                    for (int i = 0; i < 10; i++)
                    {
                        ReadSensors();
                        Thread.Sleep(500);
                    }
                    _state = SensorState.Calculate;
                    Console.WriteLine("[SENSOR] Calibration done.");
                    break;

                case SensorState.Calculate:
                    var data = ReadSensors();
                    _tempBuffer.Add(data.Temperature);
                    _humBuffer.Add(data.Humidity);
                    _pressBuffer.Add(data.Pressure);

                    Console.WriteLine($"[SENSOR] Avg Temp: {_tempBuffer.Average():0.00}°C | " +
                                      $"Avg Hum: {_humBuffer.Average():0.00}% | " +
                                      $"Avg Press: {_pressBuffer.Average():0.00} hPa");
                    break;
            }
        }

        private SensorData ReadSensors()
        {
            var data = new SensorData();

            if (_dht22.TryReadTemperature(out var temp) && _dht22.TryReadHumidity(out var hum))
            {
                data.Temperature = (float)temp.DegreesCelsius;
                data.Humidity = (float)hum.Percent;
            }
            else
            {
                data.Temperature = _tempBuffer.Average();
                data.Humidity = _humBuffer.Average();
            }

            data.Pressure = (float)_bme280.ReadPressure().Hectopascals;

            return data;
        }

        public SensorData GetAverages()
        {
            return new SensorData
            {
                Temperature = _tempBuffer.Average(),
                Humidity = _humBuffer.Average(),
                Pressure = _pressBuffer.Average()
            };
        }

        public void Dispose()
        {
            _dht22?.Dispose();
            _bme280?.Dispose();
        }
    }
}
