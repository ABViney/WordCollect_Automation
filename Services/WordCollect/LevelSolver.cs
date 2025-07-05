using System.Drawing;
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
        
        _windowName = windowName;
        _windowBoundingBox = GnomeDesktop.GetWindowBoundingBox(_windowName);
        
        _selectableLetterParser = new SelectableLetterParser();
        _solvableWordParser = new SolvableWordParser();
        
        var desktopBoundingBox = GnomeDesktop.GetDesktopBoundingBox();
        _mouseController = new MouseController(desktopBoundingBox.Width, desktopBoundingBox.Height);
        
        _pointFuzzer = new BiasedRadialPointFuzzer(30);
        _pointLerp = new LinearStrokeInterpolator();
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
        SelectableLetterPool selectableLetterPool = _selectableLetterParser.GetSelectableLetterPool(screenshot.Path);
        SolvableWordPool solvableWordPool = _solvableWordParser.CreateSolvableWordPool(screenshot.Path);
        
        // Using a stack so we don't reuse words and start with the largest.
        Stack<string> potentialWords = new(selectableLetterPool.PotentialWords.Where(pw => !solvableWordPool.HasWord(pw)));
        
        // I'm tired of thinking rn. This is a placeholder (watch it be a permanent solution) selectable letter for the selectable region. 
        SelectableLetter center = new SelectableLetter("", selectableLetterPool.Area);

        // The wagon wheel is built with selectable letters so I can feed it the result from building a word to get the routed path
        WagonWheelPathingGraph<SelectableLetter> inputPaths = CreateWagonWheelPathingGraph(center, selectableLetterPool.SelectableLetters);

        // Todo: Add persistent word blacklist
        // This loop is active while the user is trying to solve the puzzle
        bool puzzleIsSolved = false;
        while (!puzzleIsSolved)
        {
            Console.Write("Is the puzzle solved yet? (y/n): ");
            string input = String.Empty;
            bool validInput = false;
            // Getting keyboard input. This provides a break where the application isn't capturing the mouse.
            while (!validInput)
            {
                input = Console.ReadKey().KeyChar.ToString().ToUpper();
                if (String.Equals(input, "N") || String.Equals(input, "Y"))
                {
                    validInput = true;
                }
            }
            if (String.Equals(input, "Y"))
            {
                // yes, the puzzle is solved, we're done here
                puzzleIsSolved = true;
                continue;
            }

            // No, the puzzle isn't solved
            // input next word
            string nextWordToTry = potentialWords.Pop();
            
            InputWord(nextWordToTry, selectableLetterPool, inputPaths);
            
            // Wait a bit if the word is filling in the solved words pool
            Thread.Sleep(Random.Shared.Next(800, 1500));
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
        Thread.Sleep(Random.Shared.Next(200, 500));

        foreach (var point in interpolatedPath)
        {
            // Give the UI time to update, this app is slow
            int timeUntilMovement = Random.Shared.Next(5, 15);
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