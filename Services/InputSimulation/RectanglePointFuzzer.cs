using System.Drawing;
using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services.InputSimulation;

public class RectangularPointFuzzer
{
    private readonly Random _random;

    public RectangularPointFuzzer()
    {
        _random = new Random();
    }

    public Point Fuzz(BoundingBox box)
    {
        int x = Convert.ToInt32(_random.Next(box.X, box.X + box.Width));
        int y = Convert.ToInt32(_random.Next(box.Y, box.Y + box.Height));
        return new Point(x, y);
    }
}