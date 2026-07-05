# Arquitetura

## Camadas

```txt
Api → Application → Domain
Worker → Application → Domain
Infrastructure → Application Ports
```

## Fluxo de importação manual

```txt
POST /meetings/import
  ↓
ImportMeetingUseCase
  ↓
Meeting + Transcript
  ↓
TranscriptNormalizer
  ↓
HeuristicSummaryBuilder
  ↓
MarkdownSummaryRenderer
  ↓
MeetingSummary
```

## Fluxo Graph futuro

```txt
Graph notification
  ↓
POST /webhooks/graph
  ↓
Persist event
  ↓
Publish job
  ↓
Worker downloads transcript
  ↓
Workflow processes summary
  ↓
Publish to Teams
```

## Escalabilidade

- API escala separada do Worker.
- Processamento pesado fica fora do webhook.
- Jobs devem ser idempotentes.
- Repositórios em memória são apenas para desenvolvimento.
- Produção deve usar PostgreSQL + fila real.
