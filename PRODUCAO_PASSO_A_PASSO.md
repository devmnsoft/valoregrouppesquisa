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

## Correção integrada — e-mail, certificados e ValoraBot

- Validar com `node scripts/validate-email-runtime.js`.
- Validar certificados com `node scripts/validate-certificates.js`.
- Validar linguagem natural do bot com `node scripts/validate-chatbot-natural-language.js`.
- Em produção IIS estática, e-mail automático fica indisponível até ativar um transporte seguro; modelos continuam editáveis.

## Contratos de dados e resiliência de render

- Validar contratos: `node scripts/validate-data-contracts.js`.
- Validar render com dados legados: `node scripts/validate-render-resilience.js`.
- Auditar Firestore: `node scripts/audit-data-shapes.js --project gestordepesquisa`.
- Reparar Firestore com backup: `node scripts/repair-data-shapes.js --project gestordepesquisa --backup --apply`.
- No Windows Server, usar `tools\windows\19-corrigir-contratos-dados.bat` e `tools\windows\20-reparar-dados-firestore.bat`.
