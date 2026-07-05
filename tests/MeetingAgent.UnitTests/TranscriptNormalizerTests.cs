using MeetingAgent.Application.Services;
using Xunit;
namespace MeetingAgent.UnitTests;

public sealed class TranscriptNormalizerTests
{
    [Fact]
    public void Normalize_plain_text_extracts_speaker_and_clean_text()
    {
        var normalizer = new TranscriptNormalizer(new TextNoiseCleaner());

        var segments = normalizer.Normalize("João: ééé vamos decidir o MVP.", "text");

        var segment = Assert.Single(segments);
        Assert.Equal("João", segment.SpeakerName);
        Assert.DoesNotContain("ééé", segment.CleanText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Normalize_vtt_extracts_segments()
    {
        var normalizer = new TranscriptNormalizer(new TextNoiseCleaner());
        var vtt = """
        WEBVTT

        00:00:01.000 --> 00:00:04.000
        <v Maria>Decidimos usar o Graph primeiro.
        """;

        var segments = normalizer.Normalize(vtt, "vtt");

        var segment = Assert.Single(segments);
        Assert.Equal("Maria", segment.SpeakerName);
        Assert.Contains("Graph", segment.CleanText);
    }
}
