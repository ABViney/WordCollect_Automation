using System.Drawing;
using Serilog;
using WordCollect_Automated.Models;
using WordCollect_Automated.Services.InputSimulation;
using WordCollect_Automated.Services.InputSimulation.Interpolation;

namespace WordCollect_Automated.Services.WordCollect;

/// <summary>
/// Features for efficiently solving a level in WordCollect
/// </summary>
public class LevelSolver
{
    // Window details
    private string _windowName;
    private BoundingBox _windowBoundingBox;
    
    // Service for scanning screenshots for selectable letters and their locations
    private SelectableLetterParser _selectableLetterParser;
    private SolvableWordParser _solvableWordParser;
    
    // Set up a controller to take over the mouse and program inputs
    private MouseController _mouseController;
    
    // Fuzzies the point an input occurs at to make it look less robotic
    private BiasedRadialPointFuzzer _pointFuzzer;
    
    // Interpolates movement between points to smooth out touch movements
    private IStrokeInterpolator _pointLerp;

    public LevelSolver(string windowName)
    {
        Log.Logger.Debug("Constructing LevelSolver");
        
        _windowName = windowName;
        _windowBoundingBox = GnomeDesktop.GetWindowBoundingBox(_windowName);
        
        _selectableLetterParser = new SelectableLetterParser();
        _solvableWordParser = new SolvableWordParser();
        
        var desktopBoundingBox = GnomeDesktop.GetDesktopBoundingBox();
        _mouseController = new MouseController(desktopBoundingBox.Width, desktopBoundingBox.Height);
        
        _pointFuzzer = new BiasedRadialPointFuzzer(30);
        _pointLerp = new LinearStrokeInterpolator();
        
        Log.Logger.Debug("Constructed LevelSolver");
    }

    /// <summary>
    /// 
    /// </summary>
    public void Solve()
    {
        // Snap a picture of the current level
        ITemporaryFile screenshot = TemporaryDataManager.CreateTemporaryPNGFile();
        GnomeDesktop.ScreenshotWindow(_windowName, screenshot.Path);
        
        // Todo: Test to make sure that a level is presented and no popups are blocking the screen before continuing
        
        // Parse the letters from the screenshot
        SolvableWordPool solvableWordPool = _solvableWordParser.CreateSolvableWordPool(screenshot.Path);
        Console.WriteLine($"Solved words: " + String.Join(", ", solvableWordPool.SolvedWords));
        
        SelectableLetterPool selectableLetterPool = _selectableLetterParser.GetSelectableLetterPool(screenshot.Path);

        List<string> blacklistedWords = EnglishDictionary.GetBlacklistedWords();

        // Filter out a list of words to use for this level
        List<string> possibleWords = new();
        foreach (string potentialWord in selectableLetterPool.PotentialWords)
        {
            // If word has already been used or is blacklisted, don't use it
            if (solvableWordPool.HasWord(potentialWord) || blacklistedWords.Contains(potentialWord)) continue;
            // If the word is not a valid length for the puzzle don't use it
            if (potentialWord.Length < solvableWordPool.MinimumWordLength 
                || potentialWord.Length > solvableWordPool.MaximumWordLength) continue;
            
            possibleWords.Add(potentialWord);
        }
        
        Console.WriteLine($"Words to try: {String.Join(", ", possibleWords)}");
        
        // Using a stack so we don't reuse words
        Stack<string> wordsToTry = new(possibleWords);
        
        // I'm tired of thinking rn. This is a placeholder (watch it be a permanent solution) selectable letter for the selectable region. 
        SelectableLetter center = new SelectableLetter("", selectableLetterPool.Area);

        // The wagon wheel is built with selectable letters so I can feed it the result from building a word to get the routed path
        WagonWheelPathingGraph<SelectableLetter> inputPaths = CreateWagonWheelPathingGraph(center, selectableLetterPool.SelectableLetters);

        // Todo: Add persistent word blacklist
        // This loop is active while the user is trying to solve the puzzle
        bool puzzleIsSolved = false;
        while (!puzzleIsSolved || wordsToTry.Count == 0)
        {
            // No, the puzzle isn't solved
            // input next word
            string nextWordToTry = wordsToTry.Pop();
            
            InputWord(nextWordToTry, selectableLetterPool, inputPaths);
            
            // Wait a bit in case for tiles to finish moving in the solved words pool
            Thread.Sleep(2500);
            
            // Recapture the screen which will tell us if anything has changed in the state of the level
            GnomeDesktop.ScreenshotWindow(_windowName, screenshot.Path);
            
            // Todo: Check if level is interrupted
            ITemporaryFile cropOfAreaToCheckForIfALevelIsStillPresented = TemporaryDataManager.CreateTemporaryPNGFile();
            ImageProcessing.CropUsingBoundingBox(screenshot.Path, cropOfAreaToCheckForIfALevelIsStillPresented.Path, new BoundingBox(391, 32, 34, 571));
            double nrsme = ImageProcessing.NormalizedRootMeanSquareError(cropOfAreaToCheckForIfALevelIsStillPresented.Path, Path.ToLevelInProgressTemplate);

            if (nrsme > 0.1)
            {
                throw new ApplicationException(
                    $"The level has been determined to not be present at the moment.\n NRSME = {nrsme}");
            }
            
            // There are a lot of words that are valid in scrabble but invalid or 'extra' in this game.
            // To save time and repetition, we'll update our solved word pool every input
            // If a word was correct, then we do nothing.
            // If a word was incorrect, we blacklist it so we don't use it again in the future.
            bool validWord = _solvableWordParser.UpdateSolvableWordPool(screenshot.Path, solvableWordPool);
            if (validWord == false)
            {
                Console.WriteLine($"Blacklisting word: {nextWordToTry}");
                EnglishDictionary.AddBlacklistedWord(nextWordToTry);
            }
        }
    }

