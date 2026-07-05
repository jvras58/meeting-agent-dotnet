using System.Text;
using MeetingAgent.Domain.Meetings;
using MeetingAgent.Domain.Summaries;

namespace MeetingAgent.Application.Services;

public sealed class MarkdownSummaryRenderer
{
    public string Render(Meeting meeting, MeetingSummaryDraft draft)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Ata da reunião: {meeting.Title}");
        sb.AppendLine();
        if (meeting.StartTime is not null) sb.AppendLine($"**Data:** {meeting.StartTime:dd/MM/yyyy HH:mm}");
        if (!string.IsNullOrWhiteSpace(meeting.OrganizerEmail)) sb.AppendLine($"**Organizador:** {meeting.OrganizerEmail}");
        sb.AppendLine();

        sb.AppendLine("## Resumo executivo");
        sb.AppendLine();
        sb.AppendLine(draft.ExecutiveSummary);
        sb.AppendLine();

        sb.AppendLine("## Principais pontos");
        sb.AppendLine();
        foreach (var point in draft.MainPoints.DefaultIfEmpty("Nenhum ponto principal identificado.")) sb.AppendLine($"- {point}");
        sb.AppendLine();

        sb.AppendLine("## Decisões tomadas");
        sb.AppendLine();
        if (draft.Decisions.Count == 0) sb.AppendLine("- Nenhuma decisão explícita identificada.");
        foreach (var decision in draft.Decisions) sb.AppendLine($"- {decision.Text}" + (decision.Context is null ? string.Empty : $" — {decision.Context}"));
        sb.AppendLine();

        sb.AppendLine("## Tarefas");
        sb.AppendLine();
        sb.AppendLine("| Tarefa | Responsável | Prazo | Status |");
        sb.AppendLine("|---|---|---|---|");
        if (draft.ActionItems.Count == 0) sb.AppendLine("| Nenhuma tarefa identificada | - | - | - |");
        foreach (var item in draft.ActionItems)
        {
            sb.AppendLine($"| {item.Task} | {item.Owner ?? "-"} | {item.DueDate?.ToString("dd/MM/yyyy") ?? "-"} | {item.Status} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Riscos");
        sb.AppendLine();
        if (draft.Risks.Count == 0) sb.AppendLine("- Nenhum risco identificado.");
        foreach (var risk in draft.Risks) sb.AppendLine($"- {risk.Text}" + (risk.Severity is null ? string.Empty : $" ({risk.Severity})"));
        sb.AppendLine();

        sb.AppendLine("## Dúvidas em aberto");
        sb.AppendLine();
        if (draft.OpenQuestions.Count == 0) sb.AppendLine("- Nenhuma dúvida em aberto identificada.");
        foreach (var question in draft.OpenQuestions) sb.AppendLine($"- {question}");

        return sb.ToString().Trim();
    }
}
