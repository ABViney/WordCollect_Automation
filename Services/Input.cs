using System.Diagnostics;

namespace WordCollect_Automated.Services;

/// <summary>
/// Simulates input on my Ubuntu/linux machine using dotool <see href="https://git.sr.ht/~geb/dotool"/>
/// </summary>
public class Input : IDisposable
{
    private static Input? _singleton;
    
    public static Input GetInstance()
    {
        if (_singleton is null)
        {
            _singleton = new Input();
        }

        return _singleton;
    }
    
    private bool _isRunning;
    private bool _isDisposed;
    private Process? doToolProcess;
    private StreamWriter _input;
    private bool _isWriting;

    public bool IsRunning => _isRunning;

    /// <summary>
    /// Issue an input command.
    /// </summary>
    /// <param name="doToolCommand"></param>
    /// <returns>True if the command was written to the input stream.</returns>
    public bool IssueCommand(string doToolCommand)
    {
        if (!_isWriting) // Closest thing to thread safety offerred at this time
        {
            _isWriting = true;
            _input.WriteLine(doToolCommand);
            _input.Flush();
            _isWriting = false;
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Star
    /// </summary>
    public bool Start()
    {
        if (!IsRunning)
        {
            return StartNewInstance();
        }

        return false;
    }

    public void Shutdown()
    {
        if (IsRunning && doToolProcess is not null)
        {
            doToolProcess.Kill();
            doToolProcess.WaitForExit();
            _isRunning = false;
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
        Shutdown();
    }
    
    private Input()
    {
    }
    
    private bool StartNewInstance()
    {
        if (_isDisposed) return false;

        if (IsRunning)
        {
            Shutdown();
        }
        
        doToolProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"dotool\"",
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };
        _isRunning = doToolProcess.Start();
        _input = doToolProcess.StandardInput;
        return _isRunning;
    }
}