# Permissões do Microsoft Graph

## MVP controlado

Permissões de aplicação possíveis:

```txt
OnlineMeetingTranscript.Read.All
OnlineMeetingRecording.Read.All
OnlineMeetings.Read.All
Calendars.Read
User.Read.All
```

Essas permissões exigem consentimento administrativo.

## Produção

Para produção, considerar:

- Resource-Specific Consent quando aplicável;
- escopo por time/reunião;
- auditoria de acesso;
- menor privilégio possível;
- rotação de secrets;
- preferir certificado a client secret quando possível.
