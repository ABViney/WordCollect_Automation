namespace WordCollect_Automated.Models;

public class BoundingBox
{
    public int Width { get; }
    public int Height { get; }
    public int X { get; }
    public int Y { get; }
    public int Area { get; }

    public BoundingBox(int width, int height, int x, int y)
    {
        Width = width;
        Height = height;
        X = x;
        Y = y;
        Area = width * height;
    }
    
    // Test if the other box is inside of this box
    public bool IsEncapsulating(BoundingBox other)
    {
        return other.X >= X &&
               other.Y >= Y &&
               other.X + other.Width <= X + Width &&
               other.Y + other.Height <= Y + Height;
    }

    // Gets a bounding box normalized to a parent container
    public BoundingBox GetAbsoluteBounds(BoundingBox parent)
    {
        return new BoundingBox(Width, Height, parent.X + X, parent.Y + Y);
    } 
        
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