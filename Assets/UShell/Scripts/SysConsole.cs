using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace UShell.Consoles
{
    public class SysConsole : MonoBehaviour, IConsole
    {
        #region FIELDS
        private const string _prompt = "%> ";

        [SerializeField]
        private bool _isLogging = true;
        [SerializeField]
        private bool _logStackTrace = false;

        private string _currentCmd;
        private int _historyPos;

        private string _currentLine;
        private bool _isInitialized;
#if UNITY_STANDALONE_WIN
        private TextWriter _previousOutput;
#else
        private Thread _readerThread;
        private char[] _buffer = new char[1024];
#endif
        #endregion

        #region EXTERN
#if UNITY_STANDALONE_WIN
        [DllImport("Kernel32.dll")]
        private static extern bool AttachConsole(uint processId);
        [DllImport("Kernel32.dll")]
        private static extern bool AllocConsole();
        [DllImport("Kernel32.dll")]
        private static extern bool FreeConsole();
        [DllImport("Kernel32.dll")]
        private static extern bool SetConsoleTitle(string title);
#endif
        #endregion

        #region MESSAGES
        void OnDestroy()
        {
            shutdown();
        }

        void OnEnable()
        {
            Shell.Main.RegisterConsole("%", this);
        }
        void OnDisable()
        {
            Shell.Main.UnregisterConsole("%", this);
        }

        void Update()
        {
            if (!_isInitialized)
                return;

            // Handling for cases where the terminal is 'dumb', i.e. cursor etc.
            // and no individual keys fired
            if (isDumb())
            {
                var lines = _currentLine.Split('\n');
                if (lines.Length > 1)
                {
                    for (int i = 0; i < lines.Length - 1; i++)
                        Shell.Main.ProcessCmdLine("%", lines[i]);
                    _currentLine = lines[lines.Length - 1];
                }
                return;
            }

            if (!Console.KeyAvailable)
                return;

            var keyInfo = Console.ReadKey();

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    _historyPos = 0;
                    Console.CursorLeft = 0;
                    Console.CursorTop = Console.BufferHeight - 1;
                    Console.Write("");
                    Console.CursorLeft = 0;
                    Shell.Main.ProcessCmdLine("%", _currentLine);
                    _currentLine = "";
                    drawInputline();
                    break;
                case ConsoleKey.Escape:
                    _currentLine = "";
                    drawInputline();
                    break;
                case ConsoleKey.Backspace:
                    if (_currentLine.Length > 0)
                        _currentLine = _currentLine.Substring(0, _currentLine.Length - 1);
                    drawInputline();
                    break;
                case ConsoleKey.UpArrow:
                    getPreviousCommand();
                    break;
                case ConsoleKey.DownArrow:
                    getNextCommand();
                    break;
                case ConsoleKey.Tab:
                    if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                        getFuzzyCmds();
                    else
                        getTabComplete();
                    break;
                default:
                    {
                        _historyPos = 0;

                        if (keyInfo.KeyChar != '\u0000')
                        {
                            _currentLine += keyInfo.KeyChar;
                            drawInputline();
                        }
                    }
                    break;
            }
        }
        #endregion

        #region METHODS
        public void Init(bool headless)
        {
#if UNITY_STANDALONE_WIN
            if (headless)
                init();
            AddLog(new Log(LogType.Log, Shell.Main.MOTD, ""));
#endif
        }
        public void AddLog(Log log)
        {
            if (!_isInitialized)
                return;

            if (_isLogging)
            {
                if (_logStackTrace)
                    outputString(log.log + log.stackTrace);
                else
                    outputString(log.log);
            }
        }
        public bool ProcessEvent(string label, string[] args)
        {
            switch (label)
            {
                case ":help":
                    Debug.Log(@"system console events: 4
    - :help: log all system console events
    - :clear: clear the system console
    - :init: initialize a new system console
    - :shutdown: shutdown the current system console
    - :nolog: does not log to the current system console
    - :stacktrace [value]: choose to display the stack trace");
                    return true;
                case ":clear":
                    Console.Clear();
                    drawInputline();
                    return true;
                case ":init":
                    init();
                    return true;
                case ":shutdown":
                    shutdown();
                    return true;
                case ":nolog":
                    if (args.Length == 0)
                    {
                        Debug.Log(!_isLogging);
                    }
                    else if (args.Length == 1)
                    {
                        if (bool.TryParse(args[0], out bool noLog))
                            _isLogging = !noLog;
                        else
                            Debug.LogError("cannot parse \'" + args[0] + "\' to bool");
                    }
                    return true;
                case ":stacktrace":
                    if (args.Length == 0)
                    {
                        outputString(_logStackTrace + "\n");
                    }
                    else if (args.Length == 1)
                    {
                        if (bool.TryParse(args[0], out bool logStackTrace))
                            _logStackTrace = logStackTrace;
                        else
                            Debug.LogError("cannot parse \'" + args[0] + "\' to bool");
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
            }

            return false;
        }

        private void clearInputLine()
        {
            if (!_isInitialized)
                return;

            if (isDumb())
                return;

            Console.CursorLeft = 0;
            Console.CursorTop = Console.BufferHeight - 1;
            Console.Write(new string(' ', Console.BufferWidth - 1));
            Console.CursorLeft = 0;
        }
        private void drawInputline()
        {
            if (!_isInitialized)
                return;

            if (isDumb())
                return;

            Console.CursorLeft = 0;
            Console.CursorTop = Console.BufferHeight - 1;
            Console.Write(_prompt + _currentLine + new string(' ', Console.BufferWidth - _currentLine.Length - _prompt.Length - 1));
            Console.CursorLeft = _currentLine.Length + _prompt.Length;
        }
        private void outputString(string message)
        {
            if (!_isInitialized)
                return;

            clearInputLine();
            Console.WriteLine(message);
            drawInputline();
        }
        private bool isDumb()
        {
            return Console.BufferWidth == 0 || Console.IsInputRedirected || Console.IsOutputRedirected;
        }

        private void init()
        {
            if (_isInitialized)
                return;

#if UNITY_STANDALONE_WIN
            if (!AttachConsole(0xffffffff))
                AllocConsole();
            _previousOutput = Console.Out;

            string title = Application.productName + " " + Application.version;
            if (!string.IsNullOrEmpty(Shell.Main.ID))
                title += " (" + Shell.Main.ID + ")";
            SetConsoleTitle(title);

            Console.Clear();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
#else
            Console.WriteLine("Dumb console: " + isDumb());
            if (isDumb())
            {
                _readerThread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    while (true)
                    {
                        var read = Console.In.Read(_buffer, 0, _buffer.Length);
                        if (read > 0)
                            _currentLine += new string(_buffer, 0, read);
                        else
                            break;
                    }
                });
                _readerThread.Start();
            }
            Console.Clear();
#endif

            _isInitialized = true;

            _currentLine = "";
            drawInputline();
        }
        private void shutdown()
        {
            if (!_isInitialized)
                return;

#if UNITY_STANDALONE_WIN
            Console.SetOut(_previousOutput);
            FreeConsole();
#endif

            _isInitialized = false;
        }

        private void getTabComplete()
        {
            string tab = Shell.Main.GetCompletion(_currentLine, out List<string> options);
            _currentLine = tab;
            drawInputline();

            StringBuilder strBuilder = new StringBuilder();
            if (options != null && options.Count > 1)
                for (int i = 0; i < options.Count; i++)
                    strBuilder.Append(options[i]).Append(" ");

            outputString(strBuilder.ToString());
        }
        private void getFuzzyCmds()
        {
            List<string> fuzzyCmds = Shell.Main.GetSimilarCmds(_currentLine, 3);
            if (fuzzyCmds.Count > 0)
            {
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < fuzzyCmds.Count; i++)
                    strBuilder.Append(fuzzyCmds[i]).Append(" ");

                outputString(strBuilder.ToString());
            }
        }
        private void getPreviousCommand()
        {
            _historyPos = Mathf.Clamp(_historyPos, 0, Shell.Main.HistoryCount);

            if (_historyPos == 0)
                _currentCmd = _currentLine;

            string cmd = Shell.Main.GetCmdLineFromHistory(_historyPos + 1);
            if (cmd != null)
            {
                _currentLine = cmd;
                drawInputline();
                _historyPos++;
            }
        }
        private void getNextCommand()
        {
            _historyPos = Mathf.Clamp(_historyPos, 0, Shell.Main.HistoryCount);

            if (_historyPos == 1)
            {
                _currentLine = _currentCmd;
                drawInputline();
                _historyPos = 0;
            }
            else
            {
                string cmd = Shell.Main.GetCmdLineFromHistory(_historyPos - 1);
                if (cmd != null)
                {
                    _currentLine = cmd;
                    drawInputline();
                    _historyPos--;
                }
            }
        }
#endregion
    }
}
