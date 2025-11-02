using System;
using System.Collections.Generic;
using System.Device.I2c;
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
        public double Temperature;
        public double Humidity;
        public double Pressure;
    }

    public class SensorBuffer
    {
        private readonly Queue<double> _buffer;
        private readonly int _maxSize;

        public SensorBuffer(int maxSize)
        {
            _buffer = new Queue<double>();
            _maxSize = maxSize;
        }

        public void Add(double value)
        {
            if (_buffer.Count >= _maxSize)
                _buffer.Dequeue();

            _buffer.Enqueue(value);
        }

        public double Average()
        {
            return _buffer.Count == 0 ? 0.0 : _buffer.Average();
        }
    }

    public class Sensors : IDisposable
    {
        private readonly SensorBuffer _tempBuffer;
        private readonly SensorBuffer _humBuffer;
        private readonly SensorBuffer _pressBuffer;

        private readonly Dht11 _dht11;
        private readonly Bme280 _bme280;
        private SensorState _state;

        public Sensors(int bufferSize = 10)
        {
            _tempBuffer = new SensorBuffer(bufferSize);
            _humBuffer = new SensorBuffer(bufferSize);
            _pressBuffer = new SensorBuffer(bufferSize);
            _state = SensorState.Idle;

            _dht11 = new Dht11(pin: 4);

            var i2cSettings = new I2cConnectionSettings(1, 0x76);
            var i2cDevice = I2cDevice.Create(i2cSettings);
            _bme280 = new Bme280(i2cDevice)
            {
                TemperatureSampling = Sampling.LowPower,
                PressureSampling = Sampling.LowPower,
                HumiditySampling = Sampling.LowPower
            };

            _bme280.SetPowerMode(Bmx280PowerMode.Normal);
            Console.WriteLine("[INFO] BME280 detected at address 0x76");
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

                    Console.WriteLine(
                        $"[SENSOR] Avg Temp: {_tempBuffer.Average():0.00}°C | " +
                        $"Avg Hum: {_humBuffer.Average():0.00}% | " +
                        $"Avg Press: {_pressBuffer.Average():0.00} hPa"
                    );
                    break;
            }

            Thread.Sleep(200);
        }

        private SensorData ReadSensors()
        {
            var data = new SensorData();

            if (_bme280 != null)
            {
                var read = _bme280.Read();
                data.Temperature = read.Temperature?.DegreesCelsius ?? _tempBuffer.Average();
                data.Humidity = read.Humidity?.Percent ?? _humBuffer.Average();
                data.Pressure = read.Pressure?.Hectopascals ?? _pressBuffer.Average();
            }
            else
            {
                if (_dht11.TryReadTemperature(out var temp) && _dht11.TryReadHumidity(out var hum))
                {
                    data.Temperature = temp.DegreesCelsius;
                    data.Humidity = hum.Percent;
                }
                else
                {
                    data.Temperature = _tempBuffer.Average();
                    data.Humidity = _humBuffer.Average();
                }
            }

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
            _dht11?.Dispose();
            _bme280?.Dispose();
        }
    }
}
