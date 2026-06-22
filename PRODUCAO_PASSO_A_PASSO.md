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
