using System.Text;

public class CSharpCommentRemover
{
    public string Execute(string content)
    {
        var result = new StringBuilder(content.Length);
        bool inString = false;
        bool inVerbatimString = false;
        bool inSingleLineComment = false;
        bool inMultiLineComment = false;
        char stringDelimiter = '"';

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            char next = i + 1 < content.Length ? content[i + 1] : '\0';

            if (inSingleLineComment)
            {
                if (c == '\r' || c == '\n')
                {
                    inSingleLineComment = false;
                    result.Append(c);
                }
                continue;
            }

            if (inMultiLineComment)
            {
                if (c == '*' && next == '/')
                {
                    inMultiLineComment = false;
                    i++;
                }
                continue;
            }

            if (inString)
            {
                result.Append(c);
                if (c == '\\' && !inVerbatimString)
                {
                    if (next != '\0')
                    {
                        result.Append(next);
                        i++;
                    }
                }
                else if (c == stringDelimiter)
                {
                    inString = false;
                    inVerbatimString = false;
                }
                continue;
            }

            if (c == '"' || c == '\'')
            {
                inString = true;
                stringDelimiter = c;
                inVerbatimString = (c == '"' && i > 0 && content[i - 1] == '@');
                result.Append(c);
                continue;
            }

            if (c == '/' && next == '/')
            {
                inSingleLineComment = true;
                i++;
                continue;
            }

            if (c == '/' && next == '*')
            {
                inMultiLineComment = true;
                i++;
                continue;
            }

            result.Append(c);
        }

        return result.ToString();
    }
}