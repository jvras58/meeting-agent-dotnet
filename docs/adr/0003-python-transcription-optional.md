# ADR 0003 — Serviço Python opcional para transcrição

## Status

Proposto.

## Decisão

Manter WhisperX/faster-whisper como serviço externo opcional, não dentro do backend .NET.

## Consequências

- .NET orquestra o produto.
- Python processa áudio quando necessário.
- Evita acoplar libs de ML diretamente ao backend.
