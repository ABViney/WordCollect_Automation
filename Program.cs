// See https://aka.ms/new-console-template for more information

using WordCollect_Automated.Services;

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
 */

/////////////
// Testing //
/////////////

// Getting desktop dimensions (works)
// var desktop = GnomeDesktop.GetDesktopBoundingBox(); 
// Console.WriteLine(desktop);

// Getting window dimensions (works)
// var window = GnomeDesktop.GetWindowBoundingBox("BE2028");
// Console.WriteLine(window);

// Focus the window (works)
// GnomeDesktop.FocusWindow("BE2028");

// Take screenshot (works)
// GnomeDesktop.ScreenshotWindow("BE2028", "~/Downloads/BE2028-screenshot.png");

// Test getting character bounding boxes (works)
// var boundingBoxes = OCR.GetCharacterBoundingBoxes("~/Downloads/be2028_isolated-character-pool_contrast.png");
// boundingBoxes.ForEach(bb => Console.WriteLine(bb));
// ImageProcessing.DrawBoundingBoxesOnImage(
//   "~/Downloads/be2028_isolated-character-pool_contrast.png", 
//   "~/Downloads/be2028_isolated-character-pool_contrast_identified-characters.png",
//   boundingBoxes);

// Cropping letters from the image (works)
// List<string> imagesOfCroppedLetters = new List<string>();
// for (int i = 0; i < boundingBoxes.Count; i++)
// {
//     string outputLocation = $"~/Downloads/cropped_letters/{i}.png";
//     imagesOfCroppedLetters.Add(outputLocation);
//     ImageProcessing.CropUsingBoundingBox("~/Downloads/be2028_isolated-character-pool_contrast.png", outputLocation, boundingBoxes[i]);
//     ImageProcessing.ScaleImage(outputLocation, outputLocation, 3.0); // some letters won't read unless scaled up
// }

// Testing Tesseract's ability to identify cropped letters
// foreach (var croppedLetter in imagesOfCroppedLetters)
// {
//     string character = OCR.IdentifyCharacter(croppedLetter);
//     Console.WriteLine($"tesseract thinks {croppedLetter} is {character}");
// }

// Test fetching available words (words)
// var words = EnglishDictionary.GetPotentialWords(new () {"E", "X", "P", "E", "N", "D"});
// var words = EnglishDictionary.GetPotentialWords(new () {"D", "O", "U", "G", "H"});
// var words = EnglishDictionary.GetPotentialWords(new () {"F", "R", "O", "Z", "E"});
// foreach (var word in words)
// {
//  Console.WriteLine(word);
// }