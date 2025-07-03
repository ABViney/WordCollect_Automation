using System.Drawing;

namespace WordCollect_Automated.Services.InputSimulation.Interpolation;

public class LinearStrokeInterpolator : IStrokeInterpolator
{
    public IEnumerable<Point> Interpolate(IEnumerable<Point> controlPoints, int resolution)
    {
        var inputPoints = new List<Point>(controlPoints);
        if (inputPoints.Count < 2) yield break;

        for (int i = 0; i < inputPoints.Count - 1; i++)
        {
            var start = inputPoints[i];
            var end = inputPoints[i + 1];

            int dX = end.X - start.X;
            int dY = end.Y - start.Y;

            for (int j = 0; j <= resolution-1; j++)
            {
                // range between 0 and 1 of this position and the next. Calling it time for simplicity.
                float deltaTime = (float)j / resolution;
                // Interpolated coordinates
                int x = (int)(deltaTime * dX + start.X);
                int y = (int)(deltaTime * dY + start.Y);
                yield return new Point(x, y);
            }
        }
    }
}