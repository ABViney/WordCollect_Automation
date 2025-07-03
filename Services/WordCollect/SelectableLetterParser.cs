using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services.WordCollect;

/// <summary>
/// Gets information about the letters the user is meant to use to solve a level.
/// </summary>
public class SelectableLetterParser
{
    private IdentifiedCharacterPool _knownSelectableLetters = new();

    private const string _knownSelectableCharacterPrefix = "Selectable_";

    /// <summary>
    /// Letters in <see cref="Path.CharacterImageRepository"/> must be prefixed with the appropriate label to be
    /// included in the appropriate <see cref="IdentifiedCharacterPool"/>.
    /// "Selectable_(letter).png" for selectable letters.
    /// "Solved_(letter).png" for the solved letters.
    /// </summary>
    public SelectableLetterParser()
    {
        // Create character directory if doesn't exist
        if (!Directory.Exists(Path.CharacterImageRepository))
        {
            Directory.CreateDirectory(Path.CharacterImageRepository);
        }
        // Load known characters
        string[] characterFiles = Directory.GetFiles(Path.CharacterImageRepository);
        var knownSelectableLetterFiles = characterFiles.Where(filepath => System.IO.Path.GetFileName(filepath).StartsWith(_knownSelectableCharacterPrefix));
        
        // Populate selectable letters
        foreach (string knownSelectableLetterFile in knownSelectableLetterFiles)
        {
            string letter = System.IO.Path.GetFileName(knownSelectableLetterFile).Substring(_knownSelectableCharacterPrefix.Length, 1);
            _knownSelectableLetters.Add(letter, knownSelectableLetterFile);
        }
    }

    /// <summary>
    /// Parses a screenshot for selectable letters.
    /// </summary>
    /// <param name="screenshot"></param>
    /// <returns></returns>
    public SelectableLetterPool GetSelectableLetterPool(string screenshot)
    {
        ITemporaryFile maskedImage = TemporaryDataManager.CreateTemporaryPNGFile();
        ITemporaryFile contrastedMaskedImage = TemporaryDataManager.CreateTemporaryPNGFile();
        
        // Mask the area of screen that has selectable letters
        ImageProcessing.MaskImage(Path.ToLetterPoolOverlay, screenshot, maskedImage.Path);
        // Increase contrast so letters are white and background is black
        ImageProcessing.RaiseContrast(maskedImage.Path, contrastedMaskedImage.Path);
        // Get the bounding boxes for the characters on screen
        var chracterBoundingBoxes = OCR.GetCharacterBoundingBoxes(contrastedMaskedImage.Path);
        
        // Each letter is cropped out and labeled pending identification
        List<SelectableLetter> identifiedSelectableLetters = new();
        foreach (var boundingBox in chracterBoundingBoxes)
        {
            // Store cropped characters in an arbitrary location the temporary data folder
            ITemporaryFile croppedCharacterFile = TemporaryDataManager.CreateTemporaryPNGFile();
            ImageProcessing.CropUsingBoundingBox(contrastedMaskedImage.Path, croppedCharacterFile.Path, boundingBox);
            string character; // 
            double rmse = _knownSelectableLetters.TryIdentifyCharacter(croppedCharacterFile.Path, out character);
            if (rmse >= 0.3) // Arbitrary assumption that an accurate match will have less than a 30% difference between both images
            {
                // If we aren't confident in our match, then identify the character with OCR
                // scale up to help with recognition
                ITemporaryFile enlargedCroppedCharacterFile = TemporaryDataManager.CreateTemporaryPNGFile();
                ImageProcessing.ScaleImage(croppedCharacterFile.Path, enlargedCroppedCharacterFile.Path, 3.0);
                // use optical character recognition to identify the character
                character = OCR.IdentifyCharacter(croppedCharacterFile.Path);
                enlargedCroppedCharacterFile.Dispose();
                
                // Copy image of identified character to appropriate directory 
                string pathToKnownSelectableCharacterImageFile = System.IO.Path.Combine(
                    Path.CharacterImageRepository,
                    $"{_knownSelectableCharacterPrefix}{character}.png");
                File.Copy(
                    croppedCharacterFile.Path,
                    pathToKnownSelectableCharacterImageFile);
                
                // add identified character to pool
                _knownSelectableLetters.Add(character, pathToKnownSelectableCharacterImageFile);
            }
            
            // Dispose of the temporary file
            croppedCharacterFile.Dispose();
            
            // Add identified character to current pool
            identifiedSelectableLetters.Add(new SelectableLetter(character, boundingBox));
        }
        
        // Dispose of temporary files
        maskedImage.Dispose();
        contrastedMaskedImage.Dispose();

        return new SelectableLetterPool(identifiedSelectableLetters);
    }
}