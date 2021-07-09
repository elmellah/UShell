using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UShell.Commands
{
    public class CommandInput : MonoBehaviour, ICommand
    {
        #region FIELDS
        private static CommandInput _instance;

        [SerializeField]
        private KeyCode[] _defaultKeyCodes;
        [SerializeField]
        private string[] _defaultCommands;

        private List<KeyCode> _keyCodes;
        private List<string> _commands;
        #endregion

        #region MESSAGES
        void Awake()
        {
            if (_instance != null)
            {
                Destroy(this.gameObject);
                return;
            }

            _instance = this;
            if (this.transform.parent != null)
                this.transform.SetParent(null);
            DontDestroyOnLoad(this.gameObject);

            Shell.Main.RegisterCmd("keycode", this, false);
            Shell.Main.RegisterCmd("bind", this, false);
            Shell.Main.RegisterCmd("unbind", this, false);

            _keyCodes = new List<KeyCode>();
            _commands = new List<string>();

            if (_defaultKeyCodes != null && _defaultCommands != null)
            {
                int length = Mathf.Min(_defaultKeyCodes.Length, _defaultCommands.Length);
                for (int i = 0; i < length; i++)
                {
                    _keyCodes.Add(_defaultKeyCodes[i]);
                    _commands.Add(_defaultCommands[i]);
                }
            }
        }
        void Update()
        {
            for (int i = 0; i < _keyCodes.Count; i++)
            {
                if (Input.GetKeyDown(_keyCodes[i]))
                    Shell.Main.ProcessCmdLine("B", _commands[i]);
            }
        }
        #endregion

        #region METHODS
        public string[] GetSyntaxes(string label)
        {
            switch (label)
            {
                case "bind":
                    return new string[] { "[key-code value]" };
                case "unbind":
                    return new string[] { "key-code" };
            }
            return new string[0];
        }
        public string[] GetInfos(string label)
        {
            switch (label)
            {
                case "bind":
                    return new string[] { "log and add bindings" };
                case "unbind":
                    return new string[] { "remove binding" };
                case "keycode":
                    return new string[] { "log all key codes" };
            }
            return new string[0];
        }
        public string GetCompletion(string label, string[] args, out List<string> options)
        {
            options = new List<string>();
            return "";
        }

        public void Execute(string label, string[] args)
        {
            switch (label)
            {
                case "bind":
                    executeBind(args);
                    break;
                case "unbind":
                    executeUnbind(args);
                    break;
                case "keycode":
                    executeKeycode(args);
                    break;
            }
        }

        private void executeBind(string[] args)
        {
            if (args.Length == 0)
            {
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < _keyCodes.Count; i++)
                    strBuilder.AppendLine(_keyCodes[i] + " -> " + _commands[i]);
                Debug.Log(strBuilder.ToString());
            }
            else if (args.Length == 2)
            {
                KeyCode keyCode = Utils.EnumParse<KeyCode>(args[0], false);
                _keyCodes.Add(keyCode);
                _commands.Add(args[1]);
            }
            else
                throw new SyntaxException();
        }
        private void executeUnbind(string[] args)
        {
            if (args.Length == 1)
            {
                KeyCode keyCode = Utils.EnumParse<KeyCode>(args[0], false);
                int index = _keyCodes.IndexOf(keyCode);
                if (index != -1)
                {
                    _keyCodes.RemoveAt(index);
                    _commands.RemoveAt(index);
                }
                else
                    Debug.LogWarning("no entry for " + args[0]);
            }
            else
                throw new SyntaxException();
        }
        private void executeKeycode(string[] args)
        {
            Array keyCodes = Enum.GetValues(typeof(KeyCode));
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < keyCodes.Length; i++)
            {
                string keyCode = keyCodes.GetValue(i).ToString();
                if (!keyCode.StartsWith("Joystick") || !keyCode.Contains("Button"))
                    strBuilder.Append(keyCode).Append("\n");
            }

            Debug.Log(strBuilder.ToString());
        }
        #endregion
    }
}
