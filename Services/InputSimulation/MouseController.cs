using System.Drawing;
using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services.InputSimulation;

public enum MouseButton
{
    Left,
    Middle,
    Right
}

/// <summary>
/// A service for controlling the mouse.
/// Designed explicitly for interfacing with <see cref="WordCollect_Automated.Services.Input"/> using dotool.
/// Dotool uses a scalar value to manipulate the mouse across the desktop. This class handles converting pixel values to
/// scalar, manipulating the cursor, and simulating button presses/releases.
/// </summary>
public class MouseController
{
    public int AreaWidth { get; }
    public int AreaHeight { get; }

    /// <summary>
    /// Creates a mouse controller for the following screenspace.
    /// </summary>
    /// <param name="desktop"></param>
    public MouseController(int areaWidth, int areaHeight)
    {
        AreaWidth = areaWidth;
        AreaHeight = areaHeight;
    }
    
    /// <summary>
    /// Moves the mouse cursor to a pixel position.
    /// </summary>
    /// <param name="position">A point representing a position in pixels.</param>
    /// <returns>True if <see cref="Input">dotool</see> is running and the command was successfully issued.</returns>
    public bool MoveTo(Point position)
    {
        double x = Convert.ToDouble(position.X) / Convert.ToDouble(AreaWidth);
        double y = Convert.ToDouble(position.Y) / Convert.ToDouble(AreaHeight);
        string command = $"mouseto {x.ToString("0.0000")} {y.ToString("0.0000")}";
        Input input = Input.GetInstance();
        if (input.IsRunning || input.Start())
        {
            return input.IssueCommand(command);
        }

        return false;
    }

    /// <summary>
    /// Presses the mouse button and releases it shortly after
    /// </summary>
    /// <returns>True if the input was submitted</returns>
    public bool Click(MouseButton button)
    {
        return Input.GetInstance().IssueCommand($"click {ParseMouseButton(button)}");
    }

    /// <summary>
    /// Presses the left mouse button. Doesn't depress until <see cref="Release"/>
    /// </summary>
    /// <returns>True if the input was submitted</returns>
    public bool Press(MouseButton button)
    {
        return Input.GetInstance().IssueCommand($"buttondown {ParseMouseButton(button)}");
    }
    
    /// <summary>
    /// Releases the left mouse button.
    /// </summary>
    /// <returns>True if the input was submitted</returns>
    public bool Release(MouseButton button)
    {
        return Input.GetInstance().IssueCommand($"buttonup {ParseMouseButton(button)}");
    }

    private string ParseMouseButton(MouseButton button)
    {
        switch (button)
        {
            case MouseButton.Left:
                return "left";
            case MouseButton.Middle:
                return "middle";
            case MouseButton.Right:
                return "right";
            default:
                throw new ArgumentException($"No match found for {button}");
        }
    }
    
}