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
    public bool Encapsulates(BoundingBox other)
    {
        return other.X >= X &&
               other.Y >= Y &&
               other.X + other.Width <= X + Width &&
               other.Y + other.Height <= Y + Height;
    }
    
    public override string ToString() => $"W:{Width}, H:{Height}, X:{X}, Y:{Y}";
}