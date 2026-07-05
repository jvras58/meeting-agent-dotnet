namespace MeetingAgent.Infrastructure.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public string Provider { get; set; } = "heuristic";
    public string Model { get; set; } = "qwen3:8b";
    public string BaseUrl { get; set; } = "http://localhost:11434";
}
