using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System;
using System.Text;
using System.IO;
#if UNITY_EDITOR && SHELL_EXTERNAL_SCRIPTS
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace UShell.Consoles
{
    public class Pipeline : MonoBehaviour, IConsole
    {
        public class PipeIn
        {
            private NamedPipeServerStream _stream;
            private byte[] _buffer = new byte[1024];

            public bool IsOpened { get { return _stream != null; } }

            public void Open(string path)
            {
                _stream = new NamedPipeServerStream(path, PipeDirection.In, 1, PipeTransmissionMode.Byte);
                _stream.BeginWaitForConnection(new AsyncCallback(callbackConnection), null);
            }
            public void Close()
            {
                if (_stream == null)
                    return;

                _stream.Dispose();
                _stream = null;
            }

            private void callbackConnection(IAsyncResult result)
            {
                _stream.EndWaitForConnection(result);
                _stream.BeginRead(_buffer, 0, _buffer.Length, new AsyncCallback(callbackRead), null);
            }
            private void callbackRead(IAsyncResult result)
            {
                int count = _stream.EndRead(result);
                if (count == 0)
                {
                    if (_stream.IsConnected)
                        _stream.Disconnect();
                    _stream.BeginWaitForConnection(new AsyncCallback(callbackConnection), null);
                }
                else
                {
                    _stream.BeginRead(_buffer, 0, _buffer.Length, new AsyncCallback(callbackRead), null);
                    string command = Encoding.UTF8.GetString(_buffer, 0, count - 1);
                    Shell.Main.ProcessCmdLine("|", command);
                }
            }
        }
        public class PipeOut
        {
            #region FIELDS
            private NamedPipeServerStream _stream;
            private byte[] _buffer = new byte[1024];
            private bool _isWriting;

            private Queue<string> _msgQueue = new Queue<string>();
            private string _currentMsg;
            private int _pos;

            private readonly byte[] _msgEnd;
            private readonly byte[] _closingMsg;
            #endregion

            #region PROPERTIES
            public bool IsOpened { get { return _stream != null; } }
            #endregion

            #region CONSTRUCTORS
            public PipeOut() : this("\n", @"\EOS") { }
            public PipeOut(string messageEnd, string closingMessage)
            {
                if (messageEnd != null)
                {
                    int byteCount = Encoding.UTF8.GetByteCount(messageEnd);
                    _msgEnd = new byte[byteCount];
                    Encoding.UTF8.GetBytes(messageEnd, 0, messageEnd.Length, _msgEnd, 0);
                }
                else
                    _msgEnd = new byte[0];

                if (closingMessage != null && closingMessage.Length > 0)
                {
                    int byteCount = Encoding.UTF8.GetByteCount(closingMessage);
                    _closingMsg = new byte[byteCount];
                    Encoding.UTF8.GetBytes(closingMessage, 0, closingMessage.Length, _closingMsg, 0);
                }
                else
                    _closingMsg = null;
            }
            #endregion

            #region METHODS
            public void Open(string path)
            {
                if (_stream != null)
                    return;

                _pos = -1;
                _isWriting = false;
                _msgQueue.Clear();
                _stream = new NamedPipeServerStream(path, PipeDirection.Out, 1, PipeTransmissionMode.Byte);

                _stream.BeginWaitForConnection(new AsyncCallback(callbackConnection), null);
            }
            public void Close()
            {
                if (_stream == null)
                    return;

                if (_closingMsg != null)
                {
                    _stream.Write(_closingMsg, 0, _closingMsg.Length);
                    _stream.Flush();
                }

                _stream.Dispose();
                _stream = null;
            }

            public void Send(string message)
            {
                if (_stream == null)
                    return;

                _msgQueue.Enqueue(message);

                if (_pos == -1 && !_isWriting)
                {
                    _currentMsg = _msgQueue.Dequeue();
                    _pos = 0;

                    send();
                }
            }


            private void callbackConnection(IAsyncResult result)
            {
                _stream.EndWaitForConnection(result);
            }
            private void callbackWrite(IAsyncResult result)
            {
                _stream.EndWrite(result);
                _stream.Flush();

                if (_pos != -1)
                {
                    send();
                }
                else if (_msgQueue.Count > 0)
                {
                    _currentMsg = _msgQueue.Dequeue();
                    _pos = 0;

                    send();
                }
                else
                {
                    _isWriting = false;
                }
            }

            private void send()
            {
                _isWriting = true;

                int charCount, byteCount;
                if (((_currentMsg.Length - _pos) * 4) + _msgEnd.Length > _buffer.Length)
                {
                    charCount = _buffer.Length / 4;
                    byteCount = Encoding.UTF8.GetBytes(_currentMsg, _pos, charCount, _buffer, 0);
                    _pos += charCount;
                }
                else
                {
                    charCount = _currentMsg.Length - _pos;
                    byteCount = Encoding.UTF8.GetBytes(_currentMsg, _pos, charCount, _buffer, 0);
                    _pos = -1;
                    for (int i = 0; i < _msgEnd.Length; i++)
                        _buffer[byteCount++] = _msgEnd[i];
                }

                _stream.BeginWrite(_buffer, 0, byteCount, new AsyncCallback(callbackWrite), null);
            }
            #endregion
        }

        #region FIELDS
#if UNITY_EDITOR && SHELL_EXTERNAL_SCRIPTS
        private const string _bashScriptsFolder = "Linux Scripts";
        private const string _powerShellScriptsFolder = "Windows Scripts";
#endif
        private static readonly string _pipeInName = ".consin";
        private static readonly string _pipeOutName = ".consout";

        private PipeIn _pipeIn;
        private PipeOut _pipeOut;

        private string _pipeInAbsolutePath;
        private string _pipeOutAbsolutePath;
        #endregion

        #region MESSAGES
        void Awake()
        {
            _pipeIn = new PipeIn();
            _pipeOut = new PipeOut();

#if UNITY_STANDALONE_WIN
            _pipeInAbsolutePath = @"\\.\pipe\" + _pipeInName + Shell.Main.ID;
            _pipeOutAbsolutePath = @"\\.\pipe\" + _pipeOutName + Shell.Main.ID;
#else
            _pipeInAbsolutePath = Application.dataPath + "/../" + _pipeInName + Shell.Main.ID;
            _pipeOutAbsolutePath = Application.dataPath + "/../" + _pipeOutName + Shell.Main.ID;
#endif
        }

        void OnEnable()
        {
            Shell.Main.RegisterConsole("|", this);
        }
        void OnDisable()
        {
            Shell.Main.UnregisterConsole("|", this);

            _pipeIn.Close();
            _pipeOut.Close();
        }
#endregion

#region METHODS
#if UNITY_EDITOR && SHELL_EXTERNAL_SCRIPTS
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            string buildFolder = Path.GetDirectoryName(pathToBuiltProject);
            string exeName = Path.GetFileName(pathToBuiltProject);

            if (target == BuildTarget.StandaloneLinux64)
            {
                TextAsset[] textAssets = Resources.LoadAll<TextAsset>(_bashScriptsFolder);
                for (int i = 0; i < textAssets.Length; i++)
                {
                    string text = textAssets[i].ToString();
                    text = text.Replace("{{EXENAME}}", exeName);
                    text = text.Replace("{{PIPEOUT}}", _pipeOutName);
                    text = text.Replace("{{PIPEIN}}", _pipeInName);
                    File.WriteAllText(buildFolder + "/" + textAssets[i].name, text);
                }
            }

            if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
            {
                TextAsset[] textAssets = Resources.LoadAll<TextAsset>(_powerShellScriptsFolder);
                for (int i = 0; i < textAssets.Length; i++)
                {
                    string text = textAssets[i].ToString();
                    text = text.Replace("{{EXENAME}}", exeName);
                    text = text.Replace("{{PIPEOUT}}", _pipeOutName);
                    text = text.Replace("{{PIPEIN}}", _pipeInName);
                    File.WriteAllText(buildFolder + "/" + textAssets[i].name, text);
                }
            }
        }
#endif

        public void Init(bool headless) {}
        public void AddLog(Log log)
        {
            if (_pipeOut != null && _pipeOut.IsOpened)
                _pipeOut.Send(log.Value);
        }
        public bool ProcessEvent(string label, string[] args)
        {
            switch (label)
            {
                case ":help":
                    Debug.Log(@"pipeline events: 4
    - :help: log all pipeline events
    - :path [in|out]: log the path of the pipe in/out
    - :open [in|out]: open pipe in/out
    - :close [in|out]: close pipe in/out");
                    return true;
                case ":path":
                    if (args.Length == 1)
                    {
                        if (args[0] == "in")
                            Debug.Log(_pipeInAbsolutePath);
                        else if (args[0] == "out")
                            Debug.Log(_pipeOutAbsolutePath);
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
                case ":open":
                    if (args.Length == 1)
                    {
                        if (args[0] == "in")
                        {
                            _pipeIn.Open(_pipeInAbsolutePath);
                            _pipeOut.Send("pipe in opened");
                        }
                        else if (args[0] == "out")
                        {
                            _pipeOut.Open(_pipeOutAbsolutePath);
                            _pipeOut.Send("pipe out opened");
                        }
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
                case ":close":
                    if (args.Length == 1)
                    {
                        if (args[0] == "in")
                        {
                            _pipeIn.Close();
                            _pipeOut.Send("pipe in closed");
                        }
                        else if (args[0] == "out")
                        {
                            _pipeOut.Close();
                            _pipeOut.Send("pipe out closed");
                        }
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
            }

            return false;
        }
#endregion
    }
}