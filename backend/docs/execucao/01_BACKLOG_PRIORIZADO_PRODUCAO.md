# Backlog priorizado de produção

| Ordem | Módulo/contexto | Regra | Arquivos prováveis | Dependência | Prioridade | Status | Aceite |
|---:|---|---|---|---|---:|---|---|
| 1 | Fundação/configuração | Produção não inicia com item obrigatório inválido | `Valora.Api/Program.cs`, `Valora.Api/Operations/ConfigurationValidationService.cs` | nenhuma | P0 | Concluído em código; build pendente | Todos os `IsBlocking` barram startup sem vazar valor |
| 2 | Fundação/build | Solução precisa compilar | `Valora.sln`, projetos existentes | SDK .NET 8 | P0 | Bloqueado pelo ambiente | clean/restore/build aprovados |
| 3 | Banco | Migração canônica idempotente | `database/postgresql/script_completo.sql` | PostgreSQL disponível | P0 | Pendente de execução | duas execuções sem erro/destruição |
| 4 | Startup | API e Web respondem | `Valora.Api`, `Valora.Web` | build e banco | P0 | Pendente | `/health`, `/Login` e `/SystemHealth` respondem |
| 5 | Login/bootstrap | Seed somente em desenvolvimento e idempotente | configuração e infraestrutura existentes | banco | P0 | Pendente de homologação | admin dev entra; inválido recebe mensagem padrão |
| 6 | Base SaaS | tenant, permissão e entitlement no backend | serviços/controllers/BFF existentes | login | P1 | Não iniciada | fluxos administrativos completos |
| 7 | Diagnóstico | LGPD e resposta atômicas; pipeline não reverte resposta | módulos Diagnostics/PublicSurvey | P1 | P2 | Não iniciada | smoke público ponta a ponta |
| 8 | Inteligência | saída somente com evidência rastreável | módulos Intelligence | respostas reais | P3 | Não iniciada | Evidence→Action/Journey |
| 9 | Entregáveis/operação | exportar somente formato real; auditar | Reports/Certificates/Governance | inteligência | P4 | Não iniciada | previews e eventos válidos |
| 10 | Go-live/design | segurança e UX após estabilidade | API/Web/docs operacionais | fases anteriores | P6/P7 | Não iniciada | publish e matriz responsiva |
| 11 | Testes automatizados | somente após estabilização | projeto de testes existente | todas anteriores | P8 | Adiado | executar/criar apenas no fechamento |
