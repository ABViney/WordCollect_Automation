using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using Serilog;
using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services;

public static class GnomeDesktop
{
    /// <summary>
    /// Activate this window (brings it to focus)
    /// </summary>
    /// <param name="window">Title of the window</param>
    public static void FocusWindow(string window)
    {
        // X11-window only method
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"wmctrl -a \\\"{window}\\\"\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
        
        Log.Logger.Debug($"Focused window {window}");
    }

    /// <summary>
    /// Creates a bounding box that represents the desktop screenspace.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static BoundingBox GetDesktopBoundingBox()
    {
        // Get a string regarding the desktop's resolution that looks like this:
        // Screen 0: minimum 16 x 16, current 5760 x 1080, maximum 32767 x 32767
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName="/bin/bash",
                Arguments = "-c \"xrandr | head -1\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        // Get the dimensions marked as "current"
        var match = Regex.Match(output, @"current\s+(\d+)\s+x\s+(\d+)");
        if (match.Success)
        {
            int width = int.Parse(match.Groups[1].Value);
            int height = int.Parse(match.Groups[2].Value);
            BoundingBox desktopBoundingBox = new BoundingBox(width, height, 0, 0);
            
            Log.Logger.Debug($"Desktop has bounds {desktopBoundingBox}");
            return desktopBoundingBox;
        }
        else
        {
            throw new InvalidOperationException("Unable to parse desktop dimensions from xrandr output.");
        }
    }
    
    /// <summary>
    /// Creates a bounding box that represents a window on the desktop.
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static BoundingBox GetWindowBoundingBox(string window)
    {
        // Run xwininfo and capture output
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"xwininfo -name \\\"{window}\\\"\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        // Get topleft and dimensions
        var absoluteUpperLeftXMatch = Regex.Match(output, @"Absolute upper-left X:\s+(-?\d+)");
        var absoluteUpperLeftYMatch = Regex.Match(output, @"Absolute upper-left Y:\s+(-?\d+)");
        var widthMatch = Regex.Match(output, @"Width:\s+(\d+)");
        var heightMatch = Regex.Match(output, @"Height:\s+(\d+)");
        if (absoluteUpperLeftXMatch.Success && absoluteUpperLeftYMatch.Success && widthMatch.Success && heightMatch.Success)
        {
            int x = int.Parse(absoluteUpperLeftXMatch.Groups[1].Value);
            int y = int.Parse(absoluteUpperLeftYMatch.Groups[1].Value);
            int width = int.Parse(widthMatch.Groups[1].Value);
            int height = int.Parse(heightMatch.Groups[1].Value);
            
            // For some reason the top 12 pixels of my screen are negative lol
            y += 12;
            
            BoundingBox windowBoundingBox = new BoundingBox(width, height, x, y);
            Log.Logger.Debug($"{window} has bounds {windowBoundingBox}");
            return windowBoundingBox;
        }
        else
        {
            throw new Exception($"Unable to parse window geometry. Output was: [{output}]");
        }
    }
    
    /// <summary>
    /// Takes a screenshot of an x11 window
    /// </summary>
    /// <param name="window"></param>
    /// <param name="outputFile"></param>
    public static void ScreenshotWindow(string window, string outputFile)
    {
        Log.Logger.Debug($"Saving screenshot of {window} to {outputFile}");
        
        // X11-window only method
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"import -window \\\"{window}\\\" -silent PNG:{outputFile}\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }
}