using System.Drawing;
using WordCollect_Automated.Models;
using WordCollect_Automated.Services.InputSimulation;

namespace WordCollect_Automated.Services.WordCollect;

/// <summary>
/// Encapsulates input sequence presets for pressing buttons so we can get back to solving levels.
/// </summary>
public class ButtonPresser
{
    private Window _window;
    private MouseController _mouseController;

    private RectangularPointFuzzer _rpf;
    
    // where the "next level" button is on the level completed screen
    private BoundingBox _levelCompleteButton;
    private BoundingBox _levelCompleteRewardButton;

    private BoundingBox _appleTournamentPregameCloseButton;
    private BoundingBox _difficultSettingCloseButton;
    private BoundingBox _endlessDealsCloseButton;
    private BoundingBox _outOfFirefliesCloseButton;
    private BoundingBox _piggyBankFullCloseButton;
    private BoundingBox _summerBloomsJigsawPuzzleCloseButton;
    private BoundingBox _twilightTreatsCloseButton;
    private BoundingBox _welcomeBasketCloseButton;
    private BoundingBox _wildWordEventCloseButton;
    
    public ButtonPresser(Window window, MouseController mouseController)
    {
        _window = window;
        _mouseController = mouseController;

        _rpf = new RectangularPointFuzzer();
        
        // These bounding boxes are for where the apt button is in the image. They need to be normalized to the desktop.
        _levelCompleteButton = _window.BoundingBox.Normalize(new BoundingBox(90, 20, 185, 795));
        _levelCompleteRewardButton = _window.BoundingBox.Normalize(new BoundingBox(98, 26, 178, 766));
        _appleTournamentPregameCloseButton = _window.BoundingBox.Normalize(new BoundingBox(85, 24, 186, 558));
        _difficultSettingCloseButton = _window.BoundingBox.Normalize(new BoundingBox(20, 21, 389, 308));
        _endlessDealsCloseButton = _window.BoundingBox.Normalize(new BoundingBox(27, 26, 393, 113));
        _outOfFirefliesCloseButton = _window.BoundingBox.Normalize(new BoundingBox(21, 20, 388, 299));
        _piggyBankFullCloseButton = _window.BoundingBox.Normalize(new BoundingBox(20, 19, 388, 234));
        _summerBloomsJigsawPuzzleCloseButton = _window.BoundingBox.Normalize(new BoundingBox(18, 17, 398, 266));
        _twilightTreatsCloseButton = _window.BoundingBox.Normalize(new BoundingBox(21, 23, 403, 238));
        _welcomeBasketCloseButton = _window.BoundingBox.Normalize(new BoundingBox(22, 22, 403, 237));
        _wildWordEventCloseButton = _window.BoundingBox.Normalize(new BoundingBox(19, 18, 394, 337));
    }

    public void PressButtonFor(GameState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        try
        {
            // Ensure the left button is not pressed
            _mouseController.Release(MouseButton.Left);
            // Move the cursor to the correct region for this state
            BoundingBox buttonBoundingBox;
            switch (state)
            {
                case GameState.AppleTournamentPregame:
                    buttonBoundingBox = _appleTournamentPregameCloseButton;
                    break;
                case GameState.DifficultSetting:
                    buttonBoundingBox = _difficultSettingCloseButton;
                    break;
                case GameState.EndlessDeals:
                    buttonBoundingBox = _endlessDealsCloseButton;
                    break;
                case GameState.LevelComplete:
                    buttonBoundingBox = _levelCompleteButton;
                    break;
                case GameState.LevelCompleteReward:
                    buttonBoundingBox = _levelCompleteRewardButton;
                    break;
                case GameState.OutOfFireflies:
                    buttonBoundingBox = _outOfFirefliesCloseButton;
                    break;
                case GameState.PiggyBankFull:
                    buttonBoundingBox = _piggyBankFullCloseButton;
                    break;
                case GameState.SummerBloomsJigsawPuzzle:
                    buttonBoundingBox = _summerBloomsJigsawPuzzleCloseButton;
                    break;
                case GameState.TwilightTreats:
                    buttonBoundingBox = _twilightTreatsCloseButton;
                    break;
                case GameState.WelcomeBasket:
                    buttonBoundingBox = _welcomeBasketCloseButton;
                    break;
                case GameState.WildWordEvent:
                    buttonBoundingBox = _wildWordEventCloseButton;
                    break;
                default:
                    throw new ArgumentException($"Gamestate {state.ToString()} not supported.");
            }

            Point position = _rpf.Fuzz(buttonBoundingBox);
            _mouseController.MoveTo(position);
            _window.Focus();
            _mouseController.Press(MouseButton.Left);
            // Wait a random amount of time for a "realistic" button press
            Thread.Sleep(Random.Shared.Next(80, 194));
            _mouseController.Release(MouseButton.Left);
        }
        catch (OperationCanceledException e)
        {
            // ensure the mouse button is released
            _mouseController.Release(MouseButton.Left);
            throw;
        }
    }
}