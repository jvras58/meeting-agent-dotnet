using System.Globalization;
using System.Text.RegularExpressions;
using MeetingAgent.Domain.Transcripts;

namespace MeetingAgent.Application.Services;

public sealed partial class TranscriptNormalizer
{
    private readonly TextNoiseCleaner _cleaner;

    public TranscriptNormalizer(TextNoiseCleaner cleaner)
    {
        _cleaner = cleaner;
    }

    public IReadOnlyCollection<TranscriptSegment> Normalize(string rawTranscript, string? sourceFormat)
    {
        if (string.IsNullOrWhiteSpace(rawTranscript)) return [];

        return string.Equals(sourceFormat, "vtt", StringComparison.OrdinalIgnoreCase)
            ? ParseVtt(rawTranscript)
            : ParsePlainText(rawTranscript);
    }

    private IReadOnlyCollection<TranscriptSegment> ParsePlainText(string rawTranscript)
    {
        var segments = new List<TranscriptSegment>();
        var lines = rawTranscript.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var current = TimeSpan.Zero;

        foreach (var line in lines)
        {
            var (speaker, text) = SplitSpeaker(line);
            if (string.IsNullOrWhiteSpace(text)) continue;

            var end = current.Add(TimeSpan.FromSeconds(Math.Max(3, text.Length / 18)));
            segments.Add(TranscriptSegment.Create(speaker, current, end, text, _cleaner.Clean(text)));
            current = end;
        }

        return segments;
    }

    private IReadOnlyCollection<TranscriptSegment> ParseVtt(string rawTranscript)
    {
        var segments = new List<TranscriptSegment>();
        var lines = rawTranscript.Split(['\r', '\n'], StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!line.Contains("-->", StringComparison.Ordinal)) continue;

            var times = line.Split("-->", StringSplitOptions.TrimEntries);
            if (times.Length != 2) continue;

            var start = ParseTimestamp(times[0]);
            var end = ParseTimestamp(times[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]);
            var textLines = new List<string>();

            i++;
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                textLines.Add(lines[i].Trim());
                i++;
            }

            var text = string.Join(" ", textLines).Trim();
            text = VttVoiceRegex().Replace(text, match => $"{match.Groups[1].Value}: ");
            text = VttTagRegex().Replace(text, string.Empty);
            var (speaker, body) = SplitSpeaker(text);

            if (!string.IsNullOrWhiteSpace(body))
            {
                segments.Add(TranscriptSegment.Create(speaker, start, end, body, _cleaner.Clean(body)));
            }
        }

        return segments;
    }

    private static (string Speaker, string Text) SplitSpeaker(string line)
    {
        var match = SpeakerRegex().Match(line);
        if (!match.Success) return ("Participante", line.Trim());

        return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
    }

    private static TimeSpan ParseTimestamp(string value)
    {
        value = value.Trim().Replace(',', '.');
        var parts = value.Split(':');
        if (parts.Length == 3 && TimeSpan.TryParseExact(value, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        if (parts.Length == 2 && TimeSpan.TryParseExact(value, @"mm\:ss\.fff", CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return TimeSpan.Zero;
    }

    [GeneratedRegex("^([^:]{1,80}):\\s*(.+)$")]
    private static partial Regex SpeakerRegex();

    [GeneratedRegex("<v\\s+([^>]+)>", RegexOptions.IgnoreCase)]
    private static partial Regex VttVoiceRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex VttTagRegex();
}
