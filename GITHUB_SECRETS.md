# Secrets no GitHub e Firebase

## Secrets possíveis no GitHub

- `FIREBASE_TOKEN`: token usado pelo Firebase CLI nos workflows atuais. Preferir migrar futuramente para autenticação por service account/OIDC quando o projeto estiver pronto.
- `FIREBASE_SERVICE_ACCOUNT`: alternativa recomendada por actions oficiais do Firebase quando adotada.
- `FIREBASE_PROJECT_ID`: projeto padrão.
- `FIREBASE_PROJECT_ID_HOMOLOG`: projeto/canal de homologação.
- `FIREBASE_PROJECT_ID_PROD`: projeto de produção.

## Segredos que devem ficar no Firebase Secret Manager

- `TELEGRAM_BOT_TOKEN`.
- `TELEGRAM_CHAT_ID`, quando aplicável ao backend.
- `SMTP_HOST`, se for tratado como sensível pela operação.
- `SMTP_USER`.
- `SMTP_PASSWORD`.
- Chaves privadas e service accounts usadas por Cloud Functions.

## Regras obrigatórias

- Nunca commitar `.env`, `serviceAccount*.json`, `secrets.json`, `private_key`, tokens Telegram ou senha SMTP.
- O frontend publicado no Firebase Hosting não deve conter segredos.
- No GitHub, manter apenas o mínimo necessário para build/deploy.
- Rotacionar qualquer segredo que apareça em log, PR, artefato ou build.
