using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UShell
{
    public struct Token
    {
        public enum Type
        {
            WORD,
            SEPARATOR,
            NEWLINE,
        }

        public Type type;
        public string value;

        public Token(Type type, string value)
        {
            this.type = type;
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            return obj is Token token &&
                   type == token.type &&
                   value == token.value;
        }
        public override int GetHashCode()
        {
            int hashCode = 1148455455;
            hashCode = hashCode * -1521134295 + type.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(value);
            return hashCode;
        }
        public override string ToString()
        {
            return type + ":" + value;
        }
    }

    public struct Option
    {
        public char Value { get; }
        public bool ExpectParam { get; }

        public Option(char value)
        {
            this.Value = value;
            this.ExpectParam = false;
        }
        public Option(char value, bool expectParam)
        {
            this.Value = value;
            this.ExpectParam = expectParam;
        }

        public override bool Equals(object obj)
        {
            return obj is Option option &&
                   Value == option.Value &&
                   ExpectParam == option.ExpectParam;
        }
        public override int GetHashCode()
        {
            int hashCode = -1416359997;
            hashCode = hashCode * -1521134295 + Value.GetHashCode();
            hashCode = hashCode * -1521134295 + ExpectParam.GetHashCode();
            return hashCode;
        }
        public override string ToString()
        {
            return base.ToString();
        }
    }

    public static class Utils
    {
        const string parseError = "cannot parse '{0}' to {1}";

        #region COMPLETION
        public static string GetCompletion(ref string prefix, bool testForEquality, out List<string> options, params IEnumerable<string>[] lexicon)
        {
            string result = GetCompletion(prefix, testForEquality, out options, lexicon);
            if (testForEquality)
                prefix += result;
            else if (result.Length > 0)
                prefix += result.Substring(0, result.Length - 1);
            return result;
        }
        public static string GetCompletion(string prefix, bool testForEquality, out List<string> options, params IEnumerable<string>[] lexicon)
        {
            options = new List<string>();

            if (testForEquality)
            {
                for (int i = 0; i < lexicon.Length; i++)
                {
                    if (lexicon[i].Contains(prefix))
                    {
                        options.Add(prefix);
                        break;
                    }
                }

                if (options.Count <= 0)
                    return "";
            }
            else
                options = GetWordsThatStartWith(prefix, false, lexicon);

            if (options.Count == 0)
                return "";
            else if (options.Count == 1)
                return options[0].Remove(0, prefix.Length) + (testForEquality ? "" : " ");
            else
                return GetLongestCommonPrefix(options).Remove(0, prefix.Length);
        }

        public static List<string> GetWordsThatStartWith(string prefix, bool ignoreCase, params IEnumerable<string>[] lexicon)
        {
            List<string> words = new List<string>();

            for (int i = 0; i < lexicon.Length; i++)
            {
                foreach (string word in lexicon[i])
                {
                    if (!words.Contains(word) && word.StartsWith(prefix, ignoreCase, CultureInfo.InvariantCulture))
                        words.Add(word);
                }
            }

            return words;
        }
        public static string GetLongestCommonPrefix(List<string> list)
        {
            int lcpCount = list[0].Length;

            for (int i = 0; i < list.Count - 1; i++)
                lcpCount = Math.Min(lcpCount, Utils.GetCommonPrefixLength(list[i], list[i + 1]));

            return list[0].Substring(0, lcpCount);
        }
        public static int GetCommonPrefixLength(string a, string b)
        {
            int minl = Math.Min(a.Length, b.Length);

            for (int i = 1; i <= minl; i++)
            {
                if (!a.StartsWith(b.Substring(0, i), false, CultureInfo.InvariantCulture))
                    return i - 1;
            }

            return minl;
        }
        #endregion

        #region CONVERSION
        public static string ConvertUnityRichTextToBashText(string text)
        {
            text = text.Replace("</color>", @"\e[39m");
            text = text.Replace("<color=#" + ColorUtility.ToHtmlStringRGBA(Color.black) + ">", @"\e[30m");
            text = text.Replace("<color=#" + ColorUtility.ToHtmlStringRGBA(Color.red) + ">", @"\e[31m");
            text = text.Replace("<color=#" + ColorUtility.ToHtmlStringRGBA(Color.green) + ">", @"\e[32m");
            text = text.Replace("<color=#" + ColorUtility.ToHtmlStringRGBA(Color.yellow) + ">", @"\e[33m");
            text = text.Replace("<color=#" + ColorUtility.ToHtmlStringRGBA(Color.blue) + ">", @"\e[34m");
            text = text.Replace("<color=#" + ColorUtility.ToHtmlStringRGBA(Color.magenta) + ">", @"\e[35m");
            text = text.Replace("<color=#" + ColorUtility.ToHtmlStringRGBA(Color.cyan) + ">", @"\e[36m");
            text = text.Replace("<color=#" + ColorUtility.ToHtmlStringRGBA(Color.white) + ">", @"\e[97m");

            text += @"\e[39m";

            //To do: do not remove tags surrounded by <noparse></noparse> (TMPro only!)
            return Regex.Replace(text, "<.*?>", String.Empty);
        }

        public static string Convert2DArrayToString(string[][] array, bool drawBorder = true, int margin = 0)
        {
            int[] sizes = new int[array[0].Length];

            for (int i = 0; i < array.Length; i++)
                for (int j = 0; j < sizes.Length; j++)
                    sizes[j] = array[i][j].Length > sizes[j] ? array[i][j].Length : sizes[j];

            string format = "";
            for (int i = 0; i < sizes.Length; i++)
                format += "{" + i + ", " + sizes[i] + "}" + (drawBorder ? "|" : "") + new string(' ', margin);

            string result = "";
            for (int i = 0; i < array.Length; i++)
                result += string.Format(format, array[i]) + "\n";

            return result;
        }
        public static string ConvertToString(object obj)
        {
            StringBuilder stringBuilder = new StringBuilder();
            convertToStringInternal(obj, stringBuilder, 0);
            return stringBuilder.ToString();
        }
        private static void convertToStringInternal(object obj, StringBuilder stringBuilder, int depth)
        {
            if (obj == null)
            {
                stringBuilder.Append('\t', depth).Append("[null]");
                return;
            }
            else if (obj.GetType() == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(obj.GetType()))
            {
                stringBuilder.Append('\t', depth).Append(obj);
                return;
            }

            IEnumerable enumerable = (IEnumerable)obj;
            stringBuilder.Append('\t', depth).Append("{\n");
            foreach (object o in enumerable)
            {
                convertToStringInternal(o, stringBuilder, depth + 1);
                stringBuilder.Append("\n");
            }
            stringBuilder.Append('\t', depth).Append("}");
        }

        public static object ConvertFromString(string input, Type T)
        {
            MethodInfo method = typeof(Utils).GetMethod(nameof(Utils.ConvertFromStringGeneric), new[] { typeof(string) }).MakeGenericMethod(T);
            return method.Invoke(null, new[] { input });
        }
        public static T ConvertFromStringGeneric<T>(string input)
        {
            const string invalidValue = "invalid value";
            const string cannotConvert = "no converter available for ";

            if (!typeof(T).IsValueType && input == "null")
                return default(T);

            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter.CanConvertFrom(typeof(string)))
            {
                if (converter.IsValid(input))
                    return (T)converter.ConvertFromInvariantString(input);
                else
                    throw new FormatException(invalidValue);
            }
            else
            {
                var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static);
                if (methods.Length > 0)
                {
                    IEnumerable<MethodInfo> operators =
                        from method in methods
                        where method.Name == "op_Explicit"
                        where method.ReturnType == typeof(T)
                        where method.GetParameters().Length == 1
                        select method;

                    foreach (var op in operators)
                    {
                        try
                        {
                            object result = ConvertFromString(input, op.GetParameters()[0].ParameterType);
                            return (T)op.Invoke(null, new[] { result });
                        }
                        catch { }
                    }
                }

                throw new InvalidOperationException(cannotConvert + typeof(T));
            }
        }
        public static object ConvertFromString(string[] input, Type T)
        {
            MethodInfo method = typeof(Utils).GetMethod(nameof(Utils.ConvertFromStringGeneric), new[] { typeof(string[]) }).MakeGenericMethod(T);
            return method.Invoke(null, new[] { input });
        }
        public static T[] ConvertFromStringGeneric<T>(string[] input)
        {
            T[] result = new T[input.Length];
            for (int i = 0; i < input.Length; i++)
                result[i] = ConvertFromStringGeneric<T>(input[i]);

            return result;
        }

        public static bool TryParseVector2(string s, NumberStyles style, IFormatProvider provider, out Vector2 result)
        {
            string[] scalars = s.Split(',');
            if (scalars.Length == 2)
            {
                if (float.TryParse(scalars[0], style, provider, out float scalar0) &&
                float.TryParse(scalars[1], style, provider, out float scalar1)
                )
                {
                    result = new Vector2(scalar0, scalar1);
                    return true;
                }
            }

            result = Vector2.zero;
            return false;
        }
        public static bool TryParseVector3(string s, NumberStyles style, IFormatProvider provider, out Vector3 result)
        {
            string[] scalars = s.Split(',');
            if (scalars.Length == 3)
            {
                if (float.TryParse(scalars[0], style, provider, out float scalar0) &&
                float.TryParse(scalars[1], style, provider, out float scalar1) &&
                float.TryParse(scalars[2], style, provider, out float scalar2)
                )
                {
                    result = new Vector3(scalar0, scalar1, scalar2);
                    return true;
                }
            }

            result = Vector3.zero;
            return false;
        }
        public static bool TryParseVector4(string s, NumberStyles style, IFormatProvider provider, out Vector4 result)
        {
            string[] scalars = s.Split(',');
            if (scalars.Length == 4)
            {
                if (float.TryParse(scalars[0], style, provider, out float scalar0) &&
                float.TryParse(scalars[1], style, provider, out float scalar1) &&
                float.TryParse(scalars[2], style, provider, out float scalar2) &&
                float.TryParse(scalars[3], style, provider, out float scalar3)
                )
                {
                    result = new Vector4(scalar0, scalar1, scalar2, scalar3);
                    return true;
                }
            }

            result = Vector4.zero;
            return false;
        }
        public static bool TryParseQuaternion(string s, NumberStyles style, IFormatProvider provider, out Quaternion result)
        {
            string[] scalars = s.Split(',');
            if (scalars.Length == 3)
            {
                if (float.TryParse(scalars[0], style, provider, out float scalar0) &&
                float.TryParse(scalars[1], style, provider, out float scalar1) &&
                float.TryParse(scalars[2], style, provider, out float scalar2)
                )
                {
                    result = Quaternion.Euler(scalar0, scalar1, scalar2);
                    return true;
                }
            }
            else if (scalars.Length == 4)
            {
                if (float.TryParse(scalars[0], style, provider, out float scalar0) &&
                float.TryParse(scalars[1], style, provider, out float scalar1) &&
                float.TryParse(scalars[2], style, provider, out float scalar2) &&
                float.TryParse(scalars[3], style, provider, out float scalar3)
                )
                {
                    result = new Quaternion(scalar0, scalar1, scalar2, scalar3);
                    return true;
                }
            }

            result = Quaternion.identity;
            return false;
        }
        public static bool TryParseColor(string s, NumberStyles style, IFormatProvider provider, out Color result)
        {
            float scalar0, scalar1, scalar2, scalar3;
            string[] scalars = s.Split(',');

            if (scalars.Length == 3)
            {
                if (float.TryParse(scalars[0], style, provider, out scalar0) &&
                float.TryParse(scalars[1], style, provider, out scalar1) &&
                float.TryParse(scalars[2], style, provider, out scalar2)
                )
                {
                    result = new Color(scalar0, scalar1, scalar2);
                    return true;
                }
            }
            else if (scalars.Length == 4)
            {
                if (float.TryParse(scalars[0], style, provider, out scalar0) &&
                float.TryParse(scalars[1], style, provider, out scalar1) &&
                float.TryParse(scalars[2], style, provider, out scalar2) &&
                float.TryParse(scalars[3], style, provider, out scalar3)
                )
                {
                    result = new Color(scalar0, scalar1, scalar2, scalar3);
                    return true;
                }
            }

            result = Color.clear;
            return false;
        }
        public static bool TryParseColor32(string s, NumberStyles style, IFormatProvider provider, out Color32 result)
        {
            byte scalar0, scalar1, scalar2, scalar3;
            string[] scalars = s.Split(',');
            
            if (scalars.Length == 4)
            {
                if (byte.TryParse(scalars[0], style, provider, out scalar0) &&
                byte.TryParse(scalars[1], style, provider, out scalar1) &&
                byte.TryParse(scalars[2], style, provider, out scalar2) &&
                byte.TryParse(scalars[3], style, provider, out scalar3)
                )
                {
                    result = new Color32(scalar0, scalar1, scalar2, scalar3);
                    return true;
                }
            }

            result = new Color32();
            return false;
        }
        public static bool TryParseRect(string s, NumberStyles style, IFormatProvider provider, out Rect result)
        {
            string[] scalars = s.Split(',');
            if (scalars.Length == 4)
            {
                if (float.TryParse(scalars[0], style, provider, out float scalar0) &&
                float.TryParse(scalars[1], style, provider, out float scalar1) &&
                float.TryParse(scalars[2], style, provider, out float scalar2) &&
                float.TryParse(scalars[3], style, provider, out float scalar3)
                )
                {
                    result = new Rect(scalar0, scalar1, scalar2, scalar3);
                    return true;
                }
            }

            result = Rect.zero;
            return false;
        }
        public static void TryParseArray(string s, out object result, Type T)
        {
            MethodInfo method = typeof(Utils).GetMethod(nameof(Utils.TryParseArrayGeneric)).MakeGenericMethod(T);
            object[] parameters = new object[] { s, null };
            method.Invoke(null, parameters);
            result = parameters[1];
        }
        public static void TryParseArrayGeneric<T>(string s, out T[] result)
        {
            if (string.IsNullOrEmpty(s))
            {
                result = new T[0];
                return;
            }

            string[] elements = s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            result = new T[elements.Length];
            for (int i = 0; i < elements.Length; i++)
                result[i] = ConvertFromStringGeneric<T>(elements[i]);
        }


        public static TEnum EnumParse<TEnum>(string value, bool ignoreCase) where TEnum : struct
        {
            if (Enum.TryParse<TEnum>(value, ignoreCase, out TEnum result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(TEnum).Name));
        }

        public static bool BoolParse(string value)
        {
            if (bool.TryParse(value, out bool result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(bool).Name));
        }
        public static char CharParse(string value)
        {
            if (char.TryParse(value, out char result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(char).Name));
        }
        public static decimal DecimalParse(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(decimal).Name));
        }

        public static sbyte SByteParse(string value)
        {
            if (sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(sbyte).Name));
        }
        public static short ShortParse(string value)
        {
            if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(short).Name));
        }
        public static int IntParse(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(int).Name));
        }
        public static long LongParse(string value)
        {
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(long).Name));
        }

        public static byte ByteParse(string value)
        {
            if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(byte).Name));
        }
        public static ushort UShortParse(string value)
        {
            if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(ushort).Name));
        }
        public static uint UIntParse(string value)
        {
            if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(uint).Name));
        }
        public static ulong ULongParse(string value)
        {
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(ulong).Name));
        }

        public static float FloatParse(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(float).Name));
        }
        public static double DoubleParse(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(double).Name));
        }
        #endregion

        #region FUZZY SEARCH
        public static List<string> GetSimilarWords(string input, bool ignoreCase, int maxDistance, bool sort, params IEnumerable<string>[] lexicon)
        {
            List<string> words = new List<string>();
            List<string>[] unsortedWords = null;
            if (sort)
            {
                unsortedWords = new List<string>[maxDistance + 1];
                for (int i = 0; i < unsortedWords.Length; i++)
                    unsortedWords[i] = new List<string>();
            }

            for (int i = 0; i < lexicon.Length; i++)
            {
                foreach (string word in lexicon[i])
                {
                    int distance = Utils.GetLevenshteinDistance(input, word, ignoreCase);
                    if (!words.Contains(word) && distance <= maxDistance)
                    {
                        if (sort)
                            unsortedWords[distance].Add(word);
                        else
                            words.Add(word);
                    }
                }
            }

            if (sort)
                for (int i = 0; i < unsortedWords.Length; i++)
                    words.AddRange(unsortedWords[i]);

            return words;
        }
        public static int GetLevenshteinDistance(string str0, string str1, bool ignoreCase = false)
        {
            return GetLevenshteinDistance(str0, str0.Length, str1, str1.Length, ignoreCase);
        }
        public static int GetLevenshteinDistance(string str0, int len0, string str1, int len1, bool ignoreCase = false)
        {
            //from https://en.wikipedia.org/wiki/Levenshtein_distance

            // create two work vectors of integer distances
            int[] v0 = new int[len1 + 1];
            int[] v1 = new int[len1 + 1];

            // initialize v0 (the previous row of distances)
            // this row is A[0][i]: edit distance for an empty s
            // the distance is just the number of characters to delete from t
            for (int i = 0; i <= len1; i++)
                v0[i] = i;

            for (int i = 0; i < len0; i++)
            {
                // calculate v1 (current row distances) from the previous row v0

                // first element of v1 is A[i+1][0]
                //   edit distance is delete (i+1) chars from s to match empty t
                v1[0] = i + 1;

                // use formula to fill in the rest of the row
                for (int j = 0; j < len1; j++)
                {
                    // calculating costs for A[i+1][j+1]
                    int deletionCost = v0[j + 1] + 1;
                    int insertionCost = v1[j] + 1;
                    int substitutionCost;
                    if (ignoreCase)
                    {
                        if (char.ToUpperInvariant(str0[i]) == char.ToUpperInvariant(str1[j]))
                            substitutionCost = v0[j];
                        else
                            substitutionCost = v0[j] + 1;
                    }
                    else
                    {
                        if (str0[i] == str1[j])
                            substitutionCost = v0[j];
                        else
                            substitutionCost = v0[j] + 1;
                    }

                    v1[j + 1] = Math.Min(deletionCost, Math.Min(insertionCost, substitutionCost));
                }

                // copy v1 (current row) to v0 (previous row) for next iteration
                // since data in v1 is always invalidated, a swap without copy could be more efficient
                int[] tmp = v0;
                v0 = v1;
                v1 = tmp;
            }

            // after the last swap, the results of v1 are now in v0
            return v0[len1];
        }
        #endregion

        #region PARSING
        /// <summary>
        /// Remove all occurences of separators preceded by an unquoted backslash. The backslash is removed with the separator.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="separators">Separator that contains a '\\' (backslash), a '\'' (single-quote) or a '"' (double-quote) is prohibited</param>
        /// <returns></returns>
        public static string RemoveEscapedSeparators(string input, char[] separators)
        {
            //If there is no separators in the input, only need to return the input -> save GC Alloc

            char[] cleanValue = new char[input.Length];
            bool dq = false; //isDoubleQuoting ('"')
            bool sq = false; //isSingleQuoting ('\'')
            bool esc = false; //isEscaping ('\\')
            int pos = 0;
            for (int j = 0; j < input.Length; j++)
            {
                bool write = true;
                if (sq)
                {
                    if (input[j] == '\'')
                        sq = false;
                }
                else if (dq)
                {
                    if (input[j] == '"')
                    {
                        if (!esc)
                            dq = false;
                        esc = false;
                    }
                    else if (input[j] == '\\')
                    {
                        if (esc)
                            esc = false;
                        else if (input[j + 1] == '"' || input[j + 1] == '\\' || Array.IndexOf(separators, input[j + 1]) > -1)
                        {
                            esc = true;
                            if (Array.IndexOf(separators, input[j + 1]) > -1)
                                write = false;
                        }
                    }
                    else if (Array.IndexOf(separators, input[j]) > -1)
                    {
                        if (esc)
                            write = false;
                        esc = false;
                    }
                }
                else
                {
                    if (input[j] == '\'')
                    {
                        if (!esc)
                            sq = true;
                        esc = false;
                    }
                    else if (input[j] == '"')
                    {
                        if (!esc)
                            dq = true;
                        esc = false;
                    }
                    else if (input[j] == '\\')
                    {
                        if (!esc)
                            if (Array.IndexOf(separators, input[j + 1]) > -1)
                                write = false;
                        esc = !esc;
                    }
                    else if (Array.IndexOf(separators, input[j]) > -1)
                    {
                        if (esc)
                            write = false;
                        esc = false;
                    }
                    else
                        esc = false;
                }
                if (write)
                    cleanValue[pos++] = input[j];
            }
            return new string(cleanValue, 0, pos);
        }
        /// <summary>
        /// Divides the input into tokens (operators and words).
        /// </summary>
        /// <param name="input"></param>
        /// <param name="operators">
        /// <para>
        /// Each operator is actually treated as if it were multiple operators :
        /// { "abc" , "hjoi" } are actually { "a", "ab", "abc", "h", "hj", "hjo", "hjoi" }.
        /// </para>
        /// </param>
        /// <returns></returns>
        public static List<Token> Tokenize(string input, Token[] operators, bool throwExceptions = true)
        {
            //substring -> GC Alloc : to solve, create a new type (Token) which is the data of a string and 2 int, one for the start and on for the end => no string created

            List<Token> tokens = new List<Token>();
            Token.Type id;
            string value;
            bool dQ = false; //isDoubleQuoting ('"')
            bool sQ = false; //isSingleQuoting ('\'')
            bool esc = false; //isEscaping ('\\')
            int tokenStartPosition = 0; //The start position of the current token
            int operatorPosition = 0; //The position relative to the tokenStartPosition (if the token is an operator; else, must be 0)
            int operatorIndex = -1; //Which operator is currently processed

            for (int i = 0; i < input.Length; i++)
            {
                if (!esc && !sQ && !dQ && operatorIndex >= 0)
                {
                    if (operatorPosition < operators[operatorIndex].value.Length && operators[operatorIndex].value[operatorPosition] == input[i])
                    {
                        operatorPosition++;
                        continue;
                    }
                    else
                    {
                        tokens.Add(operators[operatorIndex]);
                        tokenStartPosition = i;
                        operatorIndex = -1;
                        operatorPosition = 0;
                    }
                }

                if ((input[i] == '\\' && !esc && !sQ) || (input[i] == '\'' && !esc && !dQ) || (input[i] == '"' && !esc && !sQ))
                {
                    if (input[i] == '\\')
                        esc = true;
                    else if (input[i] == '\'')
                        sQ = !sQ;
                    else if (input[i] == '"')
                        dQ = !dQ;

                    continue;
                }

                if (!esc && !sQ && !dQ)
                {
                    bool canBeUsedAsTheFirstCharacterOfANewOperator = false;
                    for (int j = 0; j < operators.Length; j++)
                    {
                        if (operators[j].value[0] == input[i])
                        {
                            if (i != tokenStartPosition)
                            {
                                id = Token.Type.WORD;
                                value = input.Substring(tokenStartPosition, i - tokenStartPosition);
                                tokens.Add(new Token(id, value));
                            }

                            tokenStartPosition = i;
                            operatorIndex = j;
                            operatorPosition = 1;
                            canBeUsedAsTheFirstCharacterOfANewOperator = true;
                            break;
                        }
                    }
                    if (canBeUsedAsTheFirstCharacterOfANewOperator)
                        continue;
                }

                if (!esc && !sQ && !dQ && (input[i] == ' ' || input[i] == '\t'))
                {
                    if (i != tokenStartPosition)
                    {
                        id = operatorIndex == -1 ? Token.Type.WORD : operators[operatorIndex].type;
                        value = input.Substring(tokenStartPosition, i - tokenStartPosition);
                        tokens.Add(new Token(id, value));
                    }

                    tokenStartPosition = i + 1;
                    operatorIndex = -1;
                    operatorPosition = 0;
                }
                else if (tokenStartPosition != i)
                {
                    ;
                }
                else if (input[i] == '#')
                {
                    for (int j = i; j < input.Length; j++)
                    {
                        if (input[j] == '\n' || input[j] == '\r')
                        {
                            i = j - 1;
                            tokenStartPosition = j;
                            break;
                        }
                        if (j == input.Length - 1)
                        {
                            i = j;
                            tokenStartPosition = input.Length;
                        }
                    }
                }
                else
                {
                    Debug.Assert(tokenStartPosition == i, "lexer: the current character should be used as the start of a new word!");
                }

                esc = false;
            }


            if (throwExceptions)
            {
                string error = "lexer: missing `{0}` at the end of the string";
                if (esc)
                    throw new Exception(string.Format(error, "\\"));
                else if (sQ)
                    throw new Exception(string.Format(error, "'"));
                else if (dQ)
                    throw new Exception(string.Format(error, "\""));
            }


            if (input.Length != tokenStartPosition)
            {
                id = operatorIndex == -1 ? Token.Type.WORD : operators[operatorIndex].type;
                value = input.Substring(tokenStartPosition, input.Length - tokenStartPosition);
                tokens.Add(new Token(id, value));
            }


            return tokens;
        }
        public static void ExpandTokens(List<Token> tokens, Func<string, string> getParameterValue)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].type == Token.Type.WORD)
                    tokens[i] = new Token(Token.Type.WORD, Utils.ExpandWord(tokens[i].value, getParameterValue));
            }
        }
        public static string ExpandWord(string word, Func<string, string> getParameterValue)
        {
            bool dQ = false; //isDoubleQuoting ('"')
            bool sQ = false; //isSingleQuoting ('\'')
            bool esc = false; //isEscaping ('\\')
            bool isParam = false;
            int index = 0;
            string value = "";

            for (int i = 0; i < word.Length; i++)
            {
                if ((word[i] == '\\' && !esc && !sQ) || (word[i] == '\'' && !esc && !dQ) || (word[i] == '"' && !esc && !sQ))
                {
                    if (word[i] == '\\')
                        esc = true;
                    else if (word[i] == '\'')
                        sQ = !sQ;
                    else if (word[i] == '"')
                        dQ = !dQ;

                    if (isParam)
                        value += getParameterValue(word.Substring(index, i - index));
                    else
                        value += word.Substring(index, i - index);

                    index = i;
                    isParam = false;
                }
                else if (!esc && !sQ && word[i] == '$')
                {
                    if (isParam)
                        value += getParameterValue(word.Substring(index, i - index));
                    else
                        value += word.Substring(index, i - index);

                    index = i + 1;
                    isParam = true;
                }
            }

            if (word.Length != index)
            {
                if (isParam)
                    value += getParameterValue(word.Substring(index, word.Length - index));
                else
                    value += word.Substring(index, word.Length - index);
            }

            return value;
        }
        public static int ResolveAssignment(List<Token> tokens, Action<string, string> setParameterValue)
        {
            int i = 0;
            for (; i < tokens.Count; i++)
            {
                if (!ResolveAssignment(tokens[i].value, setParameterValue))
                    break;
            }

            return i;
        }
        public static bool ResolveAssignment(string word, Action<string, string> setParameterValue)
        {
            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] == '=')
                {
                    string name = word.Substring(0, i);
                    string unquotedName = RemoveQuoting(name);
                    if (unquotedName == name)
                    {
                        string value = word.Substring(i + 1, word.Length - (i + 1));
                        setParameterValue(name, RemoveQuoting(value));
                        return true;
                    }

                    break;
                }
            }

            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
        public static List<List<Token>> Split(List<Token> tokens, Token[] separators)
        {
            List<List<Token>> split = new List<List<Token>>();
            int index = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                if (Array.IndexOf(separators, tokens[i]) < 0)
                {
                    if (index >= split.Count)
                        split.Add(new List<Token>());

                    split[index].Add(tokens[i]);
                }
                else
                {
                    if (index < split.Count)
                        index++;
                }
            }

            return split;
        }
        /// <inheritdoc/>
        public static void RemoveQuoting(List<Token> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
                tokens[i] = new Token(tokens[i].type, RemoveQuoting(tokens[i].value));
        }
        /// <inheritdoc/>
        public static void RemoveQuoting(string[] words)
        {
            for (int i = 0; i < words.Length; i++)
                words[i] = RemoveQuoting(words[i]);
        }
        /// <summary>
        /// Remove unquoted '\\' (backslash), '\'' (single-quote) and '"' (double-quote). A backslash that is inside double-quotes is removed only if followed by a backslash or a double-quote.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveQuoting(string input)
        {
            char[] cleanWord = new char[input.Length];
            int pos = 0;

            bool dQ = false; //isDoubleQuoting ('"')
            bool sQ = false; //isSingleQuoting ('\'')
            bool esc = false; //isEscaping ('\\')


            for (int i = 0; i < input.Length; i++)
            {
                if (sQ)
                {
                    if (input[i] == '\'')
                        sQ = false;
                    else
                        cleanWord[pos++] = input[i];
                }
                else if (dQ)
                {
                    if (input[i] == '"')
                    {
                        if (esc)
                        {
                            esc = false;
                            cleanWord[pos++] = input[i];
                        }
                        else
                            dQ = false;
                    }
                    else if (input[i] == '\\')
                    {
                        if (esc)
                        {
                            esc = false;
                            cleanWord[pos++] = input[i];
                        }
                        else if (input[i + 1] == '"' || input[i + 1] == '\\'/*|| input[i + 1] == '\n'*/) //????
                            esc = true;
                        else
                            cleanWord[pos++] = input[i];
                    }
                    else
                    {
                        esc = false;
                        cleanWord[pos++] = input[i];
                    }
                }
                else
                {
                    if (input[i] == '\'')
                    {
                        if (esc)
                        {
                            esc = false;
                            cleanWord[pos++] = input[i];
                        }
                        else
                            sQ = true;
                    }
                    else if (input[i] == '"')
                    {
                        if (esc)
                        {
                            esc = false;
                            cleanWord[pos++] = input[i];
                        }
                        else
                            dQ = true;
                    }
                    else if (input[i] == '\\')
                    {
                        if (esc)
                        {
                            esc = false;
                            cleanWord[pos++] = input[i];
                        }
                        else
                            esc = true;
                    }
                    else
                    {
                        esc = false;
                        cleanWord[pos++] = input[i];
                    }
                }
            }
            
            return new string(cleanWord, 0, pos);
        }
        public static void Parse(List<Token> tokens)
        {
            if (tokens.Count > 0 && tokens[0].type == Token.Type.SEPARATOR)
                throw new Exception("parser: invalid token '" + tokens[0].value + "' at the start of the string");

            bool lastIsSeparator = false;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].type == Token.Type.SEPARATOR)
                {
                    if (lastIsSeparator)
                        throw new Exception("parser: invalid token '" + tokens[i].value + "'");
                    
                    lastIsSeparator = true;
                }
                else
                {
                    lastIsSeparator = false;
                }
            }
        }
        public static string[] ExtractArguments(List<Token> tokens)
        {
            string[] args = new string[tokens.Count - 1];
            for (int i = 0; i < args.Length; i++)
                args[i] = tokens[i + 1].value;

            return args;
        }
        public static List<char> GetOptions(string[] fields, Option[] options, out List<string> args)
        {
            var result = new List<char>();
            args = new List<string>();

            int optionIndex = -1;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i][0] == '-')
                {
                    for (int j = 1; j < fields[i].Length; j++)
                    {
                        if (optionIndex > -1 && options[optionIndex].ExpectParam)
                            throw new Exception("expecting parameter for option " + options[optionIndex]);

                        for (int k = 0; k < options.Length; k++)
                        {
                            if (options[k].Value == fields[i][j])
                            {
                                optionIndex = k;
                                break;
                            }
                        }

                        if (optionIndex > -1)
                        {
                            if (!result.Contains(fields[i][j]))
                                result.Insert(0, fields[i][j]);
                            else
                                throw new Exception("option " + fields[i][j] + " duplicated");
                        }
                        else
                            throw new Exception("invalid option " + fields[i][j]);
                    }
                }
                else
                {
                    if (optionIndex > -1 && options[optionIndex].ExpectParam)
                        args.Insert(0, fields[i]);
                    else
                        args.Add(fields[i]);

                    optionIndex = -1;
                }
            }

            return result;
        }
        #endregion
    }
}
