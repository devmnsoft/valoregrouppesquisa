# Checklist de startup

1. Instale o SDK definido em `global.json` e PostgreSQL compatível.
2. Copie apenas as variáveis necessárias de `.env.example`; nunca reutilize a chave DEV em produção.
3. Execute `dotnet clean Valora.sln`, `dotnet restore Valora.sln` e `dotnet build Valora.sln --no-restore` dentro de `backend/`.
4. Aplique `database/postgresql/script_completo.sql` com `ON_ERROR_STOP=1` duas vezes no mesmo banco.
5. Em Development, inicie `dotnet run --project Valora.Api` e depois `dotnet run --project Valora.Web`.
6. Confirme `/health`, `/health/database`, `/Login`, `/Dashboard` e `/SystemHealth`.
7. Faça login, confirme organização/plano/roles e percorra Organização, Usuários, Planos e Notificações.
8. Em produção, confirme que demo seed e erros detalhados estão desativados, HTTPS/CORS configurados e `Jwt__SigningKey` vem de secret manager.
