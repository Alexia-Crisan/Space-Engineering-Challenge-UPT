namespace TeamCepheus.Robot.Motion;

/// <summary>
/// Interface for controlling servo motors using hardware PWM.
/// </summary>
public interface IServoController : IDisposable
{
    /// <summary>
    /// Starts the servo motor.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the servo motor.
    /// </summary>
    void Stop();

    /// <summary>
    /// Sets the servo angle in degrees (0-180).
    /// </summary>
    /// <param name="angle">The angle in degrees (0-180).</param>
    void SetAngle(int angle);

    /// <summary>
    /// Gets the current servo angle in degrees.
    /// </summary>
    /// <returns>The current angle in degrees.</returns>
    int GetAngle();

    /// <summary>
    /// Checks if the servo is currently running.
    /// </summary>
    /// <returns>True if the servo is running, false otherwise.</returns>
    bool IsRunning();
}
