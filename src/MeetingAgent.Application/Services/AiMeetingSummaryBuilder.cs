using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeetingAgent.Application.Ports;
using MeetingAgent.Domain.Summaries;
using MeetingAgent.Domain.Transcripts;
using Microsoft.Extensions.Logging;

namespace MeetingAgent.Application.Services;

public sealed class AiMeetingSummaryBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAiChatService _aiChatService;
    private readonly HeuristicSummaryBuilder _fallbackSummaryBuilder;
    private readonly ILogger<AiMeetingSummaryBuilder> _logger;

    public AiMeetingSummaryBuilder(
        IAiChatService aiChatService,
        HeuristicSummaryBuilder fallbackSummaryBuilder,
        ILogger<AiMeetingSummaryBuilder> logger)
    {
        _aiChatService = aiChatService;
        _fallbackSummaryBuilder = fallbackSummaryBuilder;
        _logger = logger;
    }

    public async Task<MeetingSummaryDraft> BuildAsync(
        IReadOnlyCollection<TranscriptSegment> segments,
        CancellationToken cancellationToken = default)
    {
        var fallbackDraft = _fallbackSummaryBuilder.Build(segments);

        if (segments.Count == 0)
        {
            _logger.LogInformation("AI summary skipped because transcript has no segments.");
            return fallbackDraft;
        }

        var transcriptForPrompt = BuildTranscriptForPrompt(segments);
        if (string.IsNullOrWhiteSpace(transcriptForPrompt))
        {
            _logger.LogInformation("AI summary skipped because transcript text is empty after normalization.");
            return fallbackDraft;
        }

        try
        {
            _logger.LogInformation(
                "Calling AI summary service for {SegmentCount} transcript segment(s) and {TranscriptLength} prompt characters.",
                segments.Count,
                transcriptForPrompt.Length);

            var response = await _aiChatService.CompleteAsync(
                BuildSystemPrompt(),
                transcriptForPrompt,
                cancellationToken);

            if (TryParseAiResponse(response, fallbackDraft, out var aiDraft))
            {
                _logger.LogInformation(
                    "AI summary generated. MainPoints={MainPointsCount}, Decisions={DecisionsCount}, ActionItems={ActionItemsCount}, Risks={RisksCount}, OpenQuestions={OpenQuestionsCount}.",
                    aiDraft.MainPoints.Count,
                    aiDraft.Decisions.Count,
                    aiDraft.ActionItems.Count,
                    aiDraft.Risks.Count,
                    aiDraft.OpenQuestions.Count);

                return aiDraft;
            }

            _logger.LogWarning(
                "AI summary response could not be parsed as expected JSON. Falling back to heuristic summary. ResponseLength={ResponseLength}.",
                response.Length);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "AI summary service timed out or canceled internally. Falling back to heuristic summary.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "AI summary service failed. Falling back to heuristic summary.");
        }

        return fallbackDraft;
    }

    private static string BuildSystemPrompt()
    {
        return """
        Você é um agente especialista em atas de reunião.

        Sua tarefa é ler uma transcrição de reunião e retornar APENAS um JSON válido, sem markdown, sem comentários e sem texto fora do JSON.

        Regras:
        - Remova ruídos como risadas, cumprimentos soltos, vícios de fala e repetições.
        - Não invente decisões, tarefas, responsáveis ou prazos.
        - Se não houver evidência no texto, deixe arrays vazios.
        - Preserve detalhes técnicos importantes.
        - Use português do Brasil.

        Formato obrigatório:
        {
          "executive_summary": "Resumo executivo curto em até 2 parágrafos.",
          "main_points": ["Ponto principal 1"],
          "decisions": [
            {
              "text": "Decisão tomada",
              "context": "Contexto da decisão"
            }
          ],
          "action_items": [
            {
              "task": "Tarefa identificada",
              "owner": "Responsável, se explícito",
              "due_date": "YYYY-MM-DD ou null"
            }
          ],
          "risks": [
            {
              "text": "Risco identificado",
              "severity": "baixa, média, alta ou null"
            }
          ],
          "open_questions": ["Pergunta em aberto"]
        }
        """;
    }

    private static string BuildTranscriptForPrompt(IReadOnlyCollection<TranscriptSegment> segments)
    {
        var builder = new StringBuilder();

        foreach (var segment in segments.OrderBy(segment => segment.Start))
        {
            var text = string.IsNullOrWhiteSpace(segment.CleanText) ? segment.Text : segment.CleanText;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            builder.Append('[')
                .Append(segment.Start)
                .Append("] ")
                .Append(segment.SpeakerName)
                .Append(": ")
                .AppendLine(text.Trim());
        }

        return builder.ToString().Trim();
    }

    private static bool TryParseAiResponse(string response, MeetingSummaryDraft fallbackDraft, out MeetingSummaryDraft draft)
    {
        draft = fallbackDraft;

        var json = ExtractJsonObject(response);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        AiSummaryResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AiSummaryResponse>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (parsed is null)
        {
            return false;
        }

        var executiveSummary = FirstNonEmpty(parsed.ExecutiveSummary, fallbackDraft.ExecutiveSummary);
        var mainPoints = NormalizeStrings(parsed.MainPoints);
        var decisions = parsed.Decisions?
            .Where(decision => !string.IsNullOrWhiteSpace(decision.Text))
            .Select(decision => new Decision(decision.Text!.Trim(), NormalizeNullable(decision.Context)))
            .ToList() ?? [];

        var actionItems = parsed.ActionItems?
            .Where(actionItem => !string.IsNullOrWhiteSpace(actionItem.Task))
            .Select(actionItem => new ActionItem(
                actionItem.Task!.Trim(),
                NormalizeNullable(actionItem.Owner),
                ParseDateOnly(actionItem.DueDate)))
            .ToList() ?? [];

        var risks = parsed.Risks?
            .Where(risk => !string.IsNullOrWhiteSpace(risk.Text))
            .Select(risk => new Risk(risk.Text!.Trim(), NormalizeNullable(risk.Severity)))
            .ToList() ?? [];

        var openQuestions = NormalizeStrings(parsed.OpenQuestions);

        draft = new MeetingSummaryDraft(
            executiveSummary,
            mainPoints.Count == 0 ? fallbackDraft.MainPoints : mainPoints,
            decisions.Count == 0 ? fallbackDraft.Decisions : decisions,
            actionItems.Count == 0 ? fallbackDraft.ActionItems : actionItems,
            risks.Count == 0 ? fallbackDraft.Risks : risks,
            openQuestions.Count == 0 ? fallbackDraft.OpenQuestions : openQuestions);

        return true;
    }

    private static string? ExtractJsonObject(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');

        return start >= 0 && end > start
            ? response[start..(end + 1)]
            : null;
    }

    private static IReadOnlyCollection<string> NormalizeStrings(IEnumerable<string?>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList() ?? [];
    }

    private static string FirstNonEmpty(string? preferred, string fallback)
    {
        return string.IsNullOrWhiteSpace(preferred)
            ? fallback
            : preferred.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        return DateOnly.TryParse(value, out var date) ? date : null;
    }

    private sealed record AiSummaryResponse(
        [property: JsonPropertyName("executive_summary")] string? ExecutiveSummary,
        [property: JsonPropertyName("main_points")] IReadOnlyCollection<string?>? MainPoints,
        [property: JsonPropertyName("decisions")] IReadOnlyCollection<AiDecision>? Decisions,
        [property: JsonPropertyName("action_items")] IReadOnlyCollection<AiActionItem>? ActionItems,
        [property: JsonPropertyName("risks")] IReadOnlyCollection<AiRisk>? Risks,
        [property: JsonPropertyName("open_questions")] IReadOnlyCollection<string?>? OpenQuestions);

    private sealed record AiDecision(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("context")] string? Context);

    private sealed record AiActionItem(
        [property: JsonPropertyName("task")] string? Task,
        [property: JsonPropertyName("owner")] string? Owner,
        [property: JsonPropertyName("due_date")] string? DueDate);

    private sealed record AiRisk(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("severity")] string? Severity);
}
