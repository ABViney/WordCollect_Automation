using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
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
            return new BoundingBox(width, height, 0, 0);
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

        // Find the -geometry line
        var match = Regex.Match(output, @"-geometry\s+(\d+)x(\d+)\+(-?\d+)\+(-?\d+)");
        if (match.Success)
        {
            int width = int.Parse(match.Groups[1].Value);
            int height = int.Parse(match.Groups[2].Value);
            int x = int.Parse(match.Groups[3].Value);
            int y = int.Parse(match.Groups[4].Value);

            // Shift y to 0 if it's negative
            y = Math.Max(0, y);

            return new BoundingBox(width, height, x, y);
        }
        else
        {
            throw new Exception("Unable to parse window geometry.");
        }
    }
    
    /// <summary>
    /// Takes a screenshot of an x11 window
    /// </summary>
    /// <param name="window"></param>
    /// <param name="outputFile"></param>
    public static void ScreenshotWindow(string window, string outputFile)
    {
        // X11-window only method
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"import -window \\\"{window}\\\" {outputFile}\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }
}