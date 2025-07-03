using System.Drawing;

namespace WordCollect_Automated.Services.InputSimulation.Interpolation;

public interface IStrokeInterpolator
{
    IEnumerable<Point> Interpolate(IEnumerable<Point> controlPoints, int resolution);
}