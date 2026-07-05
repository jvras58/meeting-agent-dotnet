# Meeting Agent

Agente de IA para processar reuniões do Microsoft Teams usando **transcrições oficiais do Teams via Microsoft Graph**, com backend em **.NET**, pipeline de limpeza de transcript, extração de decisões, tarefas e geração de ata.

> Decisão do MVP: não capturar áudio bruto em tempo real. O primeiro caminho usa transcrição oficial do Teams, porque reduz complexidade, respeita políticas do Microsoft 365 e evita depender de bots de mídia em tempo real.

---

## Visão geral

```txt
Microsoft Teams
   ↓
Transcrição oficial / gravação opcional
   ↓
Microsoft Graph API
   ↓
MeetingAgent.Api
   ↓
MeetingAgent.Application
   ↓
Workflow de limpeza + resumo com IA/fallback heurístico
   ↓
MeetingAgent.Worker / Storage / Banco / Teams
```

O projeto segue uma organização inspirada em Clean Architecture/DDD:

```txt
Domain          → entidades, value objects e regras centrais
Application     → casos de uso, ports, workflows e agentes
Infrastructure  → Graph, storage, IA, filas e persistência
Api             → endpoints HTTP, webhooks e autenticação
Worker          → processamento assíncrono
Contracts       → eventos, requests e responses compartilhados
```

---

## Stack

- .NET 10
- ASP.NET Core Minimal APIs
- Worker Service
- Microsoft Graph via client credentials
- Ollama/OpenAI-compatible como adapter inicial de IA
- Fallback heurístico em C# para desenvolvimento local
- PostgreSQL para persistência futura
- Redis para cache/locks futuros
- RabbitMQ para fila futura
- Blob Storage/MinIO/Azure Blob para artefatos futuros
- OpenTelemetry/Aspire para observabilidade futura
- Docker Compose para ambiente local
- Dev Container opcional, sem dependência obrigatória de VS Code

---

## Estado atual da IA

O projeto agora possui um caminho real para IA no workflow:

```txt
Transcript normalizado
   ↓
AiMeetingSummaryBuilder
   ↓
IA via IAiChatService
   ↓
OllamaChatService, se AI_PROVIDER=ollama
   ↓
Fallback heurístico, se a IA falhar
```

Por padrão, o projeto usa:

```env
AI_PROVIDER=heuristic
```

Nesse modo, o processamento é rápido porque não chama modelo nenhum.

Para usar IA real com Ollama:

```env
AI_PROVIDER=ollama
AI_MODEL=qwen3:8b
AI_BASE_URL=http://ollama:11434
```

> Dentro do container, use `http://ollama:11434`. Rodando direto no host, use `http://localhost:11434`.

Baixe o modelo:

```bash
make ollama-model
```

---

## Pré-requisitos

### Fluxo recomendado via container

- Docker
- Docker Compose
- Make

Nesse modo, você **não precisa instalar o .NET SDK na máquina local**, porque o SDK roda dentro do container `dotnet_dev`.

### Fluxo alternativo direto no host

- .NET SDK 10
- Docker e Docker Compose para serviços auxiliares
- Make opcional

---

## Configuração inicial

```bash
cp .env.example .env
```

No PowerShell:

```powershell
Copy-Item .env.example .env
```

---

## Rodando com container de desenvolvimento

Suba o ambiente:

```bash
make up
```

Entre no container:

```bash
make shell
```

Dentro do container:

```bash
make restore
make build
make test
```

Rode a API:

```bash
make api
```

Ou com reload:

```bash
make api-watch
```

Em outro terminal, rode o Worker:

```bash
make shell
make worker
```

Ou com reload:

```bash
make shell
make worker-watch
```

---

## Rodando direto no host

Use apenas se você tiver o .NET SDK instalado localmente.

```bash
docker compose -f compose.dev.yml up -d postgres redis rabbitmq ollama
dotnet restore
dotnet build
dotnet test
dotnet run --project src/MeetingAgent.Api
```

