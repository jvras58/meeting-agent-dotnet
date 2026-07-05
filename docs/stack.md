# Stack do Meeting Agent

## Decisão principal

O projeto começa usando **transcrições oficiais do Teams via Microsoft Graph**. A gravação completa é opcional no MVP.

```txt
Teams Transcript → Graph API → .NET API/Worker → Workflow de IA → Ata final
```

## Backend

- .NET 10
- ASP.NET Core
- Worker Service
- Clean Architecture
- Ports & Adapters
- Microsoft Graph via client credentials
- Microsoft Agent Framework como evolução planejada
- Microsoft.Extensions.AI como abstração planejada

## IA

No MVP, existem dois modos:

- `AI_PROVIDER=heuristic`: resumo local simples para desenvolvimento.
- `AI_PROVIDER=ollama`: adapter para Ollama/OpenAI-compatible.

Para produção:

- Microsoft Agent Framework para workflow multiagente;
- modelo local para resumo, como Qwen/Llama/Mistral;
- WhisperX/faster-whisper somente se for reprocessar áudio.

## Infra

- PostgreSQL
- Redis
- RabbitMQ
- Blob Storage
- OpenTelemetry
- Aspire Dashboard

## Segurança

- nunca gravar reunião sem autorização;
- respeitar políticas de Teams/Microsoft 365;
- usar consentimento administrativo;
- auditar acesso a atas/transcrições;
- criptografar artefatos sensíveis;
- aplicar política de retenção.
