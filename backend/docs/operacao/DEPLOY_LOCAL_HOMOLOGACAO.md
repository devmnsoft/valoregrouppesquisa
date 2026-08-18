# Deploy local de homologação
1. Instale o SDK de `global.json` e PostgreSQL; copie apenas os valores necessários de `.env.example` para variáveis locais.
2. Execute `dotnet restore`, aplique `database/postgresql/script_completo.sql`, e rode API e Web em terminais separados com `dotnet run --project Valora.Api` e `dotnet run --project Valora.Web`.
3. Use `ASPNETCORE_ENVIRONMENT=Development` somente localmente. Não reutilize credenciais de produção nem habilite seed demo em produção.
4. Consulte `/SystemHealth`; valide login, dashboard, diagnóstico, fluxo público, relatórios, certificados, notificações e integrações antes de promover o artefato.
