using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services.WordCollect;

/// <summary>
/// Gleans information from the solved word section
/// </summary>
public class SolvableWordParser
{
    private IdentifiedCharacterPool _knownSolvedLetters = new();

    private const string _knownSolvedCharacterPrefix = "Solved_";

    public SolvableWordParser()
    {
        // Create character directory if doesn't exist
        if (!Directory.Exists(Path.ToCharacterImageRepository))
        {
            Directory.CreateDirectory(Path.ToCharacterImageRepository);
        }
        
        // Load known characters
        string[] characterFiles = Directory.GetFiles(Path.ToCharacterImageRepository);
        var knownSolvedLetterFiles = characterFiles.Where(filepath => System.IO.Path.GetFileName(filepath).StartsWith(_knownSolvedCharacterPrefix));

        // Populate solved letters
        foreach (string knownSolvedLetterFile in knownSolvedLetterFiles)
        {
            string letter = System.IO.Path.GetFileName(knownSolvedLetterFile).Substring(_knownSolvedCharacterPrefix.Length, 1);
            _knownSolvedLetters.Add(letter, knownSolvedLetterFile);
        }
    }

    /// <summary>
    /// Instantiates a new solvable word pool instance for the current pool of solvable words.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public SolvableWordPool CreateSolvableWordPool(string screenshot)
    {
        // Getting all the box data.
        // This is a pretty expensive operation to get all of this data. Fortunately, it only needs to be done once at
        // the start of a (new or partially completed) level. Subsequent screenshots can be checked for the first tile
        // of a word to see if they are solved or not before any character parsing needs to be done.
        
        ITemporaryFile maskedSolvableWords = TemporaryDataManager.CreateTemporaryPNGFile();
        ITemporaryFile contrastedMaskedSolvableWords = TemporaryDataManager.CreateTemporaryPNGFile();
        
        // Mask area of the screen with the solvable letter pool
        ImageProcessing.MaskImage(screenshot, maskedSolvableWords.Path, Path.ToSolvedWordsOverlay);
        
        // Increase contrast to differentiate dark pixels from light pixels
        ImageProcessing.RaiseContrast(maskedSolvableWords.Path, contrastedMaskedSolvableWords.Path);
        
        // Get components of the image. Skipping the first two entries excludes the mask and the tile background
        List<BoundingBox> solvedWordsBoxes = ImageProcessing
            .GetComponents(contrastedMaskedSolvableWords.Path)
            .Skip(2).ToList();
        
        // flattening the bounding boxes so only the ones representing a tile remain
        var tileBoxes = solvedWordsBoxes.Slice(0, solvedWordsBoxes.Count);
        tileBoxes.Sort((a, b) => b.Area.CompareTo(a.Area)); // Largest to smallest

        // Now to isolate the tiles containing characters
        // The largest components are boxes. Anything intersecting it is inside/part of the box.
        for (int i = 0; i < tileBoxes.Count - 1; i++)
        {
            for (int j = i + 1; j < tileBoxes.Count; j++)
            {
                if (tileBoxes[i].IsEncapsulating(tileBoxes[j]))
                {
                    // Removing these entries means they won't be checked later, so we'll only loop through actual boxes
                    tileBoxes.RemoveAt(j);
                    j--;
                }
            }
        }
        
        // Creating word groups from the boxes.
        // Words are laid out horizontally, with letterboxes that are part of the same word adjacent to each other.
        // Meta knowledge tells us that letter boxes for the same word are spaced around 3 pixels, while those for different
        // words are about 20 pixels apart.

        List<List<BoundingBox>> groupedBoxes = new(); // Each list is a word
        HashSet<BoundingBox> assignedBoxes = new(); // grouped boxes aren't revisited
        
        //////////////////////
        // Declaring functions
        //////////////////////
        
        // Defining some lambda functions to make later checks easier
        // Only need to test boxes on the same row
        Func<BoundingBox, BoundingBox, bool> IsSameRow = (a, b) =>
        {
            return a.Y < b.Y + b.Height // b isn't above a
                   && b.Y < a.Y + a.Height; // a isn't above b
        };
        // Checks if a tile is the next in a sequence based on its position to the current tile
        int adjacentBoxThreshold = 10; //Arbitrary assumption that adjacent boxes are less than 10 pixels apart
        Func<BoundingBox, BoundingBox, bool> IsTheNextTile = (a, b) =>
        {
            int right = b.X - (a.X + a.Width);
            return Math.Abs(b.Y - a.Y) < adjacentBoxThreshold // Boxes are in the same row
                   && right > 0 && right < adjacentBoxThreshold; // box b is to the direct right of box a
        };
        // Accepts a list of boxes. Gets the last box. Finds the next tile in sequence. 
        Func<List<BoundingBox>, List<BoundingBox>>? recursivelyGroupTiles = null; // declaring without assignment so I can recursively call this lambda 
        recursivelyGroupTiles = (group) =>
        {
            if (group.Count < 1) throw new ArgumentException($"{nameof(group)} is empty.");
            var currentBox = group.Last(); // Boxes are grouped from left to right
            foreach (var nextBox in tileBoxes) // iterating through candidates for the next box in the current sequence
            {
                if (assignedBoxes.Contains(nextBox) // Boxes in a group are not potential neighbors
                    || !IsSameRow(currentBox, nextBox) // Boxes can't be in the same group if they aren't on the same row
                    || !IsTheNextTile(currentBox, nextBox)) // Boxes that aren't right next to this one don't matter right now
                {
                    continue;
                }

                group.Add(nextBox); // Add the next box to this group
                assignedBoxes.Add(nextBox); // Mark it as assigned so it isn't checked again
                return recursivelyGroupTiles!(group); // this lambda is declared in the same local scope and is not used elsewhere so we know its assigned
            }

            return group; // lambdas have to return something. This just returns the same list from its parameter so no new allocation is made
        };

        //////////////////////////
        // END Declaring functions
        //////////////////////////
        
        // Sort boxes by their X values so the tile containing the first tile of a word is always encountered first
        tileBoxes.Sort((a, b) => a.X.CompareTo(b.X));
        
        foreach (var box in tileBoxes)
        {
            if (assignedBoxes.Contains(box)) continue; // Box is already part of a group so skip it
            
            List<BoundingBox> group = new List<BoundingBox>() { box }; // Boxes are sorted by X value so no need to append a box in an existing group
            assignedBoxes.Add(box); // Every box we iterate is the start of its group, so we don't need to check it at any point later.
            groupedBoxes.Add(recursivelyGroupTiles(group)); // recursively groups and orders boxes
        }

        SolvableWordPool solvableWordPool = new SolvableWordPool(groupedBoxes);

        // The preceeding code just gets the bounding boxes for the tiles in the solvable word pool. This method will
        // check if any of the tiles contain letters and update the pool to mark those words as complete.
        // The return value is useless here, but it has purpose elsewhere in discerning a change in state of the level.
        bool levelIsPartiallyComplete = UpdateSolvableWordPool(screenshot, solvableWordPool);

        return solvableWordPool;
    }
    
