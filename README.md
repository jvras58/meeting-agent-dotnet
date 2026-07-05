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
Workflow de limpeza + resumo
   ↓
MeetingAgent.Worker / Storage / Banco / Teams
```

O projeto foi organizado com uma arquitetura inspirada em Clean Architecture/DDD:

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

* .NET 10
* ASP.NET Core Minimal APIs
* Worker Service
* Microsoft Graph via client credentials
* Microsoft Agent Framework / Microsoft.Extensions.AI como direção arquitetural
* Ollama/OpenAI-compatible como adapter inicial de IA
* PostgreSQL para persistência futura
* Redis para cache/locks futuros
* RabbitMQ para fila futura
* Blob Storage/MinIO/Azure Blob para artefatos futuros
* OpenTelemetry/Aspire para observabilidade futura
* Docker Compose para ambiente local
* Dev Container opcional, sem dependência obrigatória de VS Code

A implementação atual já traz portas/interfaces para Graph, IA, storage, relógio, repositório e jobs.

O adapter inicial usa armazenamento em memória e IA heurística por padrão, para permitir desenvolvimento local sem depender de tenant Microsoft 365 logo no primeiro commit.

---

## Pré-requisitos

### Para rodar via container de desenvolvimento

Recomendado para desenvolvimento do projeto.

* Docker
* Docker Compose
* Make

Nesse modo, você **não precisa instalar o .NET SDK na sua máquina**, porque o SDK roda dentro do container `dotnet_dev`.

### Para rodar direto na máquina

Opcional.

* .NET SDK 10
* Docker e Docker Compose para serviços auxiliares
* Make, se quiser usar os atalhos

Verifique o SDK local:

```bash
dotnet --version
```

---

## Arquivos importantes de ambiente

```txt
.devcontainer/
├── Dockerfile
├── devcontainer.json
└── .zshrc

compose.dev.yml
docker-compose.yml
Makefile
.env.example
```

### Diferença entre `compose.dev.yml` e `docker-compose.yml`

O projeto pode ter dois arquivos de compose:

```txt
compose.dev.yml
```

Usado para ambiente de desenvolvimento completo, incluindo:

* container .NET `dotnet_dev`;
* PostgreSQL;
* Redis;
* RabbitMQ;
* Ollama.

```txt
docker-compose.yml
```

Pode ser usado apenas para infraestrutura ou para cenários mais próximos de execução local/produção, dependendo da evolução do projeto.

Durante o desenvolvimento, prefira:

```bash
make up
```

ou:

```bash
docker compose -f compose.dev.yml up -d
```

---

## Configuração inicial

### 1. Copiar variáveis de ambiente

```bash
cp .env.example .env
```

No Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

---

## Como iniciar com container de desenvolvimento

Este é o fluxo recomendado.

### 1. Subir ambiente

Execute no terminal da sua máquina local:

```bash
make up
```

Esse comando sobe os serviços de desenvolvimento:

* container .NET;
* PostgreSQL;
* Redis;
* RabbitMQ;
* Ollama.

Se preferir sem Makefile:

```bash
docker compose -f compose.dev.yml up -d
```

---

### 2. Entrar no container

```bash
make shell
```

Ou sem Makefile:

```bash
docker compose -f compose.dev.yml exec dotnet_dev zsh
```

Dentro do container, o projeto estará montado em:

```bash
/workspace
```

Você provavelmente verá algo parecido com:

```bash
/workspace via .NET v10.0.100
```

Isso significa que você já está dentro do container de desenvolvimento.

---

### 3. Restaurar pacotes

Dentro do container:

```bash
dotnet restore
```

Ou, se o Makefile estiver preparado para detectar container:

```bash
make restore
```

---

### 4. Build

Dentro do container:

```bash
dotnet build
```

Ou:

```bash
make build
```

---

### 5. Rodar testes

Dentro do container:

```bash
dotnet test
```

Ou:

```bash
make test
```

---

### 6. Rodar API

Dentro do container:

```bash
dotnet run --project src/MeetingAgent.Api
```

Ou:

```bash
make api
```

A API ficará disponível em:

```txt
http://localhost:5080
```

Swagger/OpenAPI:

```txt
http://localhost:5080/swagger
```

---

### 7. Rodar Worker

Em outro terminal, entre novamente no container:

```bash
make shell
```

Depois rode:

```bash
dotnet run --project src/MeetingAgent.Worker
```

Ou:

```bash
make worker
```

---

## Como iniciar rodando .NET direto na máquina

Use esse modo apenas se você tiver o .NET SDK instalado localmente.

### 1. Subir infraestrutura

```bash
docker compose up -d
```

Ou, se estiver usando o compose de desenvolvimento:

```bash
docker compose -f compose.dev.yml up -d postgres redis rabbitmq ollama
```

### 2. Restaurar pacotes

```bash
dotnet restore
```

### 3. Build

```bash
dotnet build
```

### 4. Rodar API

```bash
dotnet run --project src/MeetingAgent.Api
```

### 5. Rodar Worker

```bash
dotnet run --project src/MeetingAgent.Worker
```

### 6. Rodar testes

```bash
dotnet test
```

---

## Serviços locais

| Serviço             | URL                           |
| ------------------- | ----------------------------- |
| API                 | http://localhost:5080         |
| Swagger/OpenAPI     | http://localhost:5080/swagger |
| RabbitMQ Management | http://localhost:15672        |
| Ollama              | http://localhost:11434        |
| Aspire Dashboard    | http://localhost:18888        |

---

## Comandos úteis

### Subir ambiente de desenvolvimento

```bash
make up
```

### Entrar no container

```bash
make shell
```

### Derrubar ambiente

```bash
make down
```

### Reiniciar ambiente

```bash
make restart
```

### Restaurar pacotes

```bash
make restore
```

### Build

```bash
make build
```

### Testes

```bash
make test
```

### Rodar API

```bash
make api
```

### Rodar Worker

```bash
make worker
```

### Baixar modelo local no Ollama

```bash
make ollama-model
```

---

## Importante: comandos fora e dentro do container

Existem dois contextos diferentes:

```txt
Host / máquina local
   → onde Docker está instalado

