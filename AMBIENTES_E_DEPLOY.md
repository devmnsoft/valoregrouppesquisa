# Ambientes e deploy do Valora Pulse

## Local

- Uso: demonstração, desenvolvimento e validação funcional rápida.
- Execução: `./iniciar-site-mac-linux.sh` ou `INICIAR_SITE_WINDOWS.bat`.
- Persistência: `STORAGE_MODE: 'local'`, com `localStorage` e serviços locais do `server.py`.
- Firebase: não é obrigatório; se existir `firebase.local.json`, deve permanecer separado da configuração de produção.
- Segredos: não commitar `.env`, `data/email_config.json`, tokens ou service accounts.

## Homologação

- Uso: validar build real antes da produção, com dados de teste.
- Build: sempre executar `npm run build:prod`; o Hosting deve publicar `dist/`.
- Firebase: preferir projeto separado, informado por `FIREBASE_PROJECT_ID_HOMOLOG`.
- Preview de PR: workflow `Firebase Hosting preview` publica canal `pr-<numero>` quando os secrets estão configurados e o PR não vem de fork.
- App Check: usar modo debug/homologação e nunca misturar dados reais.

## Produção

- Uso: ambiente de clientes reais.
- Build: `dist/` gerado em ambiente limpo pelo workflow antes do deploy.
- Hosting: `firebase.json` deve manter `hosting.public` como `dist`.
- Functions: segredos via Firebase/Google Cloud Secret Manager.
- App Check: ativo e monitorado.
- Firestore Rules: publicadas e revisadas antes da release.
- Logs, alertas e backup: ativos antes de liberar tráfego.

## Fluxo recomendado

1. Abrir PR para `main`.
2. Aguardar workflow `PR validation` verde.
3. Validar preview/homologação quando aplicável.
4. Aprovar release conforme `CHECKLIST_RELEASE.md`.
5. Executar deploy manual em `Firebase production deploy` ou publicar tag `v*` com aprovação do environment `production`.
6. Registrar evidências em `TESTES_EXECUTADOS.md`.
