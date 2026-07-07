# Processamento assíncrono com PostgreSQL e RabbitMQ

Este documento descreve o fluxo assíncrono local do Meeting Agent.

## Objetivo

A API não deve fazer o processamento pesado da reunião dentro da request HTTP.

O fluxo correto é:

```txt
POST /meetings/import
   ↓
API salva meeting/transcript no PostgreSQL
   ↓
API publica MeetingProcessingRequested no RabbitMQ
   ↓
Worker consome o job
   ↓
Worker chama ProcessMeetingUseCase
   ↓
Worker gera summary com IA/fallback heurístico
   ↓
Worker salva summary no PostgreSQL
```

---

## Serviços usados

O `compose.dev.yml` já sobe:

- PostgreSQL;
- RabbitMQ;
- Ollama;
- Redis;
- container .NET de desenvolvimento.

Nesta fase, PostgreSQL e RabbitMQ deixam de ser infraestrutura ociosa e passam a fazer parte do fluxo real.

---

## Variáveis principais

```env
DATABASE_PROVIDER=postgres
DATABASE_URL=Host=postgres;Port=5432;Database=meeting_agent;Username=postgres;Password=postgres

RABBITMQ_HOST=rabbitmq
RABBITMQ_PORT=5672
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_QUEUE=meeting.processing.requested
```

---

## Comportamento da API

Ao receber:

```http
POST /meetings/import
```

A API:

1. cria a reunião;
2. cria o transcript;
3. marca a reunião como `Queued`;
4. salva no PostgreSQL;
5. publica um job no RabbitMQ;
6. retorna `202 Accepted`.

A geração do resumo fica para o Worker.

---

## Comportamento do Worker

O Worker:

1. conecta no RabbitMQ;
2. declara a fila `meeting.processing.requested`;
3. consome mensagens uma por vez;
4. carrega meeting/transcript do PostgreSQL;
5. executa o workflow de resumo;
6. salva summary no PostgreSQL;
7. confirma a mensagem no RabbitMQ.

Se houver erro, o job é reenfileirado.

---

## Fluxo de teste

Terminal 1:

```bash
make up
make shell
make api-watch
```

Terminal 2:

```bash
make shell
make worker-watch
```

Terminal 3:

```bash
curl -X POST http://localhost:5080/meetings/import \
  -H "Content-Type: application/json" \
  -d @samples/sample-import-request.json
```

Depois consulte:

```bash
curl http://localhost:5080/meetings
curl http://localhost:5080/meetings/{meetingId}/summary
```

---

## RabbitMQ Management

Acesse:

```txt
http://localhost:15672
```

Credenciais padrão:

```txt
guest / guest
```

Fila esperada:

```txt
meeting.processing.requested
```

---

## PostgreSQL

Banco padrão:

```txt
meeting_agent
```

Tabelas criadas automaticamente no startup da API/Worker:

```txt
meetings
transcripts
summaries
```

---

## Próximo passo futuro

Para produção, o ideal é evoluir para padrão outbox:

```txt
salva meeting/transcript + outbox event na mesma transação
   ↓
outbox publisher publica no RabbitMQ
   ↓
Worker processa
```

No MVP local, a publicação direta no RabbitMQ já resolve o fluxo assíncrono e elimina o processamento pesado da request HTTP.
