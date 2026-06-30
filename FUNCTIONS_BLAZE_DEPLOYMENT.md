# Functions Blaze Deployment

## Objetivo
Publicar Cloud Functions reais do projeto `gestordepesquisa` após ativação do Firebase Blaze, mantendo SMTP em Firebase Secrets.

## Pré-requisitos
- Firebase CLI autenticado no projeto correto.
- Plano Blaze ativo no projeto `gestordepesquisa`.
- Variáveis SMTP não secretas configuradas no ambiente/console conforme política do projeto.
- Secret `SMTP_PASSWORD` configurado via Firebase Secrets.

## Comandos obrigatórios
```bash
firebase functions:secrets:set SMTP_PASSWORD --project gestordepesquisa
firebase deploy --only functions --project gestordepesquisa
firebase deploy --only hosting --project gestordepesquisa
```

## Validação pós-deploy
```bash
npm run functions:production-deployment
npm run email:blaze-functions-readiness
npm run config:blaze
npm run config:cache-busting
```

## Endpoints esperados
- `sendEmail`: envio operacional, convite, resultado e teste.
- `getEmailStatus`: diagnóstico mascarado do remetente SMTP.

## Segurança
Nunca registrar `SMTP_PASSWORD`, `tokenHash`, `resultToken`, service account ou private key em logs, relatórios ou telas administrativas.
