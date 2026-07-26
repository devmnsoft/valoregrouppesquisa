# Setup local PostgreSQL

Atualizado na Sprint 64.

## Resumo
- Mantém o legado Firebase ativo até a virada final.
- Registra o script único `banco_completo.sql` e a cópia em `backend/database/postgresql/banco_completo.sql`.
- Documenta paridade progressiva do Valora.Api/Valora.Web com o legado.
- Segredos SMTP devem ficar fora do repositório.

## Comandos úteis
```bash
psql -U postgres -d postgres -f banco_completo.sql
psql "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123456" -f banco_completo.sql
npm run db:scriptbd-completo
npm run security:no-secrets
```

## Riscos antes da virada
- Validar em PostgreSQL real de homologação antes de produção.
- Executar testes E2E com navegadores instalados.
- Configurar SMTP por User Secrets, variável de ambiente ou Firebase Secret Manager; nunca commitar senha.
