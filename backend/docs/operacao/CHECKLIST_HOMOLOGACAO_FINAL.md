# Checklist de homologação final

## Compilação e publicação

- [ ] `dotnet clean`
- [ ] `dotnet restore`
- [ ] `dotnet build`
- [ ] `dotnet publish Valora.Api/Valora.Api.csproj -c Release`
- [ ] `dotnet publish Valora.Web/Valora.Web.csproj -c Release`

## Fluxo produtivo

- [ ] Iniciar API e Web com configuração de homologação e registrar o correlationId da sessão.
- [ ] Validar login, Dashboard, Diagnósticos, Workspace e link público.
- [ ] Enviar uma resposta consentida e confirmar participação e registro LGPD.
- [ ] Validar estados reais de inteligência, relatório, certificado, exportações e integrações.
- [ ] Abrir `/SystemHealth`, revisar validação de configuração, schema, backup e manutenção.
- [ ] Executar `database/postgresql/script_completo.sql` duas vezes no banco de homologação sem perda de dados.
- [ ] Validar as rotas críticas em desktop e nas larguras 360, 390, 768, 1024, 1366 e 1440 px.
- [ ] Confirmar que erros de produção são sanitizados e exibem correlationId, sem stack trace.

Pendências devem ser registradas como incidentes; não marque recursos não configurados como saudáveis.
