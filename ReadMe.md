This repo contains my efforts in automating playing a Wordscapes clone. It works really well! It's able to solve levels from start to finish, close popups, and transition to the next level to start again.

This solution is tailored specifically to run on my machine. This has been a headache, because Wayland breaks most of the established solutions for input simulation and window automation. And there were no current runtime bindings for
OpenCvSharp4 for Ubuntu 24.04 [until I published one](https://www.nuget.org/packages/OpenCvSharp4.unofficial.runtime.ubuntu.24.04-x64).
Fortunately, the XWayland compatibility layer makes some things still possible. 

Here's the external dependencies that I use:
- [scrcpy](https://github.com/Genymobile/scrcpy)
- [wmctrl](https://linux.die.net/man/1/wmctrl)
- [xwininfo](https://github.com/wudping/xwininfo)
- [xrandr](https://www.x.org/archive/X11R7.5/doc/man/man1/xrandr.1.html)
- [ImageMagick](https://www.imagemagick.org/) (import, convert, compare, composite)
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract) (the self-contained appimage)
- [dotool](https://git.sr.ht/~geb/dotool) (input simulation)
- [OpenCVSharp4](https://github.com/shimat/opencvsharp/) (OpenCV bindings)
- [SharpHook](https://github.com/TolikPylypchuk/SharpHook) (key listener)



The following is my notetaking from designing and implementing this project. Once the project became functional, further documentation took place inside the code.
___
### Automating Wordscapes
This is my first project, so implementation will be dirty. I'll lay out my steps and requirements, modifying them as needed to create a proof-of-concept.

- [x] Step 1: Use [scrcpy](https://github.com/Genymobile/scrcpy/) to conncet I/O between my phone and PC
    - A linux binary was provided. Extracted it to my Downloads folder and am launching it from there. Setup is super simple: just connect the phone to the computer via USB and run the program.
- [x] Step 2: Get the window
    - `wmctrl` for focusing the window:
        - `wmctrl -l` lists the names of X11 compatible windows (strcpy is)
        - `wmctrl -a "BE2028"` activates (focuses) the matching window
    - `xwininfo` for getting the window's dimensions
        - `xwininfo -name "BE2028"` gives me a bunch of data about the window. I just need 4 fields: absolute upper-left X/Y, and Width/Height.
    - `import` (imagemagick) for taking a screenshot of the app.
        - `import -window "BE2028" screenshot.png` screenshots the app. Lines up with the absolute upper-left X/Y.
- [x] Step 3: Figure out where stuff is on screen
    - See how many words there are and their lengths and see what letters are available and remember their location in screen space
        - Isolate character pool and pre-process the image
        - Made a template the same size as the screenshot with a transparent circle for where the characters go.
            - `composite -compose Over -gravity center <template file> <screenshot file> <isolated output>`
        - This leaves the circle of characters with everything else blacked out. Now to make the characters white and the rest of the background black.
            - `convert <isolated output> -colorspace gray -depth 8 -threshold 50% <contrasted isolated output>`
        - This gives me bounding-box data for objects in the contrasted image. It removes the output header and the 0: object which is always the background. The output of this is the bounding box data for the characters (and any encapsulated background, which needs to be addressed later)
            - `convert <contrasted isolated output> -define connected-components:verbose=true -define connected-components:exclude-header=true -connected-components 4 -auto-level null: | grep -vwE '^(  0:)' | awk '{ print $2 }'`
    - ~~Gonna try to use [tesseract-ocr](https://github.com/tesseract-ocr/tesseract?tab=readme-ov-file#installing-tesseract) to determine where characters are on the screen. I'll need to isolate my search area, which will require some compounding bounding box calculations, but if it works I'll know what letters are available and where they are.~~
        - ~~installing `tesseract-ocr` and `libtesseract-dev` via apt~~ (had some success using single character mode, using the AppImage instead)
        - Tesseract ended up working well enough to use once I isolated the character and ran it using page segmentation mode 10 (single character). It's not a great solution: its slow, likes to assume an image is special characters instead of a letter (e.g. I = | and O = @), and requires the image be scaled up for it to be somewhat consistent `convert {inputImage} -filter Point -resize 300% PNG:{outputImage}`. For the most part, though, it works great for identifying an unknown character the first time its encountered.
    - This crops a letter out of the image so it can be identified.
        - `convert {inputImage} -crop {cropArgs} +repage PNG:{outputImage}`
    - This compares the likeness of one image to another. I haven't looked deep into the math behind it, but it gives me a percentage value of how many pixels in an image differ.
        - `compare -metric RMSE {inputImage} {comparisonImage} null:`
    - Added a service to store identified characters so they can be used to identify the same character when encountered later. Uses the `compare` program.
  - [x] Step 4: Find valid words for the letters available.
      - ~~Got a copy of the English dictionary from [AyeshJayasekara/English-Dictionary](https://github.com/AyeshJayasekara/English-Dictionary-SQLite/blob/master/Dictionary.db) ~~
      - Got what looks like a better dictionary from [NASPA Zyzzyva](https://www.scrabbleplayers.org/w/NASPA_Zyzzyva_Linux_Installation) aka Scrabble. Made a word bank from it thats much smaller and less erroneous.
      - I can generate query to get a list of potential words using this template:
          ```sqlite
          SELECT DISTINCT word FROM entries e
          WHERE LENGTH(word) BETWEEN 3 AND LENGTH("eexpnd")
          AND LOWER(word) NOT LIKE '%a%'
          -- ... and one for every letter that isn't e, x, p, n, or d ...
          AND LOWER(word) NOT LIKE '%b%';
        
          -- List of a-z for when i need to create a list of what I don't have
          -- a b c d e f g h i j k l m n o p q r s t u v w x y z --
          ```
      - Added tables *blacklist* and *extra* so I can further limit the pool of words to try first when solving a puzzle.
- [x] Step 5: Automate the swipes needed to submit words.
    - AHK is Windows only.
    - ~~Testing to see if [Autokey](https://github.com/autokey/autokey) will fill the void in my heart...~~ *xOrg only I got Wayland*
        - Installing `visgrep`, `png2pat`, `xte`, and `xmousepos`...
    - Using `dotool` works. Was able to automate inputting a word. Has to convert pixel coordinates to scalar, and can't move too fast between the characters, but its accurate and works. Have to format the CLI commands like: `{ echo mouseto 0.704 0.727; sleep 0.3; echo mouseto 0.703 0.810; echo buttondown left; sleep 0.4; echo mouseto 0.705 0.726; sleep 0.200; echo mouseto 0.717 0.677; sleep 0.150; echo mouseto 0.718 0.772; sleep 0.080; echo buttonup left; }`
      - Running the process allows me to input commands line by line.
    - to get the size of my desktop so I can figure out my scalar coordinates, I can use `xrandr` which will output a lot of information, but on the first line will be my current desktop dimensions e.g. (Screen 0: minimum 16 x 16, *current 5760 x 1080*, maximum 32767 x 32767)
        - I only care about the first line, I can use `xrandr | head -1` to simplify getting it
- [ ] Step 6: Check if the level has been completed
    - When a level is completed, the changes. I need to check after each word for if the level is still being displayed.
    - There's plenty of popups, slow transitions, and general nonsense that can interrupt gameplay. These will need to be documented and watched for.
