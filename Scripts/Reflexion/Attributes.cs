using System;

namespace UShell
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConvarAttribute : Attribute
    {
        public string Label { get; }
        public string Info { get; } = null;
        public bool ReadOnly { get; } = false;
        public bool DevOnly { get; } = true;

        public ConvarAttribute() { }
        public ConvarAttribute(string label)
        {
            this.Label = label;
        }
        public ConvarAttribute(string label, string info)
        {
            this.Label = label;
            this.Info = info;
        }
        public ConvarAttribute(string label, string info, bool readOnly)
        {
            this.Label = label;
            this.Info = info;
            this.ReadOnly = readOnly;
        }
        public ConvarAttribute(string label, string info, bool readOnly, bool devOnly)
        {
            this.Label = label;
            this.Info = info;
            this.ReadOnly = readOnly;
            this.DevOnly = devOnly;
        }
        public ConvarAttribute(string label, bool readOnly)
        {
            this.Label = label;
            this.ReadOnly = readOnly;
        }
        public ConvarAttribute(string label, bool readOnly, bool devOnly)
        {
            this.Label = label;
            this.ReadOnly = readOnly;
            this.DevOnly = devOnly;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CmdAttribute : Attribute
    {
        public string Label { get; }
        public string Info { get; } = null;
        public bool DevOnly { get; } = true;

        public CmdAttribute() { }
        public CmdAttribute(string label)
        {
            this.Label = label;
        }
        public CmdAttribute(string label, string info)
        {
            this.Label = label;
            this.Info = info;
        }
        public CmdAttribute(string label, string info, bool devOnly)
        {
            this.Label = label;
            this.Info = info;
            this.DevOnly = devOnly;
        }
        public CmdAttribute(string label, bool devOnly)
        {
            this.Label = label;
            this.DevOnly = devOnly;
        }
    }

    [AttributeUsage(AttributeTargets.Event)]
    public class EventAttribute : Attribute
    {
        public string Label { get; }
        public string Info { get; } = null;
        public bool DevOnly { get; } = true;

        public EventAttribute() { }
        public EventAttribute(string label)
        {
            this.Label = label;
        }
        public EventAttribute(string label, string info)
        {
            this.Label = label;
            this.Info = info;
        }
        public EventAttribute(string label, string info, bool devOnly)
        {
            this.Label = label;
            this.Info = info;
            this.DevOnly = devOnly;
        }
        public EventAttribute(string label, bool devOnly)
        {
            this.Label = label;
            this.DevOnly = devOnly;
        }
    }
}
