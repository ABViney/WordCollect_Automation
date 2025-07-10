using WordCollect_Automated.Services;

namespace WordCollect_Automated.Models;

public class Window
{
    public string WindowName { get; }
    public BoundingBox BoundingBox { get; }

    public Window(string windowName)
    {
        WindowName = windowName;
        BoundingBox = GnomeDesktop.GetWindowBoundingBox(WindowName);
    }

    public void Focus()
    {
        GnomeDesktop.FocusWindow(WindowName);
    }

    public ITemporaryFile TakeScreenshot()
    {
        ITemporaryFile screenshot = TemporaryDataManager.CreateTemporaryPNGFile();
        GnomeDesktop.ScreenshotWindow(WindowName, screenshot.Path);
        return screenshot;
    }
}