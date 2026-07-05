# ADR 0002 — Clean Architecture

## Status

Aceito.

## Decisão

Separar Domain, Application, Infrastructure, Api e Worker.

## Consequências

- Mais arquivos no início.
- Melhor manutenibilidade.
- Permite trocar Graph/IA/persistência sem afetar regras centrais.
