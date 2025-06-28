using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services;

/* Optical Character Recognition */
public static class OCR
{
    public static List<BoundingBox> GetCharacterBoundingBoxes(string imageFile)
    {
        // Takes a file, gets the connected components in the file, removes the background entry, and isolates the
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
                Arguments = $"-c \"convert {imageFile} -define connected-components:verbose=true -define connected-components:exclude-header=true -connected-components 4 -auto-level null: | grep -vwE '^(  0:)' | awk '{{ print $2 }}'\"",
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
                
                // Add padding to the bounding boxes to help tesseract-ocr with id
                int padding = 3; // pixels
                x -= padding;
                y -= padding;
                width += padding*2;
                height += padding*2;
                
                boxes.Add(new BoundingBox(width, height, x, y));
            }
        }
        
        // Sort largest to smallest (area)
        boxes.Sort((a, b) => b.Area.CompareTo(a.Area));

        // Remove erroneous bounding boxes that represent encapsulated spcae in the characters
        for (int i = 0; i < boxes.Count - 1; i++)
        {
            for (int j = i + 1; j < boxes.Count; j++)
            {
                if (boxes[i].Encapsulates(boxes[j]))
                {
                    boxes.RemoveAt(j);
                    j--;
                }
            }
        }
        
        return boxes;
    }

    public static string IdentifyCharacter(string imageFile)
    {
        // using self-contained appimage for tesseract
        
        // use tesseract-ocr to parse the image file while set to identify a single character
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{Path.ToAssets}/tesseract* {imageFile} stdout --psm 10\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
}