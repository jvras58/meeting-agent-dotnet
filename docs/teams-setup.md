# Configuração do Microsoft Teams

## Objetivo

Configurar o Teams para gerar o máximo de artefatos úteis para o Meeting Agent.

## Requisito mínimo

A reunião precisa ter **transcrição habilitada**.

```txt
Sem transcrição → Graph-only não processa.
Com transcrição → MVP funciona.
Com gravação + transcrição → melhor cenário para evolução.
```

## Teams Admin Center

```txt
Teams Admin Center
  ↓
Meetings
  ↓
Meeting policies
  ↓
Recording & transcription
```

Ative:

```txt
Cloud recording: On
Transcription: On
Store recordings: OneDrive/SharePoint
```

## Reuniões

Para reuniões importantes:

```txt
Transcribe automatically: On
Record automatically: On, se a política permitir
Spoken language: pt-BR
Meeting chat: Enabled
Participants authenticated
```

## App Registration

Criar app no Microsoft Entra ID:

```txt
App registrations → New registration → Meeting Agent
```

Secrets necessários:

```env
AZURE_TENANT_ID=
AZURE_CLIENT_ID=
AZURE_CLIENT_SECRET=
```

## Permissões sugeridas para MVP

```txt
OnlineMeetingTranscript.Read.All
OnlineMeetingRecording.Read.All
OnlineMeetings.Read.All
Calendars.Read
User.Read.All
```

Use consentimento administrativo e revise o escopo com segurança/compliance antes de produção.
