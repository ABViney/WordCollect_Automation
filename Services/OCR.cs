using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services;

/* Optical Character Recognition */
public static class OCR
{
    private static Dictionary<string, string> _commonCharacterMismatches = new()
    {
        {"|", "I"} // Pipe is always I
    };
    
    /// <summary>
    /// Gets the bounding boxes for potential characters in an image.
    /// The image must be black and white and contain only characters.
    /// </summary>
    /// <param name="imageFile"></param>
    /// <returns></returns>
    public static List<BoundingBox> GetCharacterBoundingBoxes(string imageFile)
    {
        List<BoundingBox> boxes = ImageProcessing.GetComponents(imageFile).Select(box =>
        {
            // Add padding to the bounding boxes to help tesseract-ocr with id
            (int x, int y, int width, int height) = (box.X, box.Y, box.Width, box.Height);
            int padding = 3; // pixels
            x -= padding;
            y -= padding;
            width += padding*2;
            height += padding*2;
            return new BoundingBox(width, height, x, y);
        }).ToList();
        
        // Sort largest to smallest (area)
        boxes.Sort((a, b) => b.Area.CompareTo(a.Area));
        // Remove the background component
        boxes.RemoveAt(0);

        // Remove erroneous bounding boxes that represent encapsulated spcae in the characters
        for (int i = 0; i < boxes.Count - 1; i++)
        {
            for (int j = i + 1; j < boxes.Count; j++)
            {
                if (boxes[i].IsEncapsulating(boxes[j]))
                {
                    boxes.RemoveAt(j);
                    j--;
                }
            }
        }
        
        return boxes;
    }

    /// <summary>
    /// Attempts to identify the 
    /// </summary>
    /// <param name="imageFile"></param>
    /// <returns></returns>
    public static string IdentifyCharacter(string imageFile)
    {
        // using self-contained appimage for tesseract
        // use tesseract-ocr to parse the image file while set to identify a single character
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{Path.ToTesseractExe} {imageFile} stdout --psm 10\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new ApplicationException($"Tesseract was unable to identify {imageFile}. Result was [{output}]. Please identify this character manually.");
        }

        if (_commonCharacterMismatches.ContainsKey(output))
            output = _commonCharacterMismatches[output];
        
        return output;
    }
}