using UnityEngine;
using System;
using System.Globalization;
using System.ComponentModel;

namespace UShell
{
    public class Converters
    {
        public class Vector2Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return false;
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return default;
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                return false;
            }
        }
        public class Vector3Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string))
                    return true;

                return base.CanConvertFrom(context, sourceType);
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    if (Utils.TryParseVector3((string)value, NumberStyles.Float, CultureInfo.InvariantCulture, out Vector3 result))
                        return result;
                }

                return base.ConvertFrom(context, culture, value);
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                if (value.GetType() == typeof(Vector3))
                    return true;

                return base.IsValid(context, value);
            }
        }
        public class QuaternionConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string))
                    return true;

                return base.CanConvertFrom(context, sourceType);
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    if (Utils.TryParseQuaternion((string)value, NumberStyles.Float, CultureInfo.InvariantCulture, out Quaternion result))
                        return result;
                }

                return base.ConvertFrom(context, culture, value);
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                if (value.GetType() == typeof(Quaternion))
                    return true;

                return base.IsValid(context, value);
            }
        }
        public class ColorConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return false;
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return default;
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                return false;
            }
        }
        public class Color32Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return false;
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return default;
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                return false;
            }
        }
    }
}
