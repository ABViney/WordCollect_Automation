using System.Drawing;

namespace WordCollect_Automated.Models;

public class BoundingBox
{
    public int Width { get; }
    public int Height { get; }
    public int X { get; }
    public int Y { get; }
    public int Area { get; }
    public Point Center { get; }

    public BoundingBox(int width, int height, int x, int y)
    {
        Width = width;
        Height = height;
        X = x;
        Y = y;
        Area = width * height;
        Center = new Point(X + (Width / 2), Y + (Height / 2));
    }
    
    /// <summary>
    /// Test if a bounding box at the same level is contained within this bounding box.
    /// </summary>
    /// <param name="other"></param>
    /// <returns>True if the other bounding box can be contained within this one.</returns>
    public bool IsEncapsulating(BoundingBox other)
    {
        return other.X >= X &&
               other.Y >= Y &&
               other.X + other.Width <= X + Width &&
               other.Y + other.Height <= Y + Height;
    }

    /// <summary>
    /// Get the absolute position of a nested bounding box
    /// </summary>
    /// <param name="child">A bounding box assumed inside of this one</param>
    /// <returns>A bounding box normalized to the same level as this one.</returns>
    public BoundingBox Normalize(BoundingBox child)
    {
        return new BoundingBox(child.Width, child.Height, X + child.X, Y + child.Y);
    } 
    
    /// <summary>
    /// Creates a bounding box that encapsulates one or more other bounding boxes.
    /// </summary>
    /// <param name="boxes">Bounding boxes at the same level</param>
    /// <returns>A bounding box with a position and area that wraps the bounding boxes provided.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static BoundingBox ThatEncapsulates(IEnumerable<BoundingBox> boxes)
    {
        if (boxes == null || !boxes.Any())
            throw new ArgumentException("No bounding boxes provided.");

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (var box in boxes)
        {
            minX = Math.Min(minX, box.X);
            minY = Math.Min(minY, box.Y);
            maxX = Math.Max(maxX, box.X + box.Width);
            maxY = Math.Max(maxY, box.Y + box.Height);
        }

        int width = maxX - minX;
        int height = maxY - minY;

        return new BoundingBox(width, height, minX, minY);
    }
    
    public override string ToString() => $"W:{Width}, H:{Height}, X:{X}, Y:{Y}";
}