# Deploy Windows/IIS
1. Instale o Hosting Bundle compatível com o SDK indicado em `global.json` e o PostgreSQL client.
2. Configure um Application Pool **No Managed Code**, identidade sem privilégios administrativos e permissões de leitura na publicação e escrita somente nos diretórios de logs/storage.
3. Publique API e Web separadamente: `dotnet publish Valora.Api/Valora.Api.csproj -c Release -o ./publish/api` e `dotnet publish Valora.Web/Valora.Web.csproj -c Release -o ./publish/web`.
4. Crie aplicações/sites distintos, configure HTTPS e as variáveis do documento de produção no IIS Configuration Editor ou no ambiente do processo. Nunca grave secrets no repositório/web.config.
5. Aponte `Api__BaseUrl` da Web à API, aplique `database/postgresql/script_completo.sql` com uma conta de migração e recicle os pools.
6. Valide `/health`, `/health/database`, `/health/config`, `/health/web` e o checklist pós-deploy. Preserve a publicação anterior para rollback.