---

## Serviços locais

| Serviço | URL |
|---|---|
| API | http://localhost:5080 |
| Swagger/OpenAPI | http://localhost:5080/swagger |
| RabbitMQ Management | http://localhost:15672 |
| Ollama | http://localhost:11434 |
| Aspire Dashboard | http://localhost:18888 |

---

## Comandos úteis

```bash
make up              # sobe ambiente
make down            # derruba ambiente
make restart         # reinicia ambiente
make shell           # entra no dotnet_dev
make ps              # lista containers
make logs            # logs dos serviços docker
make restore         # dotnet restore
make build           # dotnet build
make test            # dotnet test
make api             # roda API
make api-watch       # roda API com dotnet watch
make worker          # roda Worker
make worker-watch    # roda Worker com dotnet watch
make ollama-model    # baixa qwen3:8b no Ollama
make ollama-list     # lista modelos do Ollama
```

---

## Logs

A API e o Worker normalmente são iniciados manualmente com `dotnet run` ou `dotnet watch` dentro do container.

Por isso, os logs aparecem no terminal onde você iniciou o processo:

```bash
make api-watch
```

Ao importar uma reunião, você deve ver logs como:

```txt
Import meeting request received
Importing meeting
Starting meeting summary workflow
Transcript normalized
Calling AI summary service
Calling Ollama
Ollama response received
Meeting summary workflow finished
```

Logs dos serviços Docker:

```bash
make logs
make logs-ollama
make logs-rabbitmq
make logs-postgres
```

---

## Host vs container

Existem dois contextos:

```txt
Host / máquina local
   Onde Docker está instalado.

Container dotnet_dev
   Onde o .NET SDK está instalado.
```

Se você estiver dentro do container e receber:

```txt
zsh: command not found: docker
```

ou:

```txt
make: docker: Permission denied
```

significa que você tentou chamar Docker dentro do container.

Use:

```bash
dotnet build
```

ou o Makefile atualizado:

```bash
make build
```

---

## Teste rápido sem Microsoft Graph

A API possui um endpoint de importação manual para testar o pipeline antes de configurar Teams/Graph.

Com a API rodando:

```bash
curl -X POST http://localhost:5080/meetings/import \
  -H "Content-Type: application/json" \
  -d @samples/sample-import-request.json
```

Depois consulte:

```bash
curl http://localhost:5080/meetings
```

E o resumo:

```bash
curl http://localhost:5080/meetings/{meetingId}/summary
```

No PowerShell:

```powershell
Invoke-RestMethod -Method POST `
  -Uri "http://localhost:5080/meetings/import" `
  -ContentType "application/json" `
  -InFile ".\samples\sample-import-request.json"
```

---

## Configuração do Teams para extrair o máximo do projeto

### Regra do MVP

A reunião precisa ter **transcrição oficial do Teams ativada**.

Gravação completa é opcional, mas recomendada quando você quiser reprocessar áudio no futuro.

```txt
Reunião sem transcrição → Graph-only não consegue gerar ata.
Reunião com transcrição → Meeting Agent consegue processar.
Reunião com gravação + transcrição → melhor cenário para evolução futura.
```

### Configurações recomendadas no Teams Admin Center

```txt
Teams Admin Center
   ↓
Meetings
   ↓
Meeting policies
   ↓
Recording & transcription
```

Recomendações:

```txt
Cloud recording: On
Transcription: On
Recording storage: OneDrive/SharePoint
Who can record/transcribe: Organizer and co-organizers, ou política equivalente
```

### App Registration no Microsoft Entra ID

```txt
Microsoft Entra Admin Center
   ↓
App registrations
   ↓
New registration
   ↓
Name: Meeting Agent
   ↓
Supported account types: Single tenant
   ↓
