using System.Diagnostics;
using System.Text;
using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services;

public static class ImageProcessing
{
    
    public static void MaskImage(string maskFile, string imageFile, string outputFile)
    {
        // X11-window only method
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"composite -compose Over -gravity center {maskFile} {imageFile} {outputFile}\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }

    public static void RaiseContrast(string imageFile, string outputFile)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {imageFile} -colorspace gray -threshold 50% {outputFile}\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }
    
    public static void CropUsingBoundingBox(string inputImage, string outputImage, BoundingBox box)
    {
        string cropArgs = $"{box.Width}x{box.Height}+{box.X}+{box.Y}";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {inputImage} -crop {cropArgs} +repage {outputImage}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"ImageMagick crop failed:\n{stderr}");
        }
    }

    public static void ScaleImage(string inputImage, string outputImage, double scale)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {inputImage} -filter Point -resize {(scale*100).ToString("0.00")}% {outputImage}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"ImageMagick crop failed:\n{stderr}");
        }
    }
        
    // Debug method for verifying bounding box accuracy
    public static void DrawBoundingBoxesOnImage(string inputImage, string outputImage, List<BoundingBox> boxes)
    {
        if (boxes == null || boxes.Count == 0)
            throw new ArgumentException("No bounding boxes provided.");

        // Build the -draw argument
        var drawBuilder = new StringBuilder();
        foreach (var box in boxes)
        {
            int x1 = box.X;
            int y1 = box.Y;
            int x2 = box.X + box.Width;
            int y2 = box.Y + box.Height;
            drawBuilder.Append($"rectangle {x1},{y1} {x2},{y2} ");
        }

        // Final magick command:
        string drawCommand = drawBuilder.ToString().Trim();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {inputImage} -stroke red -strokewidth 1 -fill none -draw \\\"{drawCommand}\\\" {outputImage}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"ImageMagick failed:\n{stderr}");
        }
    }
}