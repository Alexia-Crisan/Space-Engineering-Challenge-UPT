namespace TeamCepheus.Robot.Sensors;

/// <summary>
/// Struct to hold sensor data readings.
/// </summary>
public struct SensorData
{
    public double Temperature;
    public double Humidity;
    public double Pressure;
}

/// <summary>
/// Interface for sensor controller that manages BME280 sensors for reading
/// temperature, humidity, and pressure data.
/// </summary>
public interface ISensorsController : IDisposable
{
    /// <summary>
    /// Executes the sensor task cycle (calibration, reading, and logging).
    /// </summary>
    void SensorTask();

    /// <summary>
    /// Gets the averaged sensor data from the internal buffers.
    /// </summary>
    /// <returns>Averaged temperature, humidity, and pressure values.</returns>
    SensorData GetAverages();
}