Container dotnet_dev
   → onde .NET está instalado
```

Quando você está **fora do container**, pode usar:

```bash
make build
```

Internamente, o Makefile executa algo como:

```bash
docker compose -f compose.dev.yml exec dotnet_dev dotnet build
```

Quando você está **dentro do container**, não precisa chamar Docker. Pode executar diretamente:

```bash
dotnet build
```

ou, se o Makefile estiver preparado:

```bash
make build
```

---

## Erro comum: `docker: Permission denied` ou `docker: command not found`

Se você executar:

```bash
make build
```

e receber algo como:

```txt
make: docker: Permission denied
make: *** [Makefile:20: build] Error 127
```

ou:

```txt
zsh: command not found: docker
```

provavelmente você está tentando executar um comando que chama Docker **dentro do container**.

Exemplo de terminal dentro do container:

```bash
/workspace via .NET v10.0.100
```

Nesse caso, rode os comandos .NET diretamente:

```bash
dotnet restore
dotnet build
dotnet test
```

Para rodar a API:

```bash
dotnet run --project src/MeetingAgent.Api
```

Para rodar o Worker:

```bash
dotnet run --project src/MeetingAgent.Worker
```

---

## Makefile recomendado

Para evitar confusão, o Makefile pode detectar automaticamente se está rodando dentro do container.

```makefile
COMPOSE_DEV=docker compose -f compose.dev.yml

up:
	$(COMPOSE_DEV) up -d

down:
	$(COMPOSE_DEV) down

restart:
	$(COMPOSE_DEV) down
	$(COMPOSE_DEV) up -d

shell:
	$(COMPOSE_DEV) exec dotnet_dev zsh

restore:
	@if [ -f /.dockerenv ]; then \
		dotnet restore; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet restore; \
	fi

build:
	@if [ -f /.dockerenv ]; then \
		dotnet build; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet build; \
	fi

test:
	@if [ -f /.dockerenv ]; then \
		dotnet test; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet test; \
	fi

api:
	@if [ -f /.dockerenv ]; then \
		dotnet run --project src/MeetingAgent.Api; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet run --project src/MeetingAgent.Api; \
	fi

worker:
	@if [ -f /.dockerenv ]; then \
		dotnet run --project src/MeetingAgent.Worker; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet run --project src/MeetingAgent.Worker; \
	fi

ollama-model:
	$(COMPOSE_DEV) exec ollama ollama pull qwen3:8b
```

Com esse Makefile, os comandos abaixo funcionam tanto no host quanto dentro do container:

```bash
make restore
make build
make test
make api
make worker
```

Apenas estes comandos devem ser executados no host, porque dependem diretamente do Docker Compose:

```bash
make up
make down
make restart
make shell
```

---

## Teste rápido sem Microsoft Graph

A API possui um endpoint de importação manual para você testar o pipeline antes de configurar Teams/Graph.

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

---

### 1. Habilitar políticas de gravação/transcrição

No Microsoft Teams Admin Center:

```txt
Teams Admin Center
   ↓
Meetings
   ↓
Meeting policies
   ↓
