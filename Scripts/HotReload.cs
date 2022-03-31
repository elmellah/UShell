using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
///     Provide a simple interface for a <c>MonoBehaviour</c> to be compatible with hot reload.<br/>
///     No need to add the <c>HotReload</c> script to the scene, it will be added automatically.
/// </summary>
/// <remarks>
///     1- Add the following code at the top of the <c>Awake</c> method:
///     <code>
///         HotReload.Register(this);
///     </code>
///     2- Add the following code at the top of your <c>OnEnable</c> method (if any):
///     <code>
///         if (!HotReload.ExecuteOnEnable)
///             return;
///     </code>
///     3- Use the <c>IsHotReload</c> property to know if your code is executing before or after a hot reload<br/>
///     4- Serialization: work in progress...
/// </remarks>
public class HotReload : MonoBehaviour
{
    #region FIELDS
    private const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const string _gameObjectName = "Hot Reload";

    private static HotReload _instance;
    /// <summary>
    /// As it is static, this field will be reset to true after a hot reload
    /// </summary>
    private static bool _hotReload = true;

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

    public static bool ExecuteOnEnable => !_hotReload || main._executeOnEnable;
    public static bool IsHotReload => _hotReload;
    #endregion

    #region MESSAGES
    /// <summary>
    /// Awake will be executed only after entering play mode
    /// </summary>
    void Awake() => _hotReload = _enabled;
    /// <summary>
    /// OnEnable will be executed after entering play mode AND after hot reload
    /// </summary>
    void OnEnable()
    {
        _instance = this;

        _enabled = true;
        if (!_hotReload)
            return;

        Stopwatch watch = Stopwatch.StartNew();

        _executeOnEnable = true;
        // We browse the array from the last element (the first to have registered) to the first (the last to have registered) to allow deletion of elements
        // We start executing all the Awake and OnEnable methods in the order they registered...
        for (int i = _behaviours.Count - 1; i >= 0; i--)
        {
            if (removeIfNull(i))
                continue;

            var b = _behaviours[i];
            call(b, "Awake");
            call(b, "OnEnable");
        }

        // ... then we execute all the Start methods in the order they registered
        for (int i = _behaviours.Count - 1; i >= 0; i--)
        {
            if (removeIfNull(i))
                continue;

            var b = _behaviours[i];
            call(b, "Start");
        }
        _executeOnEnable = false;

        Debug.Log("hot reload: done in " + watch.ElapsedMilliseconds + "ms");
    }
    #endregion

    #region METHODS
    public static void Register(MonoBehaviour behaviour)
    {
        // We are inserting each monobehaviour at the begining of the array to allow the deletion of the elements while browsing the array
        if (!main._behaviours.Contains(behaviour))
            main._behaviours.Insert(0, behaviour);
    }

    private static void call(object obj, string methodName)
    {
        var method = obj.GetType().GetMethod(methodName, _bindingFlags, null, new Type[0], null);
        method?.Invoke(obj, null);
    }
    private bool removeIfNull(int behaviourIndex)
    {
        if (_behaviours[behaviourIndex] == null)
        {
            _behaviours.RemoveAt(behaviourIndex);
            return true;
        }

        return false;
    }
    #endregion

}
