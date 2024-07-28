using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

namespace OpenAIImageDescription.Utils;

internal class JsonHelper
{
    public static bool IsTripleBackTickJson(string message, [NotNullWhen(true)] out string? json)
    {
        const string Prefix = "```json";
        const string Suffix = "```";

        json = null;

        if (message.StartsWith(Prefix))
        {
            int start = message.IndexOf(Prefix) + Prefix.Length;
            int end = message.IndexOf(Suffix, start);

            if (end > start)
            {
                json = message[start..end];

                return true;
            }
        }

        return false;
    }
}
