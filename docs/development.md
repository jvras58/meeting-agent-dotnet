# Ambiente de Desenvolvimento

Este documento explica como usar o ambiente de desenvolvimento do **Meeting Agent**.

O projeto foi pensado para funcionar sem depender de uma IDE específica. Você pode usar VS Code, JetBrains, terminal puro, WSL, Linux, macOS ou qualquer editor.

A entrada principal é:

```bash
make up
make shell
```

---

## 1. Visão geral

O ambiente usa Docker Compose para subir:

- `dotnet_dev`: container Linux com .NET SDK;
- PostgreSQL;
- Redis;
- RabbitMQ;
- Ollama.

A aplicação principal roda dentro do container `dotnet_dev`, com o código montado em `/workspace`.

```txt
Host / máquina local
   ↓
Docker Compose
   ↓
dotnet_dev
   ↓
/workspace
```

---

## 2. Pré-requisitos

Para o fluxo recomendado:

```txt
Docker
Docker Compose
Make
```

Você não precisa instalar o .NET SDK na máquina local se usar o container de desenvolvimento.

---

## 3. Configuração inicial

Copie o arquivo de ambiente:

```bash
cp .env.example .env
```

No PowerShell:

```powershell
Copy-Item .env.example .env
```

Para rodar sem IA real, mantenha:

```env
AI_PROVIDER=heuristic
```

Para rodar com Ollama:

```env
AI_PROVIDER=ollama
AI_MODEL=qwen3:8b
AI_BASE_URL=http://ollama:11434
```

> Dentro do container, o hostname correto do Ollama é `ollama`. Rodando direto no host, use `http://localhost:11434`.

---

## 4. Host vs container

Existem dois contextos:

```txt
Host / máquina local
   Onde Docker está instalado.

Container dotnet_dev
   Onde o .NET SDK está instalado.
```

No **host**, use comandos que controlam Docker Compose:

```bash
make up
make down
make restart
make shell
make logs
```

Dentro do **container**, use comandos .NET diretamente:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/MeetingAgent.Api
```

O Makefile também detecta quando está dentro do container para comandos como `make build`, `make test`, `make api`, `make worker`, `make ollama-model` e `make ollama-list`.

---

## 5. Subir ambiente

No host:

```bash
make up
```

Sem Makefile:

```bash
docker compose -f compose.dev.yml up -d
```

---

## 6. Entrar no container

No host:

```bash
make shell
```

Sem Makefile:

```bash
docker compose -f compose.dev.yml exec dotnet_dev zsh
```

Dentro do container, o projeto estará em:

```bash
/workspace
```

Você deve ver algo parecido com:

```bash
/workspace via .NET v10.0.100
```

---

## 7. Restaurar, buildar e testar

Dentro do container:

```bash
dotnet restore
dotnet build
dotnet test
```

Ou usando Makefile:

```bash
make restore
make build
make test
```

---

## 8. Rodar API

Dentro do container:

```bash
dotnet run --project src/MeetingAgent.Api
```

Ou:

```bash
make api
```

A API fica disponível em:

```txt
http://localhost:5080
```

Swagger/OpenAPI:

```txt
http://localhost:5080/swagger
```

---

## 9. Rodar API com reload

Para desenvolvimento com reload automático:

```bash
make api-watch
```

Ou diretamente:

```bash
dotnet watch --project src/MeetingAgent.Api run
```

Os logs aparecem no mesmo terminal.

---

## 10. Rodar Worker

Em outro terminal:

```bash
make shell
make worker
```

Ou diretamente dentro do container:

```bash
dotnet run --project src/MeetingAgent.Worker
```

---

## 11. Rodar Worker com reload

```bash
make worker-watch
```

Ou diretamente:

```bash
dotnet watch --project src/MeetingAgent.Worker run
```

---

## 12. Logs

### Logs da API e do Worker

A API e o Worker normalmente são iniciados manualmente com `dotnet run` ou `dotnet watch` dentro do container.

Por isso, os logs aparecem no terminal onde você iniciou o processo.

Exemplo:

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
Meeting summary workflow finished
```

### Logs dos serviços Docker

Todos os serviços:

```bash
make logs
```

Ollama:

```bash
make logs-ollama
```

RabbitMQ:

```bash
make logs-rabbitmq
```

PostgreSQL:

```bash
make logs-postgres
```

---

## 13. IA local com Ollama

Por padrão, o projeto usa fallback heurístico:

```env
AI_PROVIDER=heuristic
```

Nesse modo, o processamento é rápido porque não chama modelo real.

Para usar IA real:

```env
AI_PROVIDER=ollama
AI_MODEL=qwen3:8b
AI_BASE_URL=http://ollama:11434
```

O container do Ollama sobe junto com o ambiente, mas ele não vem com modelos baixados. Baixe o modelo padrão com:

```bash
make ollama-model
```

Esse comando funciona tanto no host quanto dentro do `dotnet_dev`.

- No host, ele usa Docker Compose para executar `ollama pull` no container do Ollama.
- Dentro do `dotnet_dev`, ele usa a API HTTP interna do Ollama.

Para baixar outro modelo:

```bash
make ollama-model OLLAMA_MODEL=qwen3:4b
```

Liste modelos disponíveis:

```bash
make ollama-list
```

Rode a API com reload:

```bash
make api-watch
```

Depois importe uma reunião e acompanhe os logs.

Se a IA falhar ou responder fora do JSON esperado, o sistema usa fallback heurístico automaticamente.

---

## 14. Teste rápido sem Microsoft Graph

Com a API rodando:

```bash
curl -X POST http://localhost:5080/meetings/import \
  -H "Content-Type: application/json" \
  -d @samples/sample-import-request.json
```

Liste reuniões:

```bash
curl http://localhost:5080/meetings
```

Consulte o resumo:

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

## 15. Erro comum: `docker: command not found`

Se você está dentro do container e roda um comando que chama Docker, pode ver:

```txt
zsh: command not found: docker
```

Isso acontece porque o Docker fica no host, não dentro do container.

Solução dentro do container:

```bash
dotnet build
```

Ou use o Makefile atualizado:

```bash
make build
```

---

## 16. Erro comum: `make: docker: Permission denied`

Isso normalmente indica que você está dentro do container usando um Makefile antigo que tentava chamar Docker.

Atualize o Makefile e rode:

```bash
make build
```

Ou execute diretamente:

```bash
dotnet build
```

---

## 17. Comandos úteis

```bash
make up              # sobe ambiente
make down            # derruba ambiente
make restart         # reinicia ambiente
make shell           # entra no dotnet_dev
make ps              # lista containers
make logs            # logs do compose
make restore         # dotnet restore
make build           # dotnet build
make test            # dotnet test
make api             # roda API
make api-watch       # roda API com reload
make worker          # roda Worker
make worker-watch    # roda Worker com reload
make ollama-model    # baixa modelo qwen3:8b
make ollama-list     # lista modelos do Ollama
```

---

## 18. Fluxo recomendado para desenvolvimento diário

No host:

```bash
make up
make shell
```

Dentro do container:

```bash
make restore
make build
make test
make ollama-model
make api-watch
```

Em outro terminal:

```bash
make shell
make worker-watch
```

No fim:

```bash
exit
make down
```

---

## 19. Documentação relacionada

- `docs/ai-observability.md`: detalhes sobre IA, fallback heurístico, logs e watch.
- `docs/stack.md`: stack e decisões técnicas.
- `docs/teams-setup.md`: configuração do Microsoft Teams.
- `docs/graph-permissions.md`: permissões do Microsoft Graph.