Register
```

Depois crie um client secret e preencha:

```env
AZURE_TENANT_ID=
AZURE_CLIENT_ID=
AZURE_CLIENT_SECRET=
GRAPH_BASE_URL=https://graph.microsoft.com/v1.0
```

Permissões iniciais para ambiente controlado/MVP:

```txt
OnlineMeetingTranscript.Read.All
OnlineMeetingRecording.Read.All
OnlineMeetings.Read.All
Calendars.Read
User.Read.All
```

Essas permissões exigem consentimento administrativo. Para produto real, avalie Resource-Specific Consent para reduzir escopo.

---

## Endpoints principais

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/health` | Healthcheck simples |
| GET | `/ready` | Readiness check |
| POST | `/webhooks/graph` | Webhook Microsoft Graph |
| POST | `/meetings/import` | Importação manual de transcript |
| GET | `/meetings` | Lista reuniões |
| GET | `/meetings/{id}` | Detalha reunião |
| GET | `/meetings/{id}/summary` | Busca ata/resumo |
| POST | `/meetings/{id}/process` | Reprocessa reunião |

---

## Arquitetura de pastas

```txt
meeting-agent/
├── src/
│   ├── MeetingAgent.Api/
│   ├── MeetingAgent.Worker/
│   ├── MeetingAgent.Application/
│   ├── MeetingAgent.Domain/
│   ├── MeetingAgent.Infrastructure/
│   └── MeetingAgent.Contracts/
│
├── tests/
├── docs/
├── samples/
├── scripts/
├── deploy/
├── .devcontainer/
├── compose.dev.yml
├── docker-compose.yml
├── .env.example
├── Makefile
└── README.md
```

---

## Estratégia de escalabilidade

- `MeetingAgent.Api`: recebe webhooks, expõe endpoints e autentica chamadas.
- `MeetingAgent.Worker`: processa tarefas pesadas fora da request HTTP.
- `MeetingAgent.Application`: concentra regras de negócio e workflow.
- `MeetingAgent.Infrastructure`: integra com Graph, IA, storage e banco.
- `MeetingAgent.Domain`: mantém modelo central independente de frameworks.

Nada pesado deve rodar dentro do webhook:

```txt
Webhook recebe evento
   ↓
Persiste evento
   ↓
Publica job
   ↓
Worker processa
   ↓
Atualiza status
```

---

## Roadmap

### Fase 1 — MVP local

- API + Worker
- Importação manual de transcript
- Parser VTT/texto
- Limpeza heurística
- Resumo por IA local/Ollama com fallback heurístico
- Ata em JSON/Markdown

### Fase 2 — Microsoft Graph

- App Registration
- Client credentials
- Download de transcript
- Webhook de transcript disponível
- Persistência real

### Fase 3 — Teams App

- Adaptive Cards
- Publicação da ata no chat
- Bot informativo
- Consentimento explícito

### Fase 4 — IA avançada

- Microsoft Agent Framework
- Workflow multiagente
- Revisão anti-alucinação
- RAG com reuniões anteriores

### Fase 5 — Áudio avançado

- Baixar gravação
- Reprocessar com WhisperX/faster-whisper
- Diarização avançada
- Comparação com transcript oficial

---

## Documentação complementar

- `docs/development.md`: ambiente de desenvolvimento em detalhes.
- `docs/ai-observability.md`: IA real, fallback heurístico, logs e watch.
- `docs/stack.md`: decisões de stack.
- `docs/architecture.md`: arquitetura técnica.
- `docs/teams-setup.md`: configuração do Microsoft Teams.
- `docs/graph-permissions.md`: permissões Microsoft Graph.
- `docs/adr/`: registros de decisão arquitetural.

---

## Decisão final

```txt
Teams gera o artefato oficial.
.NET orquestra.
Worker processa.
Agentes analisam.
Storage persiste.
API expõe.
Teams recebe a ata.
```

O projeto deve evoluir primeiro em cima de transcrições oficiais do Teams via Microsoft Graph.

Captura de áudio bruto em tempo real deve ser considerada apenas em fases futuras, se houver necessidade real e autorização explícita.
