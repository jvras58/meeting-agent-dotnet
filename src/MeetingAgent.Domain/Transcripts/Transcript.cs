using MeetingAgent.Domain.Common;

namespace MeetingAgent.Domain.Transcripts;

public sealed class Transcript : Entity
{
    private readonly List<TranscriptSegment> _segments = [];

    private Transcript(Guid id, Guid meetingId, string source, string language, string rawContent) : base(id)
    {
        MeetingId = meetingId;
        Source = string.IsNullOrWhiteSpace(source) ? "manual" : source.Trim();
        Language = string.IsNullOrWhiteSpace(language) ? "pt-BR" : language.Trim();
        RawContent = rawContent;
        Status = TranscriptStatus.Imported;
    }

    public Guid MeetingId { get; }
    public string Source { get; }
    public string Language { get; }
    public string RawContent { get; }
    public TranscriptStatus Status { get; private set; }
    public IReadOnlyCollection<TranscriptSegment> Segments => _segments.AsReadOnly();

    public static Transcript Create(Guid meetingId, string source, string language, string rawContent)
    {
        if (meetingId == Guid.Empty) throw new ArgumentException("Meeting id is required.", nameof(meetingId));
        if (string.IsNullOrWhiteSpace(rawContent)) throw new ArgumentException("Raw content is required.", nameof(rawContent));
        return new Transcript(Guid.NewGuid(), meetingId, source, language, rawContent);
    }

    public static Transcript Restore(
        Guid id,
        Guid meetingId,
        string source,
        string language,
        string rawContent,
        TranscriptStatus status,
        IEnumerable<TranscriptSegment>? segments,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var transcript = new Transcript(id, meetingId, source, language, rawContent)
        {
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        transcript._segments.AddRange(segments ?? []);
        return transcript;
    }

    public void ReplaceSegments(IEnumerable<TranscriptSegment> segments)
    {
        _segments.Clear();
        _segments.AddRange(segments);
        Status = TranscriptStatus.Normalized;
        Touch();
    }
}
