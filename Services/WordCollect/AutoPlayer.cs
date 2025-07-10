using WordCollect_Automated.Models;
using WordCollect_Automated.Services.InputSimulation;

namespace WordCollect_Automated.Services.WordCollect;

/// <summary>
/// This class charters all the services needed to automate playing this game. It provides the interface for starting
/// and stopping the application.
/// </summary>
public class AutoPlayer
{
    private const int MAX_TRIES = 3;
    private const int MIN_MILLISECONDS_TO_WAIT = 2000;
    private const int MAX_MILLISECONDS_TO_WAIT = 3200;
    
    private int MillisecondsToWait => Random.Shared.Next(MAX_MILLISECONDS_TO_WAIT, MAX_MILLISECONDS_TO_WAIT);
    
    private Window _window;
    private MouseController _mouseController;
    
    private StateChecker _stateChecker;
    private ButtonPresser _buttonPresser;
    
    private SolvableWordParser _solvableWordParser;
    private SelectableLetterParser _selectableLetterParser;

    private LevelSolver _levelSolver;

    private List<string> _blacklistedWords;
    
    public AutoPlayer(Window window, MouseController mouseController)
    {
        _window = window;
        _mouseController = mouseController;

        _stateChecker = new StateChecker();
        _buttonPresser = new ButtonPresser(_window, _mouseController);
        
        _solvableWordParser = new SolvableWordParser();
        _selectableLetterParser = new SelectableLetterParser();
        
        _levelSolver = new LevelSolver(_window, _mouseController);

        _blacklistedWords = EnglishDictionary.GetBlacklistedWords();
    }

    private void SetupToSolveLevel(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        
    }
    
    public void PlayGame(CancellationToken cancellationToken)
    {
        // Allow cancellation
        cancellationToken.ThrowIfCancellationRequested();

        // This outer loop runs in perpetuity until something goes wrong or the user requests to stop
        while (cancellationToken.CanBeCanceled)
        {
            // Before trying to solve a level, figure out whats on screen
            ITemporaryFile screenshot = _window.TakeScreenshot();
            
            // Make sure there is a level
            GameState state = DiscernWhatsOnScreen(ref screenshot, cancellationToken);

            // if a level isn't presented, then it's a popup or the level is over. Press the appropriate button and
            // start over
            if (state != GameState.LevelPresented)
            {
                _buttonPresser.PressButtonFor(state, cancellationToken);
                screenshot.Dispose(); // release resources
                Thread.Sleep(MillisecondsToWait);
                continue;
            }
            
            // if we're at this point, then a level is presented, so its time to build the word pools

            SolvableWordPool solvableWordPool = _solvableWordParser.CreateSolvableWordPool(screenshot.Path);
            SelectableLetterPool selectableLetterPool = _selectableLetterParser.GetSelectableLetterPool(screenshot.Path);

            // Filter out a list of words to use for this level
            List<string> possibleWords = new();
            foreach (string potentialWord in selectableLetterPool.PotentialWords)
            {
                // If word has already been used or is blacklisted, don't use it
                if (solvableWordPool.HasWord(potentialWord) || _blacklistedWords.Contains(potentialWord)) continue;
                // If the word is not a valid length for the puzzle don't use it
                if (potentialWord.Length < solvableWordPool.MinimumWordLength 
                    || potentialWord.Length > solvableWordPool.MaximumWordLength) continue;
            
                possibleWords.Add(potentialWord);
            }
            
            Console.WriteLine($"Minimum word length: {solvableWordPool.MinimumWordLength}");
            Console.WriteLine($"Maximum word length: {solvableWordPool.MaximumWordLength}");
            Console.WriteLine($"Trying words {String.Join(", ", possibleWords)}");
            
            // // I'm tired of thinking rn. This is a placeholder (watch it be a permanent solution) selectable letter for the selectable region.
            // Lol it became a permanent solution
            SelectableLetter center = new SelectableLetter("", selectableLetterPool.Area);
            
            // The wagon wheel is built with selectable letters so I can feed it the result from building a word to get the routed path
            WagonWheelPathingGraph<SelectableLetter> wagonWheelPathingGraph = CreateWagonWheelPathingGraph(center, selectableLetterPool.SelectableLetters, cancellationToken);

            // Iterating through each word in the possible word pool until the puzzle is solved
            for (int i = 0; i < possibleWords.Count; i++)
            {
                _levelSolver.InputWord(possibleWords[i], selectableLetterPool, wagonWheelPathingGraph, cancellationToken);
                Thread.Sleep(MillisecondsToWait); // Wait for apples and tiles to stop flying around
                
                // Take a new screenshot
                screenshot.Dispose();
                screenshot = _window.TakeScreenshot();
                
                // Figure out whats happenin
                state = DiscernWhatsOnScreen(ref screenshot, cancellationToken);

                if (state == GameState.LevelCompleted)
                {
                    // levels over, break from this loop so we can move on to the next level
                    break;
                }

                if (state != GameState.LevelPresented)
                {
                    // A popup appeared. Clear it and retry submitting the word in case it didn't go through
                    do
                    {
                        _buttonPresser.PressButtonFor(state, cancellationToken);
                        state = DiscernWhatsOnScreen(ref screenshot, cancellationToken);
                    } while (state != GameState.LevelPresented);
                    i--;
                    continue;
                }
                
                // Check if the submitted word was valid
                bool validWord = _solvableWordParser.UpdateSolvableWordPool(screenshot.Path, solvableWordPool);

                if (!validWord)
                {
                    // if the word was invalid, then blacklist it so it isn't tried again
                    _blacklistedWords.Add(possibleWords[i]);
                    EnglishDictionary.AddBlacklistedWord(possibleWords[i]);
                }
                
                
            }
        }
        
    }

    private GameState DiscernWhatsOnScreen(ref ITemporaryFile screenshot, CancellationToken cancellationToken)
    {
        for (int i = 0; i < MAX_TRIES; i++)
        {
            GameState state = _stateChecker.CheckState(screenshot.Path);
            if (state != GameState.Unknown)
            {
                return state;
            }
            screenshot.Dispose();
            Thread.Sleep(MillisecondsToWait);
            screenshot = _window.TakeScreenshot();
        }

        // Something went so terribly wrong. I didn't account for a #th popup!
        throw new ApplicationException($"Unable to discern the state from {screenshot.Path}");
    }
    
    private WagonWheelPathingGraph<SelectableLetter> CreateWagonWheelPathingGraph(SelectableLetter intermediarySelectable,
        List<SelectableLetter> selectableLetters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
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