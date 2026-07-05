using MeetingAgent.Application.Ports;
using MeetingAgent.Domain.Summaries;

namespace MeetingAgent.Application.UseCases;

public sealed class GetMeetingSummaryUseCase
{
    private readonly ISummaryRepository _summaryRepository;

    public GetMeetingSummaryUseCase(ISummaryRepository summaryRepository)
    {
        _summaryRepository = summaryRepository;
    }

    public Task<MeetingSummary?> ExecuteAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        return _summaryRepository.GetByMeetingIdAsync(meetingId, cancellationToken);
    }
}
