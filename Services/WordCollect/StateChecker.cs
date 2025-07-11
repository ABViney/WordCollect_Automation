using System.Text.RegularExpressions;
using WordCollect_Automated.Models;

namespace WordCollect_Automated.Services.WordCollect;

/// <summary>
/// A service for discerning the current state of the game based on a screenshot.
/// Uses template matching to determine the current state of the application. Information gleaned from this service
/// can be used to determine what the next input should be.
/// </summary>
public class StateChecker
{
    private Window _window;
    private Dictionary<string, GameState> templateToState;
    private Dictionary<string, BoundingBox> templateToRegion;
    
    
    public StateChecker()
    {
        templateToState = new Dictionary<string, GameState>
        {
            { Path.ToAppleTournamentPregameTemplate,   GameState.AppleTournamentPregame },
            { Path.ToEndlessDealsTemplate,             GameState.EndlessDeals },
            { Path.ToDifficultSettingTemplate,         GameState.DifficultSetting },
            { Path.ToLevelCompleteTemplate,            GameState.LevelComplete },
            { Path.ToLevelCompleteRewardTemplate,      GameState.LevelComplete },
            { Path.ToLevelInProgressTemplate,          GameState.LevelPresented },
            { Path.ToOutOfFirefliesTemplate,           GameState.OutOfFireflies },
            { Path.ToPiggyBankFullTemplate,            GameState.PiggyBankFull },
            { Path.ToSummerBloomsJigsawPuzzleTemplate, GameState.SummerBloomsJigsawPuzzle },
            { Path.ToTwilightTreatsTemplate,           GameState.TwilightTreats },
            { Path.ToWelcomeBasketTemplate,            GameState.WelcomeBasket },
            { Path.ToWildWordEventTemplate,            GameState.WildWordEvent }
        };
        templateToRegion = new Dictionary<string, BoundingBox>
        {
            { Path.ToAppleTournamentPregameTemplate,   GetBoundingBoxForTemplate(Path.ToAppleTournamentPregameTemplate) },
            { Path.ToEndlessDealsTemplate,             GetBoundingBoxForTemplate(Path.ToEndlessDealsTemplate) },
            { Path.ToDifficultSettingTemplate,         GetBoundingBoxForTemplate(Path.ToDifficultSettingTemplate) },
            { Path.ToLevelCompleteTemplate,            GetBoundingBoxForTemplate(Path.ToLevelCompleteTemplate) },
            { Path.ToLevelCompleteRewardTemplate,      GetBoundingBoxForTemplate(Path.ToLevelCompleteRewardTemplate) },
            { Path.ToLevelInProgressTemplate,          GetBoundingBoxForTemplate(Path.ToLevelInProgressTemplate) },
            { Path.ToOutOfFirefliesTemplate,           GetBoundingBoxForTemplate(Path.ToOutOfFirefliesTemplate) },
            { Path.ToPiggyBankFullTemplate,            GetBoundingBoxForTemplate(Path.ToPiggyBankFullTemplate) },
            { Path.ToSummerBloomsJigsawPuzzleTemplate, GetBoundingBoxForTemplate(Path.ToSummerBloomsJigsawPuzzleTemplate) },
            { Path.ToTwilightTreatsTemplate,           GetBoundingBoxForTemplate(Path.ToTwilightTreatsTemplate) },
            { Path.ToWelcomeBasketTemplate,            GetBoundingBoxForTemplate(Path.ToWelcomeBasketTemplate) },
            { Path.ToWildWordEventTemplate,            GetBoundingBoxForTemplate(Path.ToWildWordEventTemplate) }
        };
    }

    /// <summary>
    /// Attempts to discern the game state of a screenshot by testing it against known templates.
    /// </summary>
    /// <param name="screenshot"></param>
    /// <returns></returns>
    public GameState CheckState(string screenshot)
    {
        foreach ((string templatePath, BoundingBox region) in templateToRegion)
        {
            // If the template matches the region of the screenshot then the value returned is 0
            double nrmse = ImageProcessing.NormalizedRootMeanSquareError(screenshot, templatePath, region);
            if (nrmse <= 0.1)
            {
                return templateToState[templatePath];
            }
        }

        // If no match was found, the game state is unknown
        return GameState.Unknown;
    }
    
    private BoundingBox GetBoundingBoxForTemplate(string templateFileName)
    {
        var regex = new Regex(@"_(\d+)x(\d+)\+(\d+)\+(\d+)");
        var match = regex.Match(templateFileName);
        if (match.Success)
        {
            int width = int.Parse(match.Groups[1].Value);
            int height = int.Parse(match.Groups[2].Value);
            int x = int.Parse(match.Groups[3].Value);
            int y = int.Parse(match.Groups[4].Value);
                
            return new BoundingBox(width, height, x, y);
        }

        throw new ArgumentException($"Unable to parse width/height/x/y info from {templateFileName}");
    }
}