namespace MeetingAgent.Domain.Transcripts;

public sealed record TranscriptSegment(
    Guid Id,
    string SpeakerName,
    TimeSpan Start,
    TimeSpan End,
    string Text,
    string CleanText)
{
    public static TranscriptSegment Create(string? speakerName, TimeSpan start, TimeSpan end, string text, string? cleanText = null)
    {
        return new TranscriptSegment(
            Guid.NewGuid(),
            string.IsNullOrWhiteSpace(speakerName) ? "Participante" : speakerName.Trim(),
            start,
            end,
            text.Trim(),
            string.IsNullOrWhiteSpace(cleanText) ? text.Trim() : cleanText.Trim());
    }
}
