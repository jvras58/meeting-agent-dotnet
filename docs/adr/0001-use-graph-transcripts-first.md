# ADR 0001 — Usar Graph Transcripts primeiro

## Status

Aceito.

## Contexto

O objetivo é gerar resumo de reuniões do Teams. Captura de áudio em tempo real com bots de mídia aumenta a complexidade e exige cuidados pesados de compliance.

## Decisão

Começar com transcrições oficiais do Teams via Microsoft Graph.

## Consequências

- Menos complexidade no MVP.
- Dependência de transcrição habilitada no Teams.
- Evolução futura pode baixar gravação e reprocessar áudio.
