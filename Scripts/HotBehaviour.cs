using UnityEngine;
using System;

public abstract class HotBehaviour : MonoBehaviour
#if UNITY_EDITOR
    , ISerializationCallbackReceiver
#endif
{
#if UNITY_EDITOR
    [NonSerialized]
    protected static bool __hotReload = true;
    [NonSerialized]
    private bool __enabled = false;

    protected bool IsHotReload { get => __hotReload; }

    public virtual void OnAfterDeserialize() { }
    public virtual void OnBeforeSerialize() { }

    /// <summary>
    /// Must be called at the beginning of the Awake method
    /// </summary>
    protected void __Awake()
    {
        if (!__enabled)
            __hotReload = false;
    }
    /// <summary>
    /// Must be called at the beginning of the OnEnable method
    /// </summary>
    protected void __CallAwakeIfHotReload()
    {
        __enabled = true;
        if (__hotReload)
        {
            var method = this.GetType().GetMethod("Awake", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new System.Type[0], null);
            method?.Invoke(this, null);
        }
    }
    /// <summary>
    /// Must be called at the end of the OnEnable method
    /// </summary>
    protected void __CallStartIfHotReload()
    {
        if (IsHotReload)
        {
            var method = this.GetType().GetMethod("Start", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new System.Type[0], null);
            method?.Invoke(this, null);
        }
    }
#else
    protected const bool IsHotReload = false;

    protected void __Awake() { }
    protected void __CallAwakeIfHotReload() { }
    protected void __CallStartIfHotReload() { }
#endif
}