Recording & transcription
```

Configurações recomendadas:

```txt
Cloud recording: On
Transcription: On
Recording storage: OneDrive/SharePoint
Who can record/transcribe: Organizer and co-organizers, ou política equivalente
```

---

### 2. Padronizar reuniões processadas

Crie um usuário ou serviço organizador, por exemplo:

```txt
meeting-agent@empresa.com
```

Esse usuário ajuda a:

* centralizar permissões;
* garantir política correta;
* controlar reuniões processadas;
* reduzir variação entre organizadores;
* facilitar auditoria.

---

### 3. Ativar transcrição automática quando possível

Nas reuniões que serão processadas:

```txt
Record automatically: On, se necessário
Transcribe automatically: On, quando disponível
Spoken language: pt-BR, se a reunião for em português
Meeting chat: Enabled
Participants: autenticados sempre que possível
```

---

### 4. Criar App Registration no Microsoft Entra ID

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

Depois crie um client secret:

```txt
Certificates & secrets
   ↓
New client secret
```

Preencha o `.env`:

```env
AZURE_TENANT_ID=
AZURE_CLIENT_ID=
AZURE_CLIENT_SECRET=
GRAPH_BASE_URL=https://graph.microsoft.com/v1.0
```

---

### 5. Permissões do Microsoft Graph

Para ambiente controlado/MVP:

```txt
OnlineMeetingTranscript.Read.All
OnlineMeetingRecording.Read.All
OnlineMeetings.Read.All
Calendars.Read
User.Read.All
```

Essas permissões exigem consentimento administrativo.

Para produto real, avalie Resource-Specific Consent para reduzir escopo.

---

### 6. Webhook do Graph

O endpoint preparado para receber notificações é:

```txt
POST /webhooks/graph
```

Durante validação inicial do Graph, a API responde `validationToken` quando esse parâmetro vem na query string.

---

## Endpoints principais

| Método | Endpoint                 | Descrição                       |
| ------ | ------------------------ | ------------------------------- |
| GET    | `/health`                | Healthcheck simples             |
| GET    | `/ready`                 | Readiness check                 |
| POST   | `/webhooks/graph`        | Webhook Microsoft Graph         |
| POST   | `/meetings/import`       | Importação manual de transcript |
| GET    | `/meetings`              | Lista reuniões                  |
| GET    | `/meetings/{id}`         | Detalha reunião                 |
| GET    | `/meetings/{id}/summary` | Busca ata/resumo                |
| POST   | `/meetings/{id}/process` | Reprocessa reunião              |

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
│   └── MeetingAgent.UnitTests/
│
├── docs/
│   ├── stack.md
│   ├── architecture.md
│   ├── teams-setup.md
│   ├── graph-permissions.md
│   ├── development.md
│   └── adr/
│
├── samples/
├── scripts/
├── deploy/
├── .devcontainer/
│   ├── Dockerfile
│   ├── devcontainer.json
│   └── .zshrc
│
├── compose.dev.yml
├── docker-compose.yml
├── .env.example
├── Makefile
└── README.md
```

---

## Estratégia de escalabilidade

### Separação de responsabilidades

* `MeetingAgent.Api`: recebe webhooks, expõe endpoints e autentica chamadas.
* `MeetingAgent.Worker`: processa tarefas pesadas fora da request HTTP.
* `MeetingAgent.Application`: concentra regras de negócio e workflow.
* `MeetingAgent.Infrastructure`: integra com Graph, IA, storage e banco.
* `MeetingAgent.Domain`: mantém modelo central independente de frameworks.

---

### Processamento assíncrono

Nada pesado deve rodar dentro do webhook.

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

### Idempotência

Todo evento deve ser idempotente:

```txt
Se transcript X da reunião Y já foi processado, não processar novamente.
```

---

### Observabilidade

Todo fluxo deve carregar:

```txt
correlationId
meetingId
transcriptId
jobId
```

---

## Roadmap

### Fase 1 — MVP local

* API + Worker
* Importação manual de transcript
* Parser VTT/texto
* Limpeza heurística
* Resumo heurístico ou Ollama
* Ata em JSON/Markdown

### Fase 2 — Microsoft Graph

* App Registration
* Client credentials
* Download de transcript
* Webhook de transcript disponível
* Persistência real

### Fase 3 — Teams App

* Adaptive Cards
* Publicação da ata no chat
* Bot informativo
* Consentimento explícito

### Fase 4 — IA avançada

* Microsoft Agent Framework
* Workflow multiagente
* Revisão anti-alucinação
* RAG com reuniões anteriores

### Fase 5 — Áudio avançado

* Baixar gravação
* Reprocessar com WhisperX/faster-whisper
* Diarização avançada
* Comparação com transcript oficial

---

## Documentação complementar

* `docs/development.md`: ambiente de desenvolvimento em detalhes
* `docs/stack.md`: decisões de stack
* `docs/architecture.md`: arquitetura técnica
* `docs/teams-setup.md`: configuração do Microsoft Teams
* `docs/graph-permissions.md`: permissões Microsoft Graph
* `docs/adr/`: registros de decisão arquitetural

---

## Decisão final

O princípio arquitetural do projeto é:

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
