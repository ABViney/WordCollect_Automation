using System.Drawing;
using WordCollect_Automated.Models;
using WordCollect_Automated.Services.InputSimulation;

namespace WordCollect_Automated.Services.WordCollect;

/// <summary>
/// Features for efficiently solving a level in WordCollect
/// </summary>
public class LevelSolver
{
    private string _window;
    private BoundingBox _windowBoundingBox;
    private SelectableLetterParser _selectableLetterParser;
    private MouseController _mouseController;

    public LevelSolver(string window)
    {
        _window = window;
        _windowBoundingBox = GnomeDesktop.GetWindowBoundingBox(_window);
        _selectableLetterParser = new SelectableLetterParser();
        var desktopBoundingBox = GnomeDesktop.GetDesktopBoundingBox();
        _mouseController = new MouseController(desktopBoundingBox.Width, desktopBoundingBox.Height);
    }

    /// <summary>
    /// 
    /// </summary>
    public void Solve()
    {
        // Snap a picture of the current level
        ITemporaryFile screenshot = TemporaryDataManager.CreateTemporaryFile();
        GnomeDesktop.ScreenshotWindow(_window, screenshot.Path);
        
        // Todo: Test to make sure that a level is presented and no popups are blocking the screen before continuing
        
        // Parse the letters from the screenshot
        SelectableLetterPool selectableLetterPool = _selectableLetterParser.GetSelectableLetterPool(screenshot.Path);
        // Using a stack so we don't reuse words and start with the largest.
        Stack<string> potentialWords = new(selectableLetterPool.PotentialWords);
        
        // I'm tired of thinking rn. This is a placeholder (watch it be a permanent solution) selectable letter for the selectable region. 
        SelectableLetter center = new SelectableLetter("", selectableLetterPool.Area);

        // The wagon wheel is built with selectable letters so I can feed it the result from building a word to get the routed path
        WagonWheelPathingGraph<SelectableLetter> inputPaths = CreateWagonWheelPathingGraph(center, selectableLetterPool.SelectableLetters);

        // Fuzzies the point an input occurs at to make it look less robotic
        BiasedRadialPointFuzzer brpf = new BiasedRadialPointFuzzer(30);

        // Interpolates movement between points to smooth out touch movements
        IStrokeInterpolator lerp = new LinearStrokeInterpolator();
        
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
            Console.WriteLine($"Submitting word: {nextWordToTry}");
            
            // Pick which letters in the pool will be used to enter this word.
            IEnumerable<SelectableLetter> selectableLetterOrder = selectableLetterPool.BuildWord(nextWordToTry);
            
            // Get the points the cursor must pass through
            IEnumerable<Point> path = inputPaths.ResolvePath(selectableLetterOrder).Select(sl =>
            {
                var selectableLetterBoundingBox = sl.BoundingBox;
                var normalizedBoundingBox = _windowBoundingBox.Normalize(selectableLetterBoundingBox);
                return brpf.Fuzz(normalizedBoundingBox.Center);
            });
            
            // Create a route for the cursor to move
            Queue<Point> interpolatedPath = new (lerp.Interpolate(path, 50));

            // Focus the window
            GnomeDesktop.FocusWindow(_window);
            
            // Move to the first letter
            _mouseController.MoveTo(interpolatedPath.Dequeue());
            _mouseController.Press(MouseButton.Left);
            // Wait before beginning movements
            Thread.Sleep(Random.Shared.Next(600, 1000));

            foreach (var checkpoint in checkpoints)
            {
                // Give the UI time to update, this app is slow
                int timeUntilMovement = Random.Shared.Next(300, 700);
                Thread.Sleep(timeUntilMovement);
                _mouseController.MoveTo(checkpoint);
            }
            // Wait before release
            Thread.Sleep(Random.Shared.Next(600, 1000));
            _mouseController.Release(MouseButton.Left);
            
            // Wait a bit if the word is filling in the solved words pool
            Thread.Sleep(Random.Shared.Next(1500, 4000));
        }
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