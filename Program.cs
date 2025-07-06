// See https://aka.ms/new-console-template for more information

using Serilog;
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

LevelSolver ls = new LevelSolver("BE2028");
ls.Solve();