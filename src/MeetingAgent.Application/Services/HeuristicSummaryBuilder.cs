using System.Text.RegularExpressions;
using MeetingAgent.Domain.Summaries;
using MeetingAgent.Domain.Transcripts;

namespace MeetingAgent.Application.Services;

public sealed partial class HeuristicSummaryBuilder
{
    public MeetingSummaryDraft Build(IReadOnlyCollection<TranscriptSegment> segments)
    {
        var cleanLines = segments
            .Select(s => s.CleanText)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var allText = string.Join(" ", cleanLines);
        var mainPoints = cleanLines
            .Where(line => line.Length > 30)
            .Take(8)
            .Select(TrimSentence)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var decisions = cleanLines
            .Where(line => DecisionRegex().IsMatch(line))
            .Select(line => new Decision(TrimSentence(line), "Detectado por palavras-chave de decisão."))
            .Take(8)
            .ToList();

        var actionItems = cleanLines
            .Where(line => ActionRegex().IsMatch(line))
            .Select(line => new ActionItem(TrimSentence(line), TryExtractOwner(line), null))
            .Take(10)
            .ToList();

        var risks = cleanLines
            .Where(line => RiskRegex().IsMatch(line))
            .Select(line => new Risk(TrimSentence(line), "A revisar"))
            .Take(8)
            .ToList();

        var questions = cleanLines
            .Where(line => line.Contains('?'))
            .Select(TrimSentence)
            .Take(8)
            .ToList();

        var executiveSummary = allText.Length == 0
            ? "Não foi possível gerar resumo porque a transcrição está vazia."
            : $"A reunião abordou {Math.Min(mainPoints.Count, 8)} ponto(s) principal(is). Foram identificadas {decisions.Count} decisão(ões), {actionItems.Count} tarefa(s), {risks.Count} risco(s) e {questions.Count} dúvida(s) em aberto. Revise os itens abaixo antes de publicar a ata final.";

        return new MeetingSummaryDraft(executiveSummary, mainPoints, decisions, actionItems, risks, questions);
    }

    private static string TrimSentence(string line)
    {
        line = line.Trim();
        return line.Length <= 220 ? line : line[..220].TrimEnd() + "...";
    }

    private static string? TryExtractOwner(string line)
    {
        var match = OwnerRegex().Match(line);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    [GeneratedRegex("\\b(decidimos|decidido|decisão|ficou definido|vamos seguir|aprovado|aprovada)\\b", RegexOptions.IgnoreCase)]
    private static partial Regex DecisionRegex();

    [GeneratedRegex("\\b(vai fazer|precisa fazer|ficou com|responsável|tarefa|pendência|ação|action item|implementar|enviar|validar|corrigir)\\b", RegexOptions.IgnoreCase)]
    private static partial Regex ActionRegex();

    [GeneratedRegex("\\b(risco|bloqueio|problema|atenção|dependência|atraso|impedimento)\\b", RegexOptions.IgnoreCase)]
    private static partial Regex RiskRegex();

    [GeneratedRegex("(?:responsável|ficou com|dono)\\s+([A-Za-zÀ-ÿ ]{2,40})", RegexOptions.IgnoreCase)]
    private static partial Regex OwnerRegex();
}
