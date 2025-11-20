using System;

namespace TeamCepheus.Robot.Motion;

/// <summary>
/// Interface for motion controllers controlling a tank-drive robot.
/// </summary>
public interface IMotionController : IDisposable
{
    /// <summary>
    /// Drive the robot
    /// </summary>
    /// <param name="forward"></param>
    void Drive(bool forward);

    /// <summary>
    /// Rotate left
    /// </summary>
    void RotateLeft();

    /// <summary>
    /// Rotate right
    /// </summary>
    void RotateRight();

    /// <summary>
    /// Set speed of each motor
    /// </summary>
    void Speed(uint motorA, uint motorB);

    /// <summary>
    /// Stop power to motors (coast)
    /// </summary>
    void Stop();

    /// <summary>
    /// Brake motors (active braking)
    /// </summary>
    void Brake();
}
