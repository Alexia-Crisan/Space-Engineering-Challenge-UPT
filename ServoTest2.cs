using System.Device.Pwm;
using Iot.Device.ServoMotor;

class Program
{
    static void Main()
    {
        using var servo = new ServoMotor(PwmChannel.Create(0, 0));

        servo.Start();

        Console.WriteLine("Enter angle (0-180) or 'q' to quit:");

        while (true)
        {
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (input == null) continue;
            if (input.Trim().ToLower() == "q") break;

            if (int.TryParse(input, out int angle))
            {
                servo.WriteAngle(angle);
                Console.WriteLine($"→ Angle: {angle}° ");
            }
            else
            {
                Console.WriteLine("Please enter a number between 0 and 180, or 'q' to quit.");
            }
        }

        servo.Stop();
    }
}