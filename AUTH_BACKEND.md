# Autenticação Backend
Endpoints: `POST /auth/register-company`, `POST /auth/login`, `POST /auth/forgot-password`, `GET /me`. Senhas são armazenadas com BCrypt e o acesso usa JWT Bearer. O segredo versionado é placeholder; em produção usar variáveis de ambiente conforme `.env.example`.
