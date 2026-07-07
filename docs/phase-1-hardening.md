# Fase 1.1 — Hardening do processamento assíncrono

Esta etapa fecha as principais pendências antes da Fase 2 com Graph/Teams real.

## O que mudou

### Migrations versionadas

O bootstrap do PostgreSQL passou a usar tabela `schema_migrations` e migrations com versão.

Migrations atuais:

- `202607070001_initial_meeting_schema`
- `202607070002_meeting_processing_outbox`

### Outbox transacional

O import não salva mais meeting/transcript e publica RabbitMQ em passos separados.

Agora o fluxo é:

```txt
API recebe POST /meetings/import
   ↓
abre transação PostgreSQL
   ↓
salva meeting
   ↓
salva transcript
   ↓
salva outbox_messages
   ↓
commit
```

Depois, o `OutboxPublisherWorker` lê `outbox_messages` e publica no RabbitMQ.

Isso evita a falha clássica:

```txt
salvou no banco
mas falhou ao publicar no RabbitMQ
```

### Retry e DLQ

O Worker não usa mais requeue infinito para erro permanente.

Agora ele usa header:

```txt
x-retry-count
```

Ao bater o limite configurado, a mensagem vai para:

```txt
meeting.processing.dead-letter
```

Variáveis:

```env
RABBITMQ_DEAD_LETTER_QUEUE=meeting.processing.dead-letter
RABBITMQ_MAX_RETRY_ATTEMPTS=3
RABBITMQ_RETRY_DELAY_SECONDS=5
OUTBOX_BATCH_SIZE=10
```

### Testes

Foi adicionado teste de contrato para o import garantir que ele chama o store transacional e gera evento `MeetingProcessingRequested`.

Também há um teste opcional de integração PostgreSQL. Ele só executa de verdade se a variável abaixo existir:

```env
TEST_DATABASE_URL=Host=postgres;Port=5432;Database=meeting_agent;Username=postgres;Password=postgres
```

Sem essa variável, o teste retorna sem tocar banco externo.

## Como validar localmente

```bash
make build
make test
```

Para rodar o teste de integração PostgreSQL dentro do devcontainer:

```bash
TEST_DATABASE_URL="Host=postgres;Port=5432;Database=meeting_agent;Username=postgres;Password=postgres" make test
```

Fluxo completo:

```bash
make api-watch
make worker-watch
```

```bash
curl -sS -X POST http://localhost:5080/meetings/import \
  -H "Content-Type: application/json" \
  -d @samples/sample-import-request.json
```

Depois conferir:

```bash
curl -sS http://localhost:5080/meetings
curl -sS http://localhost:5080/meetings/{meetingId}/summary
```

RabbitMQ Management:

```txt
http://localhost:15672
```

Filas esperadas:

```txt
meeting.processing.requested
meeting.processing.dead-letter
```
