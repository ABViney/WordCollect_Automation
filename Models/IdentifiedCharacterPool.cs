using WordCollect_Automated.Services;

namespace WordCollect_Automated.Models;

public class IdentifiedCharacterPool
{
    // Maps the character to an identified image file
    private Dictionary<string, string> _identifiedCharacters { get; }

    public IdentifiedCharacterPool()
    {
        _identifiedCharacters = new();
    }

    public void Add(string character, string imageFile)
    {
        if (!_identifiedCharacters.ContainsKey(character))
        {
            _identifiedCharacters[character] = imageFile;
        }
    }

    /// <summary>
    /// Attempts to identify the provided character against the pool of existing identified characters.
    /// Returns the normalized real mean square error of the comparison that had the closest match.
    /// Writes out the character of the closest match.
    /// </summary>
    /// <seealso cref="ImageProcessing.NormalizeRealSquareError"/> 
    /// <param name="unidentifiedCharacterImage"></param>
    /// <param name="character"></param>
    /// <returns></returns>
    public double TryIdentifyCharacter(string unidentifiedCharacterImage, out string character)
    {
        // The lower this number, the closer a match between the unknown character and a known character
        double closestMatchRMSE = 1.1;
        string closestCharacterMatch = "";

        BoundingBox unidentifiedCharacterImageDimensions =
            ImageProcessing.GetImageDimensions(unidentifiedCharacterImage);
        
        foreach ((string identifiedCharacter, string identifiedCharacterImageFile) in _identifiedCharacters)
        {
            // Resize the identified character to fit the unidentified character
            ITemporaryFile resizedIdentifiedCharacterImageFile = TemporaryDataManager.CreateTemporaryPNGFile();
            ImageProcessing.ResizeImage(identifiedCharacterImageFile, resizedIdentifiedCharacterImageFile.Path, unidentifiedCharacterImageDimensions);
            
            double nrmse = ImageProcessing.NormalizedRootMeanSquareError(unidentifiedCharacterImage, resizedIdentifiedCharacterImageFile.Path);
            if (nrmse < closestMatchRMSE)
            {
                closestMatchRMSE = nrmse;
                closestCharacterMatch = identifiedCharacter;
            }
            
            // Dispose the temporary file
            resizedIdentifiedCharacterImageFile.Dispose();
        }

        character = closestCharacterMatch;
        return closestMatchRMSE;
    }
}