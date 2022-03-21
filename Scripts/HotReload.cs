using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class HotReload : MonoBehaviour
{
    #region FIELDS
    private const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const string _gameObjectName = "Hot Reload";

    private static HotReload _instance;
    protected static bool _hotReload = true;

    [NonSerialized]
    private bool _enabled = false;
    [NonSerialized]
    private bool _executeOnEnable = false;

    private List<MonoBehaviour> _behaviours = new List<MonoBehaviour>();
    #endregion

    #region PROPERTIES
    private static HotReload main
    {
        get
        {
            if (_instance != null)
                return _instance;

            GameObject go = new GameObject(_gameObjectName);
            DontDestroyOnLoad(go);
            return go.AddComponent<HotReload>();
        }
    }

    public static bool ExecuteOnEnable { get => !_hotReload || main._executeOnEnable; }
    public static bool IsHotReload { get => _hotReload; }
    #endregion

    #region MESSAGES
    void Awake() => _hotReload = _enabled;
    void OnEnable()
    {
        _instance = this;

        _enabled = true;
        if (!_hotReload)
            return;

        Stopwatch watch = Stopwatch.StartNew();

        _executeOnEnable = true;
        for (int i = _behaviours.Count - 1; i >= 0; i--)
        {
            var b = _behaviours[i];
            if (b == null)
            {
                _behaviours.RemoveAt(i);
                continue;
            }

            call(b, "Awake");
            call(b, "OnEnable");
        }

        for (int i = _behaviours.Count - 1; i >= 0; i--)
        {
            var b = _behaviours[i];
            if (b == null)
            {
                _behaviours.RemoveAt(i);
                continue;
            }

            call(b, "Start");
        }
        _executeOnEnable = false;

        Debug.Log("hot reload: done in " + watch.ElapsedMilliseconds + "ms");
    }
    #endregion

    #region METHODS
    public static void Register(MonoBehaviour behaviour)
    {
        if (!main._behaviours.Contains(behaviour))
            main._behaviours.Insert(0, behaviour);
    }

    private static void call(object obj, string methodName)
    {
        var method = obj.GetType().GetMethod(methodName, _bindingFlags, null, new Type[0], null);
        method?.Invoke(obj, null);
    }
    #endregion

}
