using System.Text.RegularExpressions;

namespace MeetingAgent.Application.Services;

public sealed partial class TextNoiseCleaner
{
    private static readonly string[] FillerWords =
    [
        "ééé", "eee", "hã", "ahn", "aham", "né", "tipo assim", "basicamente", "risos", "kkkk", "kkk"
    ];

    public string Clean(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var output = input.Trim();
        foreach (var filler in FillerWords)
        {
            output = Regex.Replace(output, $@"\b{Regex.Escape(filler)}\b", string.Empty, RegexOptions.IgnoreCase);
        }

        output = MultipleSpacesRegex().Replace(output, " ");
        output = SpaceBeforePunctuationRegex().Replace(output, "$1");
        return output.Trim();
    }

    [GeneratedRegex("\\s{2,}")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex("\\s+([,.;:!?])")]
    private static partial Regex SpaceBeforePunctuationRegex();
}
