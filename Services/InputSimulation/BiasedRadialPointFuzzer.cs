using System.Drawing;

namespace WordCollect_Automated.Services.InputSimulation;

/// <summary>
/// A service for getting a fuzzed position from a <see cref="Point"/>.
/// This class provides fuzzed values that bias towards the original position.
/// </summary>
public class BiasedRadialPointFuzzer
{
    private const int CENTER_BIAS = 2;
    
    private int _radius;
    private readonly Random _random;

    public int Radius
    {
        get => _radius;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(Radius), "Radius must be non-negative.");
            _radius = value;        
        }
    }

    public BiasedRadialPointFuzzer(int radius)
    {
        Radius = radius;
        _random = new Random();
    }

    public Point Fuzz(Point point)
    {
        // uses polar coordinates to get a random point in a circle
        // This is a random angle in the circle
        double theta = _random.NextDouble() * 2 * Math.PI;
        // The Math.Pow is to further increase the bias of the radius of the point toward the center
        double r = Math.Pow(_random.NextDouble(), CENTER_BIAS);
        double dX = r * Math.Cos(theta);
        double dY = r * Math.Sin(theta);
        int newX = point.X + Convert.ToInt32(Math.Round(dX * _radius));
        int newY = point.Y + Convert.ToInt32(Math.Round(dY * _radius));
        return new Point(newX, newY);
    }
}