namespace WordCollect_Automated.Services.WordCollect;

/// <summary>
/// Represent every state that the game can be in.
/// </summary>
public enum GameState
{
    // A state is unknown if none of the other states match. This can be due to encountering a new popup,
    // or if the game is in the midst of a state change
    Unknown,
    
    // States of the level
    LevelPresented,
    LevelCompleted,
    
    // Popups that have been seen while solving levels
    AppleTournamentPregame, // Small popup that tells you to find words in a streak to collect apples
    EndlessDeals, // Tries to get you to buy packs for whatever reason
    OutOfFireflies, // Tries to get you to buy hints
    PiggyBankFull, // Tries to get you to buy your piggy bank so you can get more useless coins
    SummerBloomsJigsawPuzzle, // Honestly no clue what this is supposed to be
    WildWordEvent, // Complete 50 levels to earn a huge coin reward!
}