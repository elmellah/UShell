using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace UShell.Consoles
{
    public class Terminal : MonoBehaviour, IConsole
    {
        #region FIELDS
        [Header("OPTIONS")]
        [SerializeField]
        private bool _displayOnStartup;
        [SerializeField]
        private KeyCode _displayKey;
        [SerializeField]
        private int _maxChars = 16248;
        [SerializeField]
        private bool _logStackTrace = false;
        [SerializeField]
        private bool _displayOnError = false;
        [SerializeField]
        private bool _destroyIfHeadless = true;

        [Header("LOG COLORS")]
        [SerializeField]
        private Color32 _assertColor = new Color32(214, 214, 214, 255);
        [SerializeField]
        private Color32 _errorColor = Color.red;
        [SerializeField]
        private Color32 _exceptionColor = new Color32(214, 0, 0, 255);
        [SerializeField]
        private Color32 _logColor = new Color32(214, 214, 214, 255);
        [SerializeField]
        private Color32 _warningColor = Color.yellow;

        [Header("REFERENCES")]
        [SerializeField]
        private GameObject _content;
        [SerializeField]
        private Text _text;
        [SerializeField]
        private InputField _input;
        [SerializeField]
        private Image _background;
        [SerializeField]
        private ScrollRect _scrollRect;

        private string _strColorAssert;
        private string _strColorError;
        private string _strColorException;
        private string _strColorWarning;

        private StringBuilder _strBuilder;
        private bool _newLog;
        private int _historyPos;
        private string _currentCmd;

        private bool _updateScrollRectNextFrame = false;
        private bool _wasScreenTouched = false;
        #endregion

        #region MESSAGES
        void Awake()
        {
            _strBuilder = new StringBuilder();

            _strColorAssert = ColorUtility.ToHtmlStringRGBA(_assertColor);
            _strColorError = ColorUtility.ToHtmlStringRGBA(_errorColor);
            _strColorException = ColorUtility.ToHtmlStringRGBA(_exceptionColor);
            _strColorWarning = ColorUtility.ToHtmlStringRGBA(_warningColor);

            _text.color = _logColor;

            if (_displayOnStartup) show(); else hide();
        }

        void OnEnable()
        {
            Shell.Main.RegisterConsole("$", this);
        }
        void OnDisable()
        {
            Shell.Main.UnregisterConsole("$", this);
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(_displayKey))
                toggleVisibility();

            if (Debug.isDebugBuild)
            {
                if (Input.touchCount == 0 && _wasScreenTouched)
                {
                    _wasScreenTouched = false;
                }
                else if (Input.touchCount == 3 && !_wasScreenTouched)
                {
                    _wasScreenTouched = true;
                    toggleVisibility();
                }
            }

            if (!_content.activeInHierarchy)
                return;


            if (_updateScrollRectNextFrame)
            {
                _updateScrollRectNextFrame = false;
                if (_scrollRect.content.sizeDelta.y > 0)
                    _scrollRect.content.anchoredPosition = new Vector2(_scrollRect.content.anchoredPosition.x, _scrollRect.content.sizeDelta.y);
            }

            if (_newLog)
            {
                _newLog = false;

                if (_strBuilder.Length > _maxChars)
                    _strBuilder.Remove(0, _strBuilder.Length - _maxChars);

                _text.text = _strBuilder.ToString();
                if (_strBuilder.Length <= 0)
                    _text.SetLayoutDirty();

                if (_scrollRect.content.sizeDelta.y < 0 || (Mathf.Abs(_scrollRect.content.sizeDelta.y - _scrollRect.content.anchoredPosition.y) <= 1f))
                    _updateScrollRectNextFrame = true;
            }

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Tab))
                getFuzzyCmds();
            else if (Input.GetKeyDown(KeyCode.Tab))
                getTabComplete();
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                getPreviousCommand();
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                getNextCommand();

            if (Input.GetKeyDown(KeyCode.Return))
                sendCommand();

            if (_input.touchScreenKeyboard != null && _input.touchScreenKeyboard.status == TouchScreenKeyboard.Status.Done)
                sendCommand();

            if (!Input.touchSupported)
                _input.ActivateInputField();
        }
        #endregion

        #region METHODS
        public void Init(bool headless)
        {
            if (headless && _destroyIfHeadless)
            {
                Destroy(this.gameObject);
                return;
            }
            AddLog(new Log(LogType.Log, Shell.Main.MOTD, ""));
        }
        public void AddLog(Log log)
        {
            if (log.LogType == LogType.Log)
            {
                _strBuilder.Append(log.Value).Append("\n");
            }
            else
            {
                _strBuilder.Append("<color=#");

                switch (log.LogType)
                {
                    case LogType.Assert:
                        _strBuilder.Append(_strColorAssert);
                        break;
                    case LogType.Error:
                        if (_displayOnError && !_content.activeInHierarchy)
                            show();
                        _strBuilder.Append(_strColorError);
                        break;
                    case LogType.Exception:
                        if (_displayOnError && !_content.activeInHierarchy)
                            show();
                        _strBuilder.Append(_strColorException);
                        break;
                    case LogType.Warning:
                        _strBuilder.Append(_strColorWarning);
                        break;
                }

                if (_logStackTrace)
                    _strBuilder.Append(">").AppendLine(log.Value).Append(log.StackTrace).Append("</color>\n");
                else
                    _strBuilder.Append(">").Append(log.Value).Append("</color>\n");
            }

            _newLog = true;
        }
        public bool ProcessEvent(string label, string[] args)
        {
            switch (label)
            {
                case ":help":
                    Debug.Log(@"terminal events: 12
    - :help: log all terminal events
    - :clear: clear the terminal
    - :opacity [value]: change the background opacity
    - :fontsize [value]: change the size of the text
    - :show: show the terminal
    - :hide: hide the terminal
    - :toggle: toggle the visibility of the terminal
    - :stacktrace [value]: choose to display the stack trace in the terminal
    - :maxchars [value]: change the maximum number of characters that can be displayed in the terminal
    - :showonerror [value]: show the terminal when an error/exception occurs
    - :scrollbarwidth [value]: set the width of the scrollbar
    - :font [name]: display and set the current font");
                    return true;
                case ":clear":
                    _strBuilder.Clear();
                    _newLog = true;
                    return true;
                case ":opacity":
                    if (args.Length == 0)
                    {
                        _strBuilder.Append(_background.color.a).Append("\n");
                        _newLog = true;
                    }
                    else if (args.Length == 1)
                    {
                        if (float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float opacity))
                            _background.color = new Color(_background.color.r, _background.color.g, _background.color.b, opacity);
                        else
                            Debug.LogError("cannot parse \'" + args[0] + "\' to float");
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
                case ":fontsize":
                    if (args.Length == 0)
                    {
                        _strBuilder.Append(_text.fontSize).Append("\n");
                        _newLog = true;
                    }
                    else if (args.Length == 1)
                    {
                        if (int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int size))
                            _text.fontSize = size;
                        else
                            Debug.LogError("cannot parse \'" + args[0] + "\' to int");
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
                case ":show":
                    show();
                    return true;
                case ":hide":
                    hide();
                    return true;
                case ":toggle":
                    toggleVisibility();
                    return true;
                case ":stacktrace":
                    if (args.Length == 0)
                    {
                        _strBuilder.Append(_logStackTrace).Append("\n");
                        _newLog = true;
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
                case ":maxchars":
                    if (args.Length == 0)
                    {
                        _strBuilder.Append(_maxChars).Append("\n");
                        _newLog = true;
                    }
                    else if (args.Length == 1)
                    {
                        if (int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxChars))
                            _maxChars = maxChars;
                        else
                            Debug.LogError("cannot parse \'" + args[0] + "\' to int");
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
                case ":showonerror":
                    if (args.Length == 0)
                    {
                        _strBuilder.Append(_displayOnError).Append("\n");
                        _newLog = true;
                    }
                    else if (args.Length == 1)
                    {
                        if (bool.TryParse(args[0], out bool displayOnError))
                            _displayOnError = displayOnError;
                        else
                            Debug.LogError("cannot parse \'" + args[0] + "\' to bool");
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
                case ":scrollbarwidth":
                    if (args.Length == 0)
                    {
                        RectTransform scrollBar = _scrollRect.verticalScrollbar.GetComponent<RectTransform>();
                        if (scrollBar != null)
                        {
                            float width = -scrollBar.offsetMin.x;
                            _strBuilder.Append(width).Append("\n");
                            _newLog = true;
                        }
                        else
                            Debug.LogWarning("no scrollbar assigned!");
                    }
                    else if (args.Length == 1)
                    {
                        if (float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float scrollBarWidth))
                        {
                            RectTransform scrollBar = _scrollRect.verticalScrollbar.GetComponent<RectTransform>();
                            if (scrollBar != null)
                            {
                                scrollBar.offsetMin = new Vector2(-scrollBarWidth, scrollBar.offsetMin.y);
                                _scrollRect.Rebuild(CanvasUpdate.Prelayout);
                            }
                            else
                                Debug.LogWarning("no scrollbar assigned!");
                        }
                        else
                            Debug.LogError("cannot parse \'" + args[0] + "\' to float");
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
                case ":font":
                    if (args.Length == 0)
                    {
                        Debug.Log(_text.font.name);
                    }
                    else if (args.Length == 1)
                    {
                        Font font = Shell.Main.GetFont(args[0]);
                        if (font != null)
                        {
                            _text.font = font;
                            _input.textComponent.font = font;
                            Text placeHolder = _input.placeholder.GetComponent<Text>();
                            if (placeHolder != null)
                                placeHolder.font = font;
                        }
                        else
                            Debug.LogWarning("font not found!");
                    }
                    else
                        Debug.LogWarning("syntax error");
                    return true;
                case ":throw":
                    throw new System.Exception(string.Join(" ", args));
            }

            return false;
        }

        public void OnPromptValueChanged()
        {
            _historyPos = 0;
        }


        private void getTabComplete()
        {
            UnityEngine.Event outEvent = new UnityEngine.Event();
            UnityEngine.Event.PopEvent(outEvent);

            string tab = Shell.Main.GetCompletion(_input.text, out List<string> options);
            _input.SetTextWithoutNotify(_input.text + tab);
            _input.MoveTextEnd(false);

            if (options != null && options.Count > 1)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    _strBuilder.Append(options[i]).Append(" ");
                    _newLog = true;
                    _updateScrollRectNextFrame = true;
                }
                _strBuilder.Append("\n");
            }
        }
        private void getFuzzyCmds()
        {
            UnityEngine.Event outEvent = new UnityEngine.Event();
            UnityEngine.Event.PopEvent(outEvent);

            List<string> fuzzyCmds = Shell.Main.GetSimilarCmds(_input.text, 3);
            if (fuzzyCmds.Count > 0)
            {
                for (int i = 0; i < fuzzyCmds.Count; i++)
                {
                    _strBuilder.Append(fuzzyCmds[i]).Append(" ");
                    _newLog = true;
                }
                _strBuilder.Append("\n");
            }

            _updateScrollRectNextFrame = true;
        }
        private void getPreviousCommand()
        {
            UnityEngine.Event outEvent = new UnityEngine.Event();
            UnityEngine.Event.PopEvent(outEvent);

            _historyPos = Mathf.Clamp(_historyPos, 0, Shell.Main.HistoryCount);

            if (_historyPos == 0)
                _currentCmd = _input.text;

            string cmd = Shell.Main.GetCmdLineFromHistory(_historyPos + 1);
            if (cmd != null)
            {
                _input.SetTextWithoutNotify(cmd);
                _input.MoveTextEnd(false);
                _historyPos++;
            }
        }
        private void getNextCommand()
        {
            UnityEngine.Event outEvent = new UnityEngine.Event();
            UnityEngine.Event.PopEvent(outEvent);

            _historyPos = Mathf.Clamp(_historyPos, 0, Shell.Main.HistoryCount);

            if (_historyPos == 1)
            {
                _input.SetTextWithoutNotify(_currentCmd);
                _historyPos = 0;
            }
            else
            {
                string cmd = Shell.Main.GetCmdLineFromHistory(_historyPos - 1);
                if (cmd != null)
                {
                    _input.SetTextWithoutNotify(cmd);
                    _input.MoveTextEnd(false);
                    _historyPos--;
                }
            }
        }

        private void toggleVisibility()
        {
            if (_content.activeInHierarchy)
                hide();
            else
                show();
        }
        private void show()
        {
            _content.SetActive(true);
            if (!Input.touchSupported)
                _input.ActivateInputField();
        }
        private void hide()
        {
            _input.DeactivateInputField();
            _content.SetActive(false);
        }
        private void sendCommand()
        {
            string cmd = _input.text;
            if (!string.IsNullOrEmpty(cmd)) {
                _historyPos = 0;
                _input.SetTextWithoutNotify("");
                _updateScrollRectNextFrame = true;
                Shell.Main.ProcessCmdLine("$", cmd);
            }
        }
        #endregion
    }
}
