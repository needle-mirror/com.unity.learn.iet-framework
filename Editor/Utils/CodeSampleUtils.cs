using System.Linq;
using System.Text;

namespace Unity.Tutorials.Core.Editor
{
    internal static class CodeSampleUtils
    {
        /// <summary>
        /// Returns the string formatted as code (indented)
        /// </summary>
        /// <param name="self"></param>
        /// <returns>The given string as tabulated formated code</returns>
        public static string AsFormattedCode(string originalString)
        {
            var lines = originalString.Split('\n').Select(s => s.Trim());

            var strBuilder = new StringBuilder();

            int indentCount = 0;
            bool shouldIndent = false;

            foreach (string line in lines)
            {
                if (shouldIndent)
                    indentCount++;

                if (line.Trim() == "}")
                    indentCount--;

                if (indentCount == 0)
                {
                    strBuilder.AppendLine(line);
                    shouldIndent = line.Contains("{");

                    continue;
                }

                string blankSpace = string.Empty;
                for (int i = 0; i < indentCount; i++)
                {
                    blankSpace += "    ";
                }

                if (line.Contains("}") && line.Trim() != "}")
                    indentCount--;

                strBuilder.AppendLine(blankSpace + line);
                shouldIndent = line.Contains("{");
            }

            return strBuilder.ToString().Trim();
        }


        /// <summary>
        /// Add rich text tag to color the given code. This only look for a subset of keywords, comments and function
        /// </summary>
        /// <param name="code"></param>
        /// <returns>A string containing the code with added rich text tag with color</returns>
        public static string HighlightCode(string code)
        {
            var strBuilder = new StringBuilder();
            var currentToken = new StringBuilder();

            //as we use rich text tag, we cannot use USS class, so we have to fix them in code.
            const string keywordColor = "#6C95EB";
            const string funcColor = "#39CC8F";
            const string stringColor = "#C9A26D";
            const string commentColor = "#85C46C";

            var keyword = new string[]
            {
                "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "double", "float", "decimal", "char", "var",
                "string", "char", "void", "object", "typeof", "sizeof", "null", "true", "false", "if", "else", "while", "for", "foreach", "do", "switch",
                "case", "default", "lock", "try", "throw", "catch", "finally", "goto", "break", "continue", "return", "public", "private", "internal",
                "protected", "static", "readonly", "sealed", "const", "fixed", "stackalloc", "volatile", "new", "override", "abstract", "virtual",
                "event", "extern", "ref", "out", "in", "is", "as", "params", "__arglist", "__makeref", "__reftype", "__refvalue", "this", "base",
                "namespace", "using", "class", "struct", "interface", "enum", "delegate", "checked", "unchecked", "unsafe", "operator", "implicit", "explicit"
            };

            var separator = new char[] { '(', ')', '{', '}', '[', ']', '.', ',', ';'};

            char previousCharacter = ' ';
            bool singleLineCommentOpened = false;
            bool multiLineCommentOpened = false;

            foreach (var c in code)
            {
                if (multiLineCommentOpened)
                {
                    //for multiline comments, we only care about the comment block closing, so we just append the character
                    //to the token until we encounter the closing character
                    currentToken.Append(c);
                    if (c == '/' && previousCharacter == '*')
                    {
                        multiLineCommentOpened = false;
                        currentToken.Insert(0, $"<i><color={commentColor}>");
                        currentToken.Append("</color></i>");
                        strBuilder.Append(currentToken);
                        currentToken.Clear();
                    }
                }
                else
                {

                    if (c == '*' && previousCharacter == '/')
                    {
                        //opening a block comment, we ignore everything else from now on
                        multiLineCommentOpened = true;
                        currentToken.Append(c);
                    }
                    //the character is a white space (space, line return etc.)
                    else if (char.IsWhiteSpace(c) || separator.Any(c1 => c1 == c))
                    {
                        //if we have a single comment type opened, this may mean we need to close it
                        if (singleLineCommentOpened)
                        {
                            //we finished a line, so we do close the comment and color the current token (which will be the full line)
                            if (c == '\r' || c == '\n')
                            {
                                currentToken.Insert(0, $"<i><color={commentColor}>");
                                currentToken.Append("</color></i>");
                                singleLineCommentOpened = false;

                                strBuilder.Append(currentToken);
                                strBuilder.Append(c);
                                currentToken.Clear();
                            }
                            else
                            {
                                //we add the character to the token, as for a single line comment, everything will be colored
                                currentToken.Append(c);
                            }
                        }
                        else
                        {
                            var token = currentToken.ToString();
                            if (keyword.Any(s => token == s))
                            {
                                currentToken.Insert(0, $"<color={keywordColor}>");
                                currentToken.Append("</color>");
                            }
                            else if (c == '(')
                            {
                                currentToken.Insert(0, $"<color={funcColor}>");
                                currentToken.Append("</color>");
                            }
                            else if (token.EndsWith('"'))
                            {
                                currentToken.Insert(0, $"<color={stringColor}>");
                                currentToken.Append("</color>");
                            }

                            strBuilder.Append(currentToken);
                            strBuilder.Append(c);
                            currentToken.Clear();
                        }
                    }
                    else if (c == '/' && previousCharacter == '/')
                    {
                        //we are opening a line comment, ignore until we encounter a end of line
                        currentToken.Append(c);
                        singleLineCommentOpened = true;
                    }
                    else
                    {
                        currentToken.Append(c);
                    }
                }

                previousCharacter = c;
            }

            return strBuilder.ToString();
        }
    }
}
