# Produção — passo a passo

## Publicar tudo
Execute `tools/windows/07-publicar-tudo.bat` em uma estação Windows com acesso ao IIS e à chave do Firebase Admin SDK.

## Fazer uma etapa por vez
1. `tools/windows/01-validar-codigo.bat`
2. `tools/windows/02-importar-base-producao.bat`
3. `tools/windows/03-validar-base-producao.bat`
4. `tools/windows/04-gerar-dist-producao.bat`
5. `tools/windows/05-publicar-iis.bat`
6. `tools/windows/06-healthcheck-prd.bat`

## Apenas abrir produção
Execute `tools/windows/08-abrir-producao.bat`.

## Spark agora; Blaze depois
No Spark, Cloud Functions ficam desabilitadas e recursos de e-mail/integração server-side usam fallback amigável. Ao migrar para Blaze, habilite `ENABLE_CLOUD_FUNCTIONS`, publique Functions e reative `sendSurveyInvitations`, `getEmailStatus`, integrações e webhooks server-side.

## Correção runtime capabilities e e-mail por ambiente

- Local: `server.py` fornece API local, outbox e SMTP opcional.
- PRD Spark: IIS estático + Firebase Auth/Firestore, sem API local, sem Cloud Functions, sem envio automático de e-mail.
- PRD Blaze futuro: Cloud Functions com Secret Manager para e-mail seguro e logs remotos.
- Backend externo futuro: API autenticada para transporte externo.
- Validações: `node scripts/validate-runtime-capabilities.js` e `node scripts/validate-email-environment.js` garantem que PRD Spark não chame `/api/email/*`, `/api/outbox`, `getEmailStatus` ou `logServerEvent`.
