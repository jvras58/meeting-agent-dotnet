# IA, logs e modo watch

Este documento explica o comportamento atual da IA, dos logs e do modo de desenvolvimento com reload.

## Estado atual do processamento

O pipeline de resumo possui dois caminhos:

```txt
Transcript normalizado
   ↓
AiMeetingSummaryBuilder
   ↓
IA via IAiChatService
   ↓
Fallback heurístico, se a IA falhar
   ↓
MarkdownSummaryRenderer
```

Ou seja, o projeto agora tenta usar IA real quando `AI_PROVIDER=ollama` está configurado.

Se a IA não estiver configurada, falhar ou retornar um JSON inválido, o sistema volta automaticamente para o resumo heurístico.

Isso é intencional para manter o ambiente local funcionando mesmo sem tenant Microsoft 365, sem Graph e sem modelo local carregado.

---

## Modo heurístico

Configuração padrão:

```env
AI_PROVIDER=heuristic
AI_MODEL=qwen3:8b
```

Nesse modo, o processamento é rápido porque não chama modelo nenhum.

O sistema usa regras simples em C# para detectar:

- principais pontos;
- decisões;
- tarefas;
- riscos;
- perguntas em aberto.

Esse modo serve para validar API, parser, estrutura do projeto e contratos.

---

## Modo com Ollama

Para usar IA real localmente:

```env
AI_PROVIDER=ollama
AI_MODEL=qwen3:8b
AI_BASE_URL=http://ollama:11434
```

Dentro do `compose.dev.yml`, o hostname correto para o Ollama é:

```txt
http://ollama:11434
```

Rodando a API direto no host, fora do container, use:

```txt
http://localhost:11434
```

Baixe o modelo:

```bash
make ollama-model
```

Ou diretamente:

```bash
docker compose -f compose.dev.yml exec ollama ollama pull qwen3:8b
```

Depois rode a API:

```bash
make api-watch
```

Ao importar uma reunião, os logs devem mostrar algo como:

```txt
Calling AI summary service...
Calling Ollama...
Ollama response received...
AI summary generated...
```

Se aparecer fallback heurístico, significa que o Ollama falhou ou retornou fora do JSON esperado.

---

## Formato esperado da resposta da IA

A IA deve retornar apenas JSON válido:

```json
{
  "executive_summary": "Resumo executivo curto.",
  "main_points": ["Ponto principal"],
  "decisions": [
    {
      "text": "Decisão tomada",
      "context": "Contexto"
    }
  ],
  "action_items": [
    {
      "task": "Tarefa",
      "owner": "Responsável",
      "due_date": "2026-07-10"
    }
  ],
  "risks": [
    {
      "text": "Risco",
      "severity": "média"
    }
  ],
  "open_questions": ["Pergunta em aberto"]
}
```

O parser tenta extrair o primeiro objeto JSON da resposta.

Se o modelo responder com Markdown, texto solto ou JSON quebrado, o fallback heurístico será usado.

---

## Logs da aplicação

A API e o Worker usam logs no console.

Para ver logs da API, rode a API diretamente:

```bash
make api
```

Ou com reload:

```bash
make api-watch
```

Os logs aparecem no mesmo terminal.

Para o Worker:

```bash
make worker
```

Ou:

```bash
make worker-watch
```

---

## Logs dos containers

Para ver todos os logs do compose:

```bash
make logs
```

Logs específicos:

```bash
make logs-ollama
make logs-rabbitmq
make logs-postgres
```

Atenção: a API e o Worker normalmente não aparecem em `docker compose logs`, porque o container `dotnet_dev` fica parado com `tail -f /dev/null` e a aplicação é iniciada manualmente via `dotnet run` ou `dotnet watch`.

---

## Modo watch / reload

Para desenvolvimento com reload automático:

```bash
make api-watch
```

Em outro terminal:

```bash
make worker-watch
```

Isso usa:

```bash
dotnet watch --project src/MeetingAgent.Api run
```

E:

```bash
dotnet watch --project src/MeetingAgent.Worker run
```

---

## Teste rápido com IA real

1. Suba o ambiente:

```bash
make up
```

2. Copie o `.env`:

```bash
cp .env.example .env
```

3. Edite o `.env`:

```env
AI_PROVIDER=ollama
AI_MODEL=qwen3:8b
AI_BASE_URL=http://ollama:11434
```

4. Baixe o modelo:

```bash
make ollama-model
```

5. Entre no container:

```bash
make shell
```

6. Rode a API:

```bash
make api-watch
```

7. Em outro terminal, importe uma reunião:

```bash
curl -X POST http://localhost:5080/meetings/import \
  -H "Content-Type: application/json" \
  -d @samples/sample-import-request.json
```

8. Observe os logs no terminal da API.

---

## Como saber se usou IA real?

Usou IA real se aparecerem logs parecidos com:

```txt
Calling AI summary service
Calling Ollama
Ollama response received
AI summary generated
```

Não usou IA real se aparecer:

```txt
AI summary response could not be parsed
AI summary service failed
Falling back to heuristic summary
```

Ou se `AI_PROVIDER=heuristic` estiver ativo.

---

## Decisão de arquitetura

O projeto mantém fallback heurístico por segurança:

```txt
IA disponível e resposta válida
   → usa IA

IA indisponível, lenta ou resposta inválida
   → usa heurística
```

Isso permite desenvolver e testar o fluxo completo mesmo sem modelo local ou sem internet.
