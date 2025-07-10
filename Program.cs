// See https://aka.ms/new-console-template for more information

using Serilog;
using SharpHook;
using SharpHook.Data;
using WordCollect_Automated.Services.WordCollect;
using Path = WordCollect_Automated.Path;

/*
 * Creating a program that automates playing WordCollect (Wordscapes clone). I joined a program where I can play mobile
 * games and get rewards based on my progress/actions in the game.
 *
 * The max reward for this game is $200, tho the money trickles in over the course of thousands of levels.
 *
 * This program is designed to automate playing this game for me using tools available to my system.
 *
 * xrandr - get desktop dimension across all monitors (somehow works on my gnome wayland)
 * xwininfo - get an x11 window position and geometry
 * wmctrl - activating an x11 window
 * imagemagkick - convert, composite, import... for image processing, screenshotting, cropping, etc.
 * tesseract - identifying characters
 *
 * The dictionary db can be any db with a table "entries" with a column "word". I used https://github.com/AyeshJayasekara/English-Dictionary-SQLite/blob/master/Dictionary.db
 * before making my own word bank of 3-7 letter words from the scrabble dictionary.
 *
 * Tesseract can be either the Appimage (self-contained) or the native package (requires tesseract-ocr, libtesseract-dev,
 * and setting up a tessdata folder)
 **/

using var log = new LoggerConfiguration()
 .MinimumLevel.Debug()
 // .MinimumLevel.Information()
 .WriteTo.Console() // Debug
 .WriteTo.File(Path.ToLogs, rollingInterval: RollingInterval.Day)
 .CreateLogger();

Log.Logger = log;

Window window = new Window("BE2028");

var desktopBoundingBox = GnomeDesktop.GetDesktopBoundingBox();
MouseController mouseController = new MouseController(desktopBoundingBox.Width, desktopBoundingBox.Height);

AutoPlayer autoPlayer = new AutoPlayer(window, mouseController);

CancellationTokenSource cts = new CancellationTokenSource();
CancellationToken token = cts.Token;

Thread keyListener = new Thread(() =>
{
    token.ThrowIfCancellationRequested();

    // Hook that reads input events so I can escape hijacking
    var hook = new TaskPoolGlobalHook();
    hook.KeyPressed += (sender, eventArgs) =>
    {
        if (eventArgs.Data.KeyCode == KeyCode.VcEscape)
        {
            Log.Information("Escape was pressed. Requesting cancellation.");
            cts.Cancel(); // Terminate the program
        }
    };

    hook.Run();
});
keyListener.Start();

autoPlayer.PlayGame(token);

// StateChecker sc = new StateChecker();
// string[] states = new[]
// {
//  "/home/viney/Downloads/wordcollect/popups/apple-tournament-pregame.png",
//  "/home/viney/Downloads/wordcollect/popups/endless-deals.png",
//  "/home/viney/Downloads/wordcollect/popups/level-complete.png",
//  "/home/viney/Downloads/wordcollect/levels/BRING-5.png",
//  "/home/viney/Downloads/wordcollect/popups/out-of-fireflies.png",
//  "/home/viney/Downloads/wordcollect/popups/piggy-bank-full.png",
//  "/home/viney/Downloads/wordcollect/popups/summer-blooms-jigsaw-puzzle.png",
//  "/home/viney/Downloads/wordcollect/popups/wild-word-event.png",
//  "/home/viney/Downloads/wordcollect/popups/coin-rush.png"
// };
//
// foreach (var state in states)
// {
//  Console.WriteLine(sc.CheckState(state).ToString());
// }


// LevelSolver ls = new LevelSolver("BE2028");
// ls.Solve();

// string levelComplete = "/home/viney/Downloads/wordcollect/popups/level-complete.png";
// string levelCompleteTemplate = "/home/viney/Downloads/wordcollect/popups/level-complete_20x20+218+319.png";
// Console.WriteLine(ImageProcessing.NormalizedRootMeanSquareError(levelComplete, levelCompleteTemplate, new (20,20, 218,319)));
// string source = "/home/viney/Downloads/wordcollect/levels/STUDIO-5.png";
// string ridge = "/home/viney/Downloads/wordcollect/levels/STUDIO-5-ridge.png";
// string contrast = "/home/viney/Downloads/wordcollect/levels/STUDIO-5-threshold.png";
// string thin = "/home/viney/Downloads/wordcollect/levels/STUDIO-5-skeleton.png";
//
// ImageProcessing.ApplyRidgeDetectionFilter(source, ridge, scale: 0.25);
// ImageProcessing.RaiseContrast(ridge, contrast, 0.30);
// ImageProcessing.ApplyThinningFilter(contrast, thin);

// List<BoundingBox> components = ImageProcessing.GetComponents(image);
// ImageProcessing.DrawBoundingBoxesOnImage(image, "/home/viney/Downloads/wordcollect/levels/BRING-solvable-words-components.png", components);

// string ridge = "/home/viney/dev/c#/WordCollect-Automated/bin/Debug/net9.0/temp/z50dauhd.png";
// string threshold = ridge.Replace("z50dauhd", "z50dauhd-threshold");
// string thinned = ridge.Replace("z50dauhd", "z50dauhd-thinned");
// ImageProcessing.ApplyThinningFilter(ridge, thinned);
// ImageProcessing.ApplyThreshold(ridge, threshold, 0.5, ThresholdTypes.Otsu);
