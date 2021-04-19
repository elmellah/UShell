using System;

namespace UShell
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConvarAttribute : Attribute
    {
        public string Label { get; }
        public string Info { get; }
        public bool ReadOnly { get; }

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
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CmdAttribute : Attribute
    {
        public string Label { get; }
        public string Info { get; }

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
    }

    [AttributeUsage(AttributeTargets.Event)]
    public class EventAttribute : Attribute
    {
        public string Label { get; }
        public string Info { get; }

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
    }
}
