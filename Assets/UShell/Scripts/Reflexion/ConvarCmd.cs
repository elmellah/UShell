using System;
using System.Reflection;

namespace UShell
{
    internal struct ConvarCmd
    {
        private FieldInfo _fieldInfo;
        private PropertyInfo _propertyInfo;


        public bool IsValid { get => IsField ^ IsProperty; }
        public bool IsField { get => _fieldInfo != null; }
        public bool IsProperty { get => _propertyInfo != null; }

        public Type Type
        {
            get
            {
                if (IsField) return _fieldInfo.FieldType;
                if (IsProperty) return _propertyInfo.PropertyType;

                return typeof(object);
            }
        }
        public Type DeclaringType
        {
            get
            {
                if (IsField) return _fieldInfo.DeclaringType;
                if (IsProperty) return _propertyInfo.DeclaringType;

                return typeof(object);
            }
        }
        public bool IsStatic
        {
            get
            {
                if (IsField)
                    return _fieldInfo.IsStatic;
                if (IsProperty)
                {
                    MethodInfo methodInfo = _propertyInfo.GetMethod;
                    if (methodInfo != null)
                        return methodInfo.IsStatic;
                }

                return true;
            }
        }

        public string Name
        {
            get
            {
                if (IsField)
                {
                    ConvarAttribute attribute = _fieldInfo.GetCustomAttribute<ConvarAttribute>();
                    if (attribute != null && !string.IsNullOrEmpty(attribute.Label))
                        return attribute.Label;
                    else
                        return _fieldInfo.Name;
                }
                if (IsProperty)
                {
                    ConvarAttribute attribute = _propertyInfo.GetCustomAttribute<ConvarAttribute>();
                    if (attribute != null && !string.IsNullOrEmpty(attribute.Label))
                        return attribute.Label;
                    else
                        return _propertyInfo.Name;
                }

                return "";
            }
        }
        public string Info
        {
            get
            {
                if (IsField)
                {
                    ConvarAttribute attribute = _fieldInfo.GetCustomAttribute<ConvarAttribute>();
                    if (attribute != null && !string.IsNullOrEmpty(attribute.Info))
                        return attribute.Info;
                }
                if (IsProperty)
                {
                    ConvarAttribute attribute = _propertyInfo.GetCustomAttribute<ConvarAttribute>();
                    if (attribute != null && !string.IsNullOrEmpty(attribute.Info))
                        return attribute.Info;
                }

                return "";
            }
        }
        public bool CanRead
        {
            get
            {
                if (IsField)
                    return true;
                if (IsProperty)
                    return _propertyInfo.CanRead;

                return false;
            }
        }
        public bool CanWrite
        {
            get
            {
                if (IsField)
                {
                    ConvarAttribute attribute = _fieldInfo.GetCustomAttribute<ConvarAttribute>();
                    if (attribute != null)
                        return !attribute.ReadOnly;
                }
                if (IsProperty)
                {
                    ConvarAttribute attribute = _propertyInfo.GetCustomAttribute<ConvarAttribute>();
                    if (attribute != null)
                        return _propertyInfo.CanWrite && !attribute.ReadOnly;
                }

                return false;
            }
        }


        public ConvarCmd(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
            _propertyInfo = null;
        }
        public ConvarCmd(PropertyInfo propertyInfo)
        {
            _fieldInfo = null;
            _propertyInfo = propertyInfo;
        }

        public object GetValue(object obj)
        {
            if (IsField)
                return _fieldInfo.GetValue(obj);
            else if (IsProperty)
                return _propertyInfo.GetValue(obj);

            return null;
        }
        public void SetValue(object obj, object value)
        {
            if (IsField)
                _fieldInfo.SetValue(obj, value);
            else if (IsProperty)
                _propertyInfo.SetValue(obj, value);
        }
    }
}
