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
    private Window _window;
    
    // Set up a controller to take over the mouse and program inputs
    private MouseController _mouseController;
    
    // Fuzzies the point an input occurs at to make it look less robotic
    private BiasedRadialPointFuzzer _pointFuzzer;
    
    // Interpolates movement between points to smooth out touch movements
    private IStrokeInterpolator _pointLerp;

    public LevelSolver(Window window, MouseController mouseController)
    {
        Log.Logger.Debug("Constructing LevelSolver");

        _window = window;
        _mouseController = mouseController;
        
        _pointFuzzer = new BiasedRadialPointFuzzer(30);
        _pointLerp = new LinearStrokeInterpolator();
        
        Log.Logger.Debug("Constructed LevelSolver");
    }

    /// <summary>
    /// Input a word into the game.
    /// </summary>
    /// <param name="word">The word to spell out</param>
    /// <param name="selectableLetterPool">The pool of selectable letters</param>
    /// <param name="wagonWheelPathingGraph">A model of the characters on screen</param>
    public void InputWord(string word, SelectableLetterPool selectableLetterPool, WagonWheelPathingGraph<SelectableLetter> wagonWheelPathingGraph, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {

            Console.WriteLine($"Submitting word: {word}");

            // Pick which letters in the pool will be used to enter this word.
            IEnumerable<SelectableLetter> selectableLetterOrder = selectableLetterPool.BuildWord(word);

            // Get the points the cursor must pass through
            IEnumerable<Point> path = wagonWheelPathingGraph.ResolvePath(selectableLetterOrder).Select(sl =>
            {
                var selectableLetterBoundingBox = sl.BoundingBox;
                var normalizedBoundingBox = _window.BoundingBox.Normalize(selectableLetterBoundingBox);
                return _pointFuzzer.Fuzz(normalizedBoundingBox.Center);
            });

            // Create a route for the cursor to move
            Queue<Point> interpolatedPath = new(_pointLerp.Interpolate(path, 50));

            // Focus the window
            _window.Focus();

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
        catch (OperationCanceledException e)
        {
            _mouseController.Release(MouseButton.Left); // Don't hijack my mouse button plz
            throw;
        }
    }
}