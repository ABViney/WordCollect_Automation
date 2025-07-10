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
    
    // In Assets/
    public static string ToEnglishDictionaryDB => System.IO.Path.Combine(ToAssets, "EnglishDictionary.db");
    public static string ToTesseractExe => System.IO.Path.Combine(ToAssets, "tesseract*");
    public static string ToLetterPoolOverlay =>
        System.IO.Path.Combine(ToAssets, "be2028_letter-pool-overlay.png");
    public static string ToSolvedWordsOverlay =>
        System.IO.Path.Combine(ToAssets, "be2028_solved-words-overlay.png");
    public static string ToSolvedTileOverlay =>
        System.IO.Path.Combine(ToAssets, "be2028_general-tile-overlay.png");
    public static string ToLevelInProgressTemplate =>
        System.IO.Path.Combine(ToAssets, "be2028_level-in-progress-template.png");
    
    // In Temp/
    public static string ToCurrentLetterPool => System.IO.Path.Combine(ToTemporaryData, "current-character-pool/");
    
    // Outputs of CharacterReader.cs
    
    
}