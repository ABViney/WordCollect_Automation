using System.Reflection;

namespace WordCollect_Automated;

public static class Path
{
    /////////////
    // Folders //
    /////////////
    public static string ToAssets =>
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "assets");
    
    public static string ToTemporaryData => 
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "temp/");
    
    public static string ToCharacterImageRepository =>
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "characters/");
    
    public static string ToLogs => 
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs/");
    
    ///////////
    // Files //
    ///////////
    
    // Databse
    public static string ToEnglishDictionaryDB => System.IO.Path.Combine(ToAssets, "EnglishDictionary.db");
    
    // Tesseract executable
    public static string ToTesseractExe => System.IO.Path.Combine(ToAssets, "tesseract*");
    
    // Masks
    public static string ToLetterPoolOverlay =>
        System.IO.Path.Combine(ToAssets, "be2028_letter-pool-overlay.png");
    public static string ToSolvedWordsOverlay =>
        System.IO.Path.Combine(ToAssets, "be2028_solved-words-overlay.png");
    public static string ToSolvedTileOverlay =>
        System.IO.Path.Combine(ToAssets, "be2028_general-tile-overlay.png");
    
    // Templates
    public static string ToLevelInProgressTemplate =>
        System.IO.Path.Combine(ToAssets, "level-in-progress-template_35x50+300+590.png");
    public static string  ToLevelCompleteTemplate =>
        System.IO.Path.Combine(ToAssets, "level-complete_20x20+220+320.png");
    public static string  ToAppleTournamentPregameTemplate =>
        System.IO.Path.Combine(ToAssets, "apple-tournament-pregame_20x20+85+515.png");
    public static string  ToEndlessDealsTemplate =>
        System.IO.Path.Combine(ToAssets, "endless-deals_40x20+310+175.png");
    public static string  ToOutOfFirefliesTemplate =>
        System.IO.Path.Combine(ToAssets, "out-of-fireflies_20x20+215+280.png");
    public static string  ToPiggyBankFullTemplate =>
        System.IO.Path.Combine(ToAssets, "piggy-bank-full_20x20+160+400.png");
    public static string  ToSummerBloomsJigsawPuzzleTemplate =>
        System.IO.Path.Combine(ToAssets, "summer-blooms-jigsaw-puzzle_20x20+350+230.png");
    public static string  ToWildWordEventTemplate =>
        System.IO.Path.Combine(ToAssets, "wild-word-event_20x20+345+330.png");
    
        
    
        
        
        
    
    
}