using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UShell
{
    public static class Tests
    {
        //[MenuItem("Tests/UShell %T")]
        private static void test()
        {
            if (!Application.isPlaying)
                return;

            testCompletion();
            testFuzzySearch();
            testHistory();
            testFont();
            testConsoleRegistration();
            testCommandRegistration();
            testParsing();

            testLevenshteinDistance();
        }


        private static void testCompletion()
        {
            int count = 2;
            int validCount = 0;
            validCount += assertCompletion("hel", "help ", 0) ? 1 : 0;
            validCount += assertCompletion("dfghgf", "dfghgf", 0) ? 1 : 0;

            string message = validCount + "/" + count;
            if (validCount == count) Debug.LogAssertion(message);
            else Debug.LogError(message);
        }
        private static bool assertCompletion(string prefix, string expected, int optionsCountExpected)
        {
            string result = Shell.Main.GetCompletion(prefix, out List<string> options);

            bool assertValue = result == expected && options != null && options.Count == optionsCountExpected;

            string message = assertValue + ": `" + prefix + "' -> `" + expected + "'";
            if (assertValue) Debug.LogAssertion(message);
            else Debug.LogError(message);

            return assertValue;
        }

        private static void testFuzzySearch()
        {
        }
        private static void testHistory()
        {
        }
        private static void testFont()
        {
        }
        private static void testConsoleRegistration()
        {
        }
        private static void testCommandRegistration()
        {
        }

        private static void testLevenshteinDistance()
        {
            //https://planetcalc.com/1721/

            assertLevenshteinDistance("book", "BOOK", false, 4);
            assertLevenshteinDistance("ab", "ac", false, 1);
            assertLevenshteinDistance("book", "back", false, 2);
            assertLevenshteinDistance("kitten", "sitting", false, 3);
            assertLevenshteinDistance("saturday", "sunday", false, 3);
            assertLevenshteinDistance("cats", "fast", false, 3);
            assertLevenshteinDistance("elephant", "relevant", false, 3);
            assertLevenshteinDistance("intention", "execution", false, 5);
            assertLevenshteinDistance("levenshtein", "einstein", false, 4);
            assertLevenshteinDistance("saka", "aoba", false, 3);
            assertLevenshteinDistance("monkey", "money", false, 1);
            assertLevenshteinDistance("dccb", "abc", false, 3);
            assertLevenshteinDistance("me", "my", false, 1);
            assertLevenshteinDistance("mesh", "tests", false, 3);
            assertLevenshteinDistance("catgactg", "tactg", false, 3);
            assertLevenshteinDistance("dafac", "fdbbec", false, 4);

            assertLevenshteinDistance("book", "BOOK", true, 0);
        }
        private static bool assertLevenshteinDistance(string word0, string word1, bool ignoreCase, int expectedDistance)
        {
            int distance = Utils.GetLevenshteinDistance(word0, word1, ignoreCase);

            bool assertValue = distance == expectedDistance;

            string message = string.Format("{0}: (`{1}', `{2}', {3}, {4}) -> ({5})", assertValue, word0, word1, ignoreCase, expectedDistance, distance);
            if (assertValue) Debug.LogAssertion(message);
            else Debug.LogError(message);

            return assertValue;
        }

        private static void testParsing()
        {

        }
        private static void assertParsing(string input, Token[] operators, List<Token> expectedOutput)
        {
            input = Utils.RemoveEscapedSeparators(input, new char[] { '\n', '\r' });
            List<Token> tokens = Utils.Tokenize(input, operators);

            bool success = true;
            if (tokens.Count == expectedOutput.Count)
            {
                for (int i = 0; i < tokens.Count; i++)
                {
                    if (!tokens[i].value.Equals(expectedOutput[i].value))
                    {
                        success = false;
                        break;
                    }
                }
            }
            else
                success = false;
            
            if (success) Debug.LogAssertion("success");
            else Debug.LogError("error");
        }
    }
}
