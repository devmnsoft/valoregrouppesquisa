# Pendências reais

| Impacto | Prioridade | Pendência | Arquivo/área provável | Recomendação objetiva |
|---|---:|---|---|---|
| Crítico | P0 | SDK `dotnet` ausente; compilação não foi comprovada | ambiente/`global.json` | instalar SDK compatível e repetir clean/restore/build |
| Crítico | P0 | API e Web não foram iniciadas | `Valora.Api`, `Valora.Web` | iniciar com configuração Development e registrar URLs/status |
| Crítico | P0 | SQL não foi executado contra PostgreSQL existente | `database/postgresql/script_completo.sql` | executar duas vezes em clone anonimizado e revisar warnings |
| Alto | P0 | Bootstrap/login/System Health sem smoke | Auth, seed e Operations | validar cenários positivos e negativos sem criar teste agora |
| Alto | P1 | Base SaaS não homologada ponta a ponta | Organization/Users/Plans | concluir uma fatia vertical por vez |
| Alto | P2 | Diagnóstico público, LGPD e participação não homologados | Diagnostics/PublicSurvey | usar dados de homologação identificados como tal |
| Alto | P3 | Rastreabilidade do pipeline não comprovada | Intelligence | verificar origem e limiar mínimo antes de qualquer insight |
| Médio | P4-P7 | Entregáveis, operação, Enterprise, go-live e design aguardam fases anteriores | módulos correspondentes | não iniciar antes do aceite da prioridade anterior |
| Planejado | P8 | Testes automatizados | `Valora.Tests` existente | manter por último, conforme decisão deste ciclo |
