using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;
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
    public static void MaskImage(string imageFile, string outputFile, string maskFile)
    {
        Log.Logger.Debug($"Saving {imageFile} to {outputFile} with overlay {maskFile}");
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
    public static void RaiseContrast(string imageFile, string outputFile, double threshold = 0.50)
    {
        int thresholdPercentage = Convert.ToInt32(threshold * 100);
        Log.Logger.Debug($"Saving {imageFile} to {outputFile} with contrast setting -threshold {thresholdPercentage}%");
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {imageFile} -colorspace gray -threshold {thresholdPercentage}% PNG:{outputFile}\"",
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
        Log.Logger.Debug($"Cropping {box} from {inputImage} to {outputImage}");
        
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
        string formattedScale = (scale * 100).ToString("0.00");
        Log.Logger.Debug($"Resizing {inputImage} to {outputImage} at {formattedScale}%");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {inputImage} -filter Point -resize {formattedScale}% PNG:{outputImage}\"",
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
        Log.Logger.Debug($"Getting normalized RMSE between {inputImage} and {comparisonImage}");
        
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
        double nrmse = Double.Parse(match.Groups[1].Value);
        Log.Logger.Debug($"Normalized RMSE between {inputImage} and {comparisonImage} is {nrmse}");
        
        return nrmse;
    }

    public static List<BoundingBox> GetComponents(string imageFile)
    {
        Log.Logger.Debug($"Getting components in {imageFile}");
        // Takes a file, gets the connected components in the file, and isolates the
        // column containing bounding box info
        // Geometry info is multiple lines of text that looks like this (WIDTHxHEIGHT+X+Y:
        // 47x47+257+831
        // 43x48+153+831
        // ...
        // 41x46+123+731
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {imageFile} -define connected-components:verbose=true -define connected-components:exclude-header=true -connected-components 8 -auto-level null: | awk '{{ print $2 }}'\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        var boxes = new List<BoundingBox>();
        var regex = new Regex(@"(\d+)x(\d+)\+(\d+)\+(\d+)");
        
        foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var match = regex.Match(line.Trim());
            if (match.Success)
            {
                int width = int.Parse(match.Groups[1].Value);
                int height = int.Parse(match.Groups[2].Value);
                int x = int.Parse(match.Groups[3].Value);
                int y = int.Parse(match.Groups[4].Value);
                
                boxes.Add(new BoundingBox(width, height, x, y));
            }
        }

        return boxes;
    }

    /// <summary>
    /// Gets the brightness of an image.
    /// </summary>
    /// <param name="inputImage"></param>
    /// <returns></returns>
    public static double GetBrightness(string inputImage)
    {
        Log.Logger.Debug($"Getting brightness of {inputImage}");
        // Running threshold and getting the percentage of white pixels to black pixels to determine the brightness.
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {inputImage} -colorspace Gray -threshold 50% -format \\\"%[fx:mean]\\\" info:\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        // imagemagick writes the output we want to stderr
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        // Capture value inside the parenthesis, this is the normalized RMSE
        var regex = new Regex(@"([\d.]+)");
        var match = regex.Match(output.Trim());
        
        if (!match.Success)
        {
            throw new ApplicationException($"Failed to capture result from ImageMagick operation: Output was [{output}]");
        }
        
        double brightness = Double.Parse(match.Groups[1].Value);
        Log.Logger.Debug($"Brightness of {inputImage} is {brightness}");

        return brightness;
    }

    /// <summary>
    /// Get the width and height of an image as a bounding box.
    /// </summary>
    /// <param name="inputImage"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static BoundingBox GetImageDimensions(string inputImage)
    {
        // Get Width Height of an image 
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"identify -format \\\"%w %h\\\" {inputImage}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        string stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        // Capture value inside the parenthesis, this is the normalized RMSE
        var regex = new Regex(@"(\d+)\s(\d+)");
        var match = regex.Match(stdout.Trim());
        
        if (!match.Success)
        {
            throw new ApplicationException($"Failed to capture result from ImageMagick compare: Output was [{stdout}]");
        }
        
        return new BoundingBox(
            int.Parse(match.Groups[1].Value), 
            int.Parse(match.Groups[2].Value), 
            0, 
            0);
    }

    public static void ResizeImage(string inputImage, string outputImage, BoundingBox dimensions)
    {
        Log.Logger.Debug($"Resizing {inputImage} to {outputImage} at {dimensions.Width}x{dimensions.Height}");
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"convert {inputImage} -resize {dimensions.Width}x{dimensions.Height}\\! PNG:{outputImage}\"",
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
            throw new Exception($"ImageMagick resize failed:\n{stderr}");
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