    /// <summary>
    /// Determines if a change has occured in the solved word pool and updates the object accordingly.
    /// </summary>
    /// <param name=""></param>
    /// <exception cref="NotImplementedException"></exception>
    public bool UpdateSolvableWordPool(string screenshot, SolvableWordPool solvableWordPool)
    {
        ITemporaryFile maskedSolvableWords = TemporaryDataManager.CreateTemporaryPNGFile();
        ITemporaryFile contrastedMaskedSolvableWords = TemporaryDataManager.CreateTemporaryPNGFile();
        
        // Mask area of the screen with the solvable letter pool
        ImageProcessing.MaskImage(screenshot, maskedSolvableWords.Path, Path.ToSolvedWordsOverlay);
        
        // Increase contrast to differentiate dark pixels from light pixels
        ImageProcessing.RaiseContrast(maskedSolvableWords.Path, contrastedMaskedSolvableWords.Path);
        
        // Tiles that are solved have a light background, whereas unsolved tiles have a dark background.
        // Post thresholding, the darker background becomes black pixels, whereas the lighter background becomes white.
        // This is threshold is an arbitrary assumption that a % of pixels on a solved tile will be white.
        double letterTileBrightnessThreshold = 0.5;
        
        // Get the list of unsolved words and test the first tile of each one to see if it contains a letter or not
        IReadOnlyList<IReadOnlyList<BoundingBox>> unsolvedWords = solvableWordPool.UnsolvedWordPositions;
        List<IReadOnlyList<BoundingBox>> solvedWords = new();
        foreach (var unsolvedWord in unsolvedWords)
        {
            // Before cropping out every tile, determine if this word hsa been solved.
            var firstTileBoundingBox = unsolvedWord[0];
            ITemporaryFile firstTile = TemporaryDataManager.CreateTemporaryPNGFile();
            ImageProcessing.CropUsingBoundingBox(contrastedMaskedSolvableWords.Path, firstTile.Path, firstTileBoundingBox);
            
            // Get the brightness of this tile. If it's greater than the brightness threshold, then this tile has a letter and the word is solved
            double brightnessValue = ImageProcessing.GetBrightness(firstTile.Path);
            if (brightnessValue > letterTileBrightnessThreshold)
            {
                solvedWords.Add(unsolvedWord);
            }
        }

        bool changeMade = false;
        if (solvedWords.Count > 0)
        {
            changeMade = true;
            foreach (var solvedWord in solvedWords)
            {
                // If the word is solved, crop out the rest of the tiles, mask them to remove the border, and determine 
                // their characters.
                ITemporaryFile resizedMask = TemporaryDataManager.CreateTemporaryPNGFile();
                // Resizing the mask to fit the solved tile bounds, as these sizes vary between levels
                ImageProcessing.ResizeImage(Path.ToSolvedTileOverlay, resizedMask.Path, solvedWord[0]);

                List<IdentifiedCharacter> characters = solvedWord.Select(tile =>
                    ParseCharacterFromTile(contrastedMaskedSolvableWords.Path, resizedMask.Path, tile)).ToList();

                solvableWordPool.AddSolvedWord(characters);
            }
        }

        return changeMade;
    }

