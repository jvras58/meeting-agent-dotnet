using MeetingAgent.Application.Services;
using MeetingAgent.Domain.Transcripts;
using Xunit;

namespace MeetingAgent.UnitTests;

public sealed class HeuristicSummaryBuilderTests
{
    [Fact]
    public void Build_detects_decisions_actions_and_risks()
    {
        var builder = new HeuristicSummaryBuilder();
        var segments = new[]
        {
            TranscriptSegment.Create("Maria", TimeSpan.Zero, TimeSpan.FromSeconds(3), "Decidimos começar pela transcrição oficial do Teams."),
            TranscriptSegment.Create("Pedro", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), "Pedro vai fazer o webhook do Graph."),
            TranscriptSegment.Create("João", TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(9), "Existe risco de política do tenant bloquear transcrição.")
        };

        var draft = builder.Build(segments);

        Assert.NotEmpty(draft.Decisions);
        Assert.NotEmpty(draft.ActionItems);
        Assert.NotEmpty(draft.Risks);
    }
}
