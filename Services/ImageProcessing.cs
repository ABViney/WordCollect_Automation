using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services;

public static class ImageProcessing
{
    /// <summary>
    /// Overlays one image over another while retaining transparency properties.
    /// </summary>
    /// <param name="maskFile"></param>
    /// <param name="imageFile"></param>
    /// <param name="outputFile"></param>
    public static void MaskImage(string maskFile, string imageFile, string outputFile)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"composite -compose Over -gravity center {maskFile} {imageFile} PNG:{outputFile}\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }

    /// <summary>
    /// Increases the contrast of an image to create a grayscale image. This preprocessing is to isolate the white
    /// characters from the not-so-white background
    /// </summary>
    /// <param name="imageFile"></param>
    /// <param name="outputFile"></param>
    public static void RaiseContrast(string imageFile, string outputFile)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {imageFile} -colorspace gray -threshold 50% PNG:{outputFile}\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }
    
    /// <summary>
    /// Crops the image based on the bounding box.
    /// </summary>
    /// <param name="inputImage"></param>
    /// <param name="outputImage"></param>
    /// <param name="box"></param>
    /// <exception cref="Exception"></exception>
    public static void CropUsingBoundingBox(string inputImage, string outputImage, BoundingBox box)
    {
        string cropArgs = $"{box.Width}x{box.Height}+{box.X}+{box.Y}";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {inputImage} -crop {cropArgs} +repage PNG:{outputImage}\"",
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

    /// <summary>
    /// Resizes an image by the scale. Uses Point filter to prevent aliasing. 
    /// </summary>
    /// <param name="inputImage"></param>
    /// <param name="outputImage"></param>
    /// <param name="scale"></param>
    /// <exception cref="Exception"></exception>
    public static void ScaleImage(string inputImage, string outputImage, double scale)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {inputImage} -filter Point -resize {(scale*100).ToString("0.00")}% PNG:{outputImage}\"",
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

    /// <summary>
    /// Returns an inclusive value from 0 (perfect match) and 1 (perfect mismatch)
    /// </summary>
    /// <returns></returns>
    public static double NormalizedRootMeanSquareError(string inputImage, string comparisonImage)
    {
        // Use imagemagick to compare similarity betweween two images
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"compare -metric RMSE {inputImage} {comparisonImage} null:\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        // compare -metric writes the output we want to stderr
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        
        // Capture value inside the parenthesis, this is the normalized RMSE
        var regex = new Regex(@"\(([\d.]+)\)");
        var match = regex.Match(stderr.Trim());
        
        if (!match.Success)
        {
            throw new ApplicationException($"Failed to capture result from ImageMagick compare: Output was [{stderr}]");
        }
        
        return Double.Parse(match.Groups[1].Value);
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
                Arguments = $"-c \"convert {inputImage} -stroke red -strokewidth 1 -fill none -draw \\\"{drawCommand}\\\" PNG:{outputImage}\"",
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