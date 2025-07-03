using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services;

/* Optical Character Recognition */
public static class OCR
{
    /// <summary>
    /// Gets the bounding boxes for potential characters in an image.
    /// The image must be black and white and contain only characters.
    /// </summary>
    /// <param name="imageFile"></param>
    /// <returns></returns>
    public static List<BoundingBox> GetCharacterBoundingBoxes(string imageFile)
    {
        List<BoundingBox> boxes = ImageProcessing.GetComponents(imageFile);
        
        // Sort largest to smallest (area)
        boxes.Sort((a, b) => b.Area.CompareTo(a.Area));

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
            throw new ApplicationException($"Tesseract was unable to identify {imageFile}. Result was [{output}]");
        }
        return output;
    }
}