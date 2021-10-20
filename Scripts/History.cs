using System;
using System.Text;
using UnityEngine;

namespace UShell
{
    [Serializable]
    public class History
    {
        #region FIELDS
        private string[] _values = new string[10];
        private string _name = "history";
        private int _pos;
        private int _count;
        #endregion

        #region PROPERTIES
        public int Count { get => _count; }
        #endregion

        #region CONSTRUCTORS
        public History() { }
        public History(int capacity)
        {
            _values = new string[capacity];
        }
        public History(string name)
        {
            _name = name;
        }
        public History(int capacity, string name)
        {
            _values = new string[capacity];
            _name = name;
        }
        #endregion

        #region METHODS
        public void Clear()
        {
            _pos = _count = 0;
        }

        public void SaveToDisk()
        {
            StringBuilder history = new StringBuilder();

            for (int i = _count; i >= 1; i--)
                history.Append(GetValue(i) + "\n");

            try
            {
                PlayerPrefs.SetString(_name, history.ToString());
            }
            catch (PlayerPrefsException e)
            {
                Debug.LogError(e.Message);
            }
        }
        public void LoadFromDisk()
        {
            string history = PlayerPrefs.GetString(_name);
            string[] split = history.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < _values.Length; i++)
            {
                if (i >= split.Length)
                    break;

                _values[i] = split[i];
            }

            _count = Math.Min(split.Length, _values.Length);
            if (_count == _values.Length)
                _pos = 0;
            else
                _pos = _count;
        }

        public string GetValue(int index)
        {
            if (index <= 0 || index > _values.Length)
                return null;

            int targetPos = (_pos - index + _values.Length) % _values.Length;
            return _values[targetPos];
        }
        public void AddValue(string value)
        {
            _values[_pos] = value;
            _pos = (_pos + 1) % _values.Length;
            _count = Math.Min(_values.Length, _count + 1);
        }
        #endregion
    }
}
