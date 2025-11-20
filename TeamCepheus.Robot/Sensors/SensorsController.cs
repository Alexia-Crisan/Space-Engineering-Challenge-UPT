using System.Device.I2c;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using UnitsNet;

namespace TeamCepheus.Robot.Sensors;

public enum SensorState
{
    Idle,
    Calibrate,
    Calculate
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

public class SensorsController : ISensorsController
{
    private readonly SensorBuffer _tempBuffer;
    private readonly SensorBuffer _humBuffer;
    private readonly SensorBuffer _pressBuffer;

    private Bme280? _bme280_1;
    private Bme280? _bme280_2;
    private SensorState _state;

    // thresholds
    private readonly double _tempThreshold = 1.0; // Celsius
    private readonly double _humThreshold = 10.0; // %
    private readonly double _pressThreshold = 2.0; // hPa

    // logs
    private readonly string _logFilePath = "sensor_log.csv";
    private readonly TimeSpan _logInterval = TimeSpan.FromSeconds(5);
    private DateTime _lastLogTime = DateTime.MinValue;

    public SensorsController(int bufferSize = 10)
    {
        _tempBuffer = new SensorBuffer(bufferSize);
        _humBuffer = new SensorBuffer(bufferSize);
        _pressBuffer = new SensorBuffer(bufferSize);
        _state = SensorState.Idle;

        try
        {
            var i2c1 = new I2cConnectionSettings(1, 0x76);
            var i2cDev1 = I2cDevice.Create(i2c1);

            _bme280_1 = new Bme280(i2cDev1)
            {
                TemperatureSampling = Sampling.LowPower,
                PressureSampling = Sampling.LowPower,
                HumiditySampling = Sampling.LowPower
            };
            _bme280_1.SetPowerMode(Bmx280PowerMode.Normal);
            Console.WriteLine("[INFO] BME280 #1 detected at 0x76");
        }
        catch (Exception ex)
        {
            _bme280_1 = null;
            Console.WriteLine("[WARN] BME280 #1 not detected! " + ex.Message);
        }

        try
        {
            var i2c2 = new I2cConnectionSettings(1, 0x77);
            var i2cDev2 = I2cDevice.Create(i2c2);

            _bme280_2 = new Bme280(i2cDev2)
            {
                TemperatureSampling = Sampling.LowPower,
                PressureSampling = Sampling.LowPower,
                HumiditySampling = Sampling.LowPower
            };
            _bme280_2.SetPowerMode(Bmx280PowerMode.Normal);
            Console.WriteLine("[INFO] BME280 #2 detected at address 0x77");
        }
        catch
        {
            _bme280_2 = null;
            Console.WriteLine("[WARN] BME280 #2 not detected! Running with a single sensor.");
        }

        InitializeLogFile();
    }

    private void InitializeLogFile()
    {
        if (!File.Exists(_logFilePath))
        {
            File.WriteAllText(_logFilePath,
                "Timestamp,Temp1,Temp2,Hum1,Hum2,Press1,Press2,TempDiff,HumDiff,PressDiff,TempAvg,HumAvg,PressAvg,Warning\n");
        }
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

                // Console.WriteLine(
                //     $"[SENSOR] Avg Temp: {_tempBuffer.Average():0.00}°C | " +
                //     $"Avg Hum: {_humBuffer.Average():0.00}% | " +
                //     $"Avg Press: {_pressBuffer.Average():0.00} hPa"
                // );
                break;
        }

        Thread.Sleep(200);
    }

    private SensorData ReadSensors()
    {
        double t1 = 0, t2 = 0;
        double h1 = 0, h2 = 0;
        double p1 = 0, p2 = 0;

        if (_bme280_1 != null)
        {
            try
            {
                var read1 = _bme280_1.Read();
                t1 = read1.Temperature?.DegreesCelsius ?? 0;
                h1 = read1.Humidity?.Percent ?? 0;
                p1 = read1.Pressure?.Hectopascals ?? 0;
            }
            catch
            {
                _bme280_1 = null;
                Console.WriteLine("[WARN] BME280 #1 read failed.");
            }
        }

        if (_bme280_2 != null)
        {
            try
            {
                var read2 = _bme280_2.Read();
                t2 = read2.Temperature?.DegreesCelsius ?? 0;
                h2 = read2.Humidity?.Percent ?? 0;
                p2 = read2.Pressure?.Hectopascals ?? 0;
            }
            catch
            {
                _bme280_2 = null;
                Console.WriteLine("[WARN] BME280 #2 read failed.");
            }
        }

        if (_bme280_1 == null && _bme280_2 == null)
        {
            t1 = t2 = _tempBuffer.Average();
            h1 = h2 = _humBuffer.Average();
            p1 = p2 = _pressBuffer.Average();
        }
        else if (_bme280_1 == null)
        {
            t1 = t2; h1 = h2; p1 = p2;
        }
        else if (_bme280_2 == null)
        {
            t2 = t1; h2 = h1; p2 = p1;
        }

        double tDiff = Math.Abs(t1 - t2);
        double hDiff = Math.Abs(h1 - h2);
        double pDiff = Math.Abs(p1 - p2);

        bool warning = false;
        string warningType = "";

        if (_bme280_1 != null && _bme280_2 != null)
        {
            if (tDiff > _tempThreshold)
            {
                warning = true;
                warningType += "Temp ";
            }
            if (hDiff > _humThreshold)
            {
                warning = true;
                warningType += "Hum ";
            }
            if (pDiff > _pressThreshold)
            {
                warning = true;
                warningType += "Press ";
            }
        }

        if (DateTime.Now - _lastLogTime >= _logInterval)
        {
            LogToCsv(t1, t2, h1, h2, p1, p2, tDiff, hDiff, pDiff, warning ? warningType.Trim() : "OK");
            _lastLogTime = DateTime.Now;
        }

        // if (warning)
        // {
        //     Console.WriteLine($"[WARN] {warningType}difference(s) exceed threshold(s)");
        // }

        return new SensorData
        {
            Temperature = (t1 + t2) / 2,
            Humidity = (h1 + h2) / 2,
            Pressure = (p1 + p2) / 2
        };
    }

    private void LogToCsv(double t1, double t2, double h1, double h2, double p1, double p2,
        double tDiff, double hDiff, double pDiff, string warning)
    {
        double tAvg = (t1 + t2) / 2.0;
        double hAvg = (h1 + h2) / 2.0;
        double pAvg = (p1 + p2) / 2.0;

        string line =
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{t1:0.00},{t2:0.00},{h1:0.00},{h2:0.00},{p1:0.00},{p2:0.00}," +
            $"{tDiff:0.00},{hDiff:0.00},{pDiff:0.00},{tAvg:0.00},{hAvg:0.00},{pAvg:0.00},{warning}\n";

        File.AppendAllText(_logFilePath, line);
    }

    public SensorData GetAverages() => new SensorData
    {
        Temperature = _tempBuffer.Average(),
        Humidity = _humBuffer.Average(),
        Pressure = _pressBuffer.Average()
    };

    public void Dispose()
    {
        _bme280_1?.Dispose();
        _bme280_2?.Dispose();
    }
}