    /// <summary>
    /// Input a word into the game.
    /// </summary>
    /// <param name="word">The word to spell out</param>
    /// <param name="selectableLetterPool">The pool of selectable letters</param>
    /// <param name="wagonWheelPathingGraph">A model of the characters on screen</param>
    private void InputWord(string word, SelectableLetterPool selectableLetterPool, WagonWheelPathingGraph<SelectableLetter> wagonWheelPathingGraph)
    {
        Console.WriteLine($"Submitting word: {word}");
        
        // Pick which letters in the pool will be used to enter this word.
        IEnumerable<SelectableLetter> selectableLetterOrder = selectableLetterPool.BuildWord(word);
        
        // Get the points the cursor must pass through
        IEnumerable<Point> path = wagonWheelPathingGraph.ResolvePath(selectableLetterOrder).Select(sl =>
        {
            var selectableLetterBoundingBox = sl.BoundingBox;
            var normalizedBoundingBox = _windowBoundingBox.Normalize(selectableLetterBoundingBox);
            return _pointFuzzer.Fuzz(normalizedBoundingBox.Center);
        });
        
        // Create a route for the cursor to move
        Queue<Point> interpolatedPath = new (_pointLerp.Interpolate(path, 50));

        // Focus the window
        GnomeDesktop.FocusWindow(_windowName);
            
        Console.WriteLine($"Inputting: {word}");
        
        // Move to the first letter
        _mouseController.MoveTo(interpolatedPath.Dequeue());
        _mouseController.Press(MouseButton.Left);
        // Wait before beginning movements
        Thread.Sleep(Random.Shared.Next(100, 400));

        foreach (var point in interpolatedPath)
        {
            // Give the UI time to update, this app is slow
            int timeUntilMovement = Random.Shared.Next(3, 8);
            Thread.Sleep(timeUntilMovement);
            _mouseController.MoveTo(point);
        }
        
        // Wait before release
        Thread.Sleep(Random.Shared.Next(100, 300));
        _mouseController.Release(MouseButton.Left);
    }
    
    private WagonWheelPathingGraph<SelectableLetter> CreateWagonWheelPathingGraph(SelectableLetter intermediarySelectable,
        List<SelectableLetter> selectableLetters)
    {
        // The letters may have been pooled in any arbitrary order. In order to instantiate the wagon wheel graph, the 
        // order of the values that make up the rim must be traversable without the hub.
        var orderedSelectableLetters = selectableLetters.OrderBy(sl =>
        {
            var center = intermediarySelectable.BoundingBox.Center;
            var point = sl.BoundingBox.Center;
            return Math.Atan2(point.Y - center.Y, point.X - center.X);
        }).ToList();

        foreach (var orderedSelectableLetter in orderedSelectableLetters)
        {
            Console.WriteLine(orderedSelectableLetter.Letter);
        }

        return new WagonWheelPathingGraph<SelectableLetter>(intermediarySelectable, orderedSelectableLetters);
    }
}