    /// <summary>
    /// Parse the character from a tile.
    /// Doing this is kinda tedious--tile sizes can vary between levels and each tile is surrounded by a border that is
    /// not guaranteed to be the same size every or can cause the component to be sized weird.
    /// Overall, this is an expensive process and I don't really have a better idea of how to do it right now.
    /// </summary>
    /// <param name="postProcessedScreenshot">a screenshot that is, at the least, contrasted.</param>
    /// <param name="tileBoundingBox">the bounds information for the tile in the image</param>
    /// <param name="maskPath">a preconfigured mask image for isolating characters from the tile</param>
    /// <returns>the identified character initialized with its tile position.</returns>
    private IdentifiedCharacter ParseCharacterFromTile(string postProcessedScreenshot, string maskPath, BoundingBox tileBoundingBox)
    {
        ITemporaryFile tile = TemporaryDataManager.CreateTemporaryPNGFile();
        ImageProcessing.CropUsingBoundingBox(postProcessedScreenshot, tile.Path, tileBoundingBox);
        BoundingBox tileImageDimensions = ImageProcessing.GetImageDimensions(tile.Path);
        BoundingBox maskImageDimensions = ImageProcessing.GetImageDimensions(maskPath);
        // ImageDimensions are implicitly at (x,y) = (0,0) so if the width or height differ then resize the mask
        bool shouldResizeMask = !tileImageDimensions.IsEncapsulating(maskImageDimensions) 
                                || maskImageDimensions.IsEncapsulating(tileImageDimensions);

        ITemporaryFile maskedTile = TemporaryDataManager.CreateTemporaryPNGFile();
        if (shouldResizeMask)
        {
            ITemporaryFile resizedMask = TemporaryDataManager.CreateTemporaryPNGFile();
            ImageProcessing.ResizeImage(maskPath, resizedMask.Path, tileImageDimensions);
            ImageProcessing.MaskImage(tile.Path, maskedTile.Path, resizedMask.Path);
            resizedMask.Dispose();
        }
        else
        {
            ImageProcessing.MaskImage(tile.Path, maskedTile.Path, maskPath);
        }

        // Getting the components for 
        var characterBoundingBoxes = OCR.GetCharacterBoundingBoxes(maskedTile.Path);
        BoundingBox characterBoundingBox;
        if (characterBoundingBoxes.Count > 1)
        {
            // An artifact made it into the mask. Get the bigger box as that's probably the letter
            characterBoundingBoxes.Sort((a,b) => b.Area.CompareTo(a.Area)); // Big to small
        }
        characterBoundingBox = characterBoundingBoxes[0];
        
        // Store cropped characters in an arbitrary location the temporary data folder
        ITemporaryFile croppedCharacterFile = TemporaryDataManager.CreateTemporaryPNGFile();
        ImageProcessing.CropUsingBoundingBox(tile.Path, croppedCharacterFile.Path, characterBoundingBox);
        string character;
        double rmse = _knownSolvedLetters.TryIdentifyCharacter(croppedCharacterFile.Path, out character);
        if (rmse >= 0.3) // Using the same assumption from SelectableLetterParse, may need to tune this for the smaller images
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
                Path.ToCharacterImageRepository,
                $"{_knownSolvedCharacterPrefix}{character}.png");
            // Todo: Gonna try overwriting, but would prob be better just to expand my identified character pools
            File.Copy(
                croppedCharacterFile.Path,
                pathToKnownSelectableCharacterImageFile, 
                true);
                
            // add identified character to pool
            _knownSolvedLetters.Add(character, pathToKnownSelectableCharacterImageFile);
        }
        
        //dispose of the temporary file
        tile.Dispose();
        croppedCharacterFile.Dispose();

        return new IdentifiedCharacter(character, tileBoundingBox);
    }
}