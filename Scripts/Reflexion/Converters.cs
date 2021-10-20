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
                if (sourceType == typeof(string))
                    return true;

                return base.CanConvertFrom(context, sourceType);
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    if (Utils.TryParseVector2((string)value, NumberStyles.Float, CultureInfo.InvariantCulture, out Vector2 result))
                        return result;
                }

                return base.ConvertFrom(context, culture, value);
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                if (value.GetType() == typeof(Vector2))
                    return true;

                return base.IsValid(context, value);
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
        public class Vector4Converter : TypeConverter
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
                    if (Utils.TryParseVector4((string)value, NumberStyles.Float, CultureInfo.InvariantCulture, out Vector4 result))
                        return result;
                }

                return base.ConvertFrom(context, culture, value);
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                if (value.GetType() == typeof(Vector4))
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
                if (sourceType == typeof(string))
                    return true;

                return base.CanConvertFrom(context, sourceType);
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    if (Utils.TryParseColor((string)value, NumberStyles.Float, CultureInfo.InvariantCulture, out Color result))
                        return result;
                }

                return base.ConvertFrom(context, culture, value);
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                if (value.GetType() == typeof(Color))
                    return true;

                return base.IsValid(context, value);
            }
        }
        public class Color32Converter : TypeConverter
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
                    if (Utils.TryParseColor32((string)value, NumberStyles.Integer, CultureInfo.InvariantCulture, out Color32 result))
                        return result;
                }

                return base.ConvertFrom(context, culture, value);
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                if (value.GetType() == typeof(Color32))
                    return true;

                return base.IsValid(context, value);
            }
        }
        public class RectConverter : TypeConverter
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
                    if (Utils.TryParseRect((string)value, NumberStyles.Float, CultureInfo.InvariantCulture, out Rect result))
                        return result;
                }

                return base.ConvertFrom(context, culture, value);
            }
            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                if (value.GetType() == typeof(Rect))
                    return true;

                return base.IsValid(context, value);
            }
        }
    }
}
