using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.ComponentModel;
using UnityEditor;

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
    }

    public static class Utils
    {
        const string parseError = "cannot parse '{0}' to {1}";

        #region COMPLETION
        public static string GetCompletion(string prefix, out List<string> options, params IEnumerator[] enumerators)
        {
            List<string> matches = new List<string>();

            if (prefix.EndsWith(" ") || prefix.EndsWith("\t"))
            {
                bool containsKey = false;
                for (int i = 0; i < enumerators.Length; i++)
                {
                    while (enumerators[i].MoveNext())
                    {
                        if (enumerators[i].Current.ToString() == prefix)
                        {
                            matches = new List<string>();
                            matches.Add(prefix);
                            containsKey = true;
                            break;
                        }
                    }
                }

                if (!containsKey)
                {
                    options = new List<string>();
                    return prefix;
                }
            }
            else
            {
                matches = Utils.GetWordsThatStartWith(prefix, true, enumerators);
            }


            if (matches.Count == 0)
            {
                options = new List<string>();
                return prefix;
            }
            else if (matches.Count == 1)
            {
                options = matches;
                return matches[0];
            }
            else
            {
                options = matches;
                return Utils.GetLongestCommonPrefix(matches);
            }
        }

        public static List<string> GetWordsThatStartWith(string prefix, bool ignoreCase, params IEnumerator[] enumerators)
        {
            List<string> words = new List<string>();

            for (int i = 0; i < enumerators.Length; i++)
            {
                while (enumerators[i].MoveNext())
                {
                    string word = enumerators[i].Current.ToString();
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
                if (!a.StartsWith(b.Substring(0, i), false, CultureInfo.InvariantCulture))
                    return i - 1;

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

            IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
            stringBuilder.Append('\t', depth).Append("{\n");
            while (enumerator.MoveNext())
            {
                convertToStringInternal(enumerator.Current, stringBuilder, depth + 1);
                stringBuilder.Append("\n");
            }
            stringBuilder.Append('\t', depth).Append("}");
        }
        
        public static object ConvertFromString(TypeConverter converter, string input)
        {
            const string invalidValue = "invalid value";
            const string cannotConvert = "no converter available";

            if (converter.CanConvertFrom(typeof(string)))
            {
                if (converter.IsValid(input))
                    return converter.ConvertFromInvariantString(input);
                else
                    throw new FormatException(invalidValue);
            }
            else
                throw new InvalidOperationException(cannotConvert);
        }

        public static bool TryParseVector3(string s, NumberStyles style, IFormatProvider provider, out Vector3 result)
        {
            string[] scalars = s.Split(',');
            if (float.TryParse(scalars[0], style, provider, out float scalar0) &&
                float.TryParse(scalars[1], style, provider, out float scalar1) &&
                float.TryParse(scalars[2], style, provider, out float scalar2)
            )
            {
                result = new Vector3(scalar0, scalar1, scalar2);
                return true;
            }

            result = Vector3.zero;
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



        public static TEnum EnumParse<TEnum>(string value, bool ignoreCase) where TEnum : struct
        {
            if (Enum.TryParse<TEnum>(value, ignoreCase, out TEnum result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(TEnum).Name));
        }
        //sbyte short long    byte ushort uint ulong    double    Decimal     char
        public static bool BoolParse(string value)
        {
            if (bool.TryParse(value, out bool result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(bool).Name));
        }
        public static int IntParse(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(int).Name));
        }
        public static float FloatParse(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
            else
                throw new FormatException(string.Format(parseError, value, typeof(float).Name));
        }





        #endregion

        #region FUZZY SEARCH
        public static List<string> GetSimilarWords(string input, bool ignoreCase, int maxDistance, bool sort, params IEnumerator[] enumerators)
        {
            List<string> words = new List<string>();
            List<string>[] unsortedWords = null;
            if (sort)
            {
                unsortedWords = new List<string>[maxDistance + 1];
                for (int i = 0; i < unsortedWords.Length; i++)
                    unsortedWords[i] = new List<string>();
            }

            for (int i = 0; i < enumerators.Length; i++)
            {
                while (enumerators[i].MoveNext())
                {
                    string word = (string)enumerators[i].Current;
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
        public static List<Token> Tokenize(string input, Token[] operators)
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
                            tokenStartPosition = i;
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


            string error = "lexer: missing `{0}` at the end of the string";
            if (esc)
                throw new Exception(string.Format(error, "\\"));
            else if (sQ)
                throw new Exception(string.Format(error, "'"));
            else if (dQ)
                throw new Exception(string.Format(error, "\""));


            if (input.Length != tokenStartPosition)
            {
                id = operatorIndex == -1 ? Token.Type.WORD : operators[operatorIndex].type;
                value = input.Substring(tokenStartPosition, input.Length - tokenStartPosition);
                tokens.Add(new Token(id, value));
            }


            return tokens;
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
        public static string[] Split(string input, params char[] separators)
        {
            List<string> words = new List<string>();
            bool dq = false; //isDoubleQuoting ('"')
            bool sq = false; //isSingleQuoting ('\'')
            bool esc = false; //isEscaping ('\\')
            int startPos = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (sq)
                {
                    if (input[i] == '\'')
                        sq = false;
                }
                else if (dq)
                {
                    if (input[i] == '"')
                    {
                        if (!esc)
                            dq = false;
                        esc = false;
                    }
                    else if (input[i] == '\\')
                    {
                        if (esc)
                            esc = false;
                        else if (input[i + 1] == '"' || input[i + 1] == '\\')
                            esc = true;
                    }
                }
                else
                {
                    if (input[i] == '\'')
                    {
                        if (!esc)
                            sq = true;
                        esc = false;
                    }
                    else if (input[i] == '"')
                    {
                        if (!esc)
                            dq = true;
                        esc = false;
                    }
                    else if (input[i] == '\\')
                        esc = !esc;
                    else if (Array.IndexOf(separators, input[i]) > -1)
                    {
                        if (!esc)
                        {
                            if (i != startPos)
                                words.Add(input.Substring(startPos, i - startPos));
                            startPos = i + 1;
                        }
                        esc = false;
                    }
                    else
                        esc = false;
                }
            }
            if (input.Length != startPos)
                words.Add(input.Substring(startPos, input.Length - startPos));

            return words.ToArray();
        }
        #endregion

        #region MISC
        public static string GetStartSpace(string str)
        {
            int spaceCount = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ' ')
                    spaceCount++;
                else
                    break;
            }

            return new string(' ', spaceCount);
        }
        public static string GetEndSpace(string str)
        {
            int spaceCount = 0;
            for (int i = str.Length - 1; i >= 0; i--)
            {
                if (str[i] == ' ')
                    spaceCount++;
                else
                    break;
            }

            return new string(' ', spaceCount);
        }

        public static string GetFirstWord(string input, params char[] separators)
        {
            bool dq = false; //isDoubleQuoting ('"')
            bool sq = false; //isSingleQuoting ('\'')
            bool esc = false; //isEscaping ('\\')
            int startPos = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (sq)
                {
                    if (input[i] == '\'')
                        sq = false;
                }
                else if (dq)
                {
                    if (input[i] == '"')
                    {
                        if (!esc)
                            dq = false;
                        esc = false;
                    }
                    else if (input[i] == '\\')
                    {
                        if (esc)
                            esc = false;
                        else if (input[i + 1] == '"' || input[i + 1] == '\\')
                            esc = true;
                    }
                }
                else
                {
                    if (input[i] == '\'')
                    {
                        if (!esc)
                            sq = true;
                        esc = false;
                    }
                    else if (input[i] == '"')
                    {
                        if (!esc)
                            dq = true;
                        esc = false;
                    }
                    else if (input[i] == '\\')
                        esc = !esc;
                    else if (Array.IndexOf(separators, input[i]) > -1)
                    {
                        if (!esc)
                        {
                            if (i != startPos)
                                return input.Substring(startPos, i - startPos);
                            startPos = i + 1;
                        }
                        esc = false;
                    }
                    else
                        esc = false;
                }
            }
            if (input.Length != startPos)
                return input.Substring(startPos, input.Length - startPos);

            return "";
        }
        public static string GetArgs(string value, params char[] separators)
        {
            return value.Trim().Remove(0, GetFirstWord(value, separators).Length).Trim();
        }
        #endregion
    }
}
