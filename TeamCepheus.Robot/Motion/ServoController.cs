using System.Device.Pwm;
using Iot.Device.ServoMotor;

namespace TeamCepheus.Robot.Motion;

/// <summary>
/// Controls a servo motor using hardware PWM on a specified PWM chip and channel.
/// </summary>
public class ServoController : IServoController
{
    private readonly PwmChannel _pwmChannel;
    private readonly ServoMotor _servo;
    private int _currentAngle = 90; // Default to middle position
    private bool _running;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the ServoController.
    /// </summary>
    /// <param name="pwmChip">The PWM chip number (e.g., 0).</param>
    /// <param name="pwmChannel">The PWM channel number (e.g., 0).</param>
    public ServoController(int pwmChip = 0, int pwmChannel = 0)
    {
        try
        {
            _pwmChannel = PwmChannel.Create(pwmChip, pwmChannel);
            _servo = new ServoMotor(_pwmChannel);
            Console.WriteLine($"[INFO] Servo initialized on PWM chip {pwmChip}, channel {pwmChannel}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to initialize servo on PWM chip {pwmChip}, channel {pwmChannel}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Starts the servo motor.
    /// </summary>
    public void Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ServoController));

        if (_running)
            return;

        try
        {
            _servo.Start();
            _running = true;
            Console.WriteLine("[INFO] Servo started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to start servo: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Stops the servo motor.
    /// </summary>
    public void Stop()
    {
        if (_disposed)
            return;

        if (!_running)
            return;

        try
        {
            _servo.Stop();
            _running = false;
            Console.WriteLine("[INFO] Servo stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to stop servo: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the servo angle in degrees (0-180).
    /// </summary>
    /// <param name="angle">The angle in degrees (0-180).</param>
    public void SetAngle(int angle)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ServoController));

        if (!_running)
        {
            Console.WriteLine("[WARN] Servo is not running. Call Start() first.");
            return;
        }

        // Clamp angle to 0-180 range
        int clampedAngle = Math.Clamp(angle, 0, 180);

        try
        {
            _servo.WriteAngle(clampedAngle);
            _currentAngle = clampedAngle;
            Console.WriteLine($"[INFO] Servo angle set to {clampedAngle}°");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to set servo angle: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the current servo angle in degrees.
    /// </summary>
    /// <returns>The current angle in degrees.</returns>
    public int GetAngle()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ServoController));

        return _currentAngle;
    }

    /// <summary>
    /// Checks if the servo is currently running.
    /// </summary>
    /// <returns>True if the servo is running, false otherwise.</returns>
    public bool IsRunning()
    {
        return _running && !_disposed;
    }

    /// <summary>
    /// Disposes the servo controller and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_running)
                Stop();

            _servo?.Dispose();
            _pwmChannel?.Dispose();
            Console.WriteLine("[INFO] Servo controller disposed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error disposing servo controller: {ex.Message}");
        }
    }
}
