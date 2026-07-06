# Sprint Backend Oficial — Migration Reality Check Audit

## 1. Resumo
Esta sprint confirmou o backend oficial em `backend/Valora.sln`, revisou o contrato SQL real de planos/assinaturas, removeu dependência runtime de colunas inexistentes em `plans` e registrou gates/documentação para homologação real.

## 2. Projeto novo identificado
`backend/Valora.sln` é a base oficial com API, Application, Domain, Infrastructure, Tests e Web.

## 3. Projeto legado identificado
O legado da raiz inicia em `index.html`, usa JavaScript puro/Firebase e permanece apenas como referência funcional.

## 4. Contexto da migração
A migração consolida regras e jornadas do legado em ASP.NET + PostgreSQL, sem criar frontend SPA ou nova solution paralela.

## 5. O que já foi migrado
- APIs e serviços oficiais para organizações, usuários, formulários, pesquisas, respostas, planos, relatórios, certificados, LGPD, e-mail, exportações, auditoria, migração e operações.
- Web Razor oficial para jornadas públicas e administrativas principais.
- Seeds PostgreSQL para planos, limites, capacidades, organização Valora e assinatura.

## 6. O que falta migrar
- Homologação com PostgreSQL real e usuários piloto.
- Validação de runtime real de e-mail, certificado e relatório.
- Redução progressiva dos gaps HTTP 501 controlados remanescentes.

## 7. Skills/regras aplicadas
Não foi necessário usar skill externa. Foram aplicadas as regras do legado como referência funcional, mantendo backend oficial como única base evolutiva.

## 8. Erros SQL corrigidos
- `price_label` não deve existir nem ser referenciado em SQL oficial.
- `badge` não deve existir nem ser referenciado em SQL oficial.
- O repositório oficial de planos deixou de consultar `badge`, `price_complement` e `visible_on_public_pricing`, pois não existem no schema real atual.

## 9. Scripts SQL corrigidos
Os scripts oficiais já estavam em contrato estruturado para `plans`, `plan_limits`, `plan_capabilities`, `organizations` e `subscriptions`; o gate SQL foi reforçado via `backend:sql-schema-validate` para impedir regressão.

## 10. Paridade de roles
Roles oficiais mapeados: `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante`, `convidado_externo`.

## 11. Paridade de módulos
Módulos oficiais mapeados: dashboard, clientes/organização, financeiro/planos, usuarios, funcionarios, formularios, pesquisas, convitesEmail/e-mail, respostas, relatorios, certificados, actionPlans, valorabot, support, lgpd, integrations, exportacoes, benchmark, whiteLabel, backup, logs, diagnosticosGratuitos, operacaoAssistida e comunicacoes.

## 12. Paridade de jornadas
Jornadas cobertas/documentadas: home pública, diagnóstico gratuito, pesquisa pública, envio de resposta, resultado público, certificado, e-mail, login, dashboard, clientes, usuários, formulários, pesquisas, links públicos, respostas, relatórios, exportações, certificados, LGPD, comunicações, auditoria, migração e health/operações.

## 13. Gaps removidos
Removida a dependência de colunas inexistentes de planos no repositório oficial, evitando falha runtime equivalente aos erros de seed SQL.

## 14. Gaps restantes
Permanecem sujeitos a `ASPNET_WEB_API_GAPS.md`: endpoints que ainda não possuam repositório real devem responder apenas HTTP 501 controlado e motivo documentado.

## 15. Validadores criados/atualizados
- `tools/validate-backend-official-sql-schema.js` validado como gate SQL/schema.
- `package.json` recebeu script `backend:sql-schema-validate`.

## 16. Comandos executados
- `dotnet restore backend/Valora.sln && dotnet build backend/Valora.sln --no-restore && dotnet test backend/Valora.sln --no-build` foi tentado, mas o ambiente não possui `dotnet` instalado.
- `npm run backend:official-validate`.
- `npm run backend:reports-email-validate`.
- `npm run backend:migration-import-validate`.
- `npm run backend:homologation-cutover-validate`.
- `npm run backend:sql-schema-validate`.
- `npm run web:permission-parity`.
- `npm run web:module-parity`.
- `npm run web:journey-parity`.
- `npm run web:no-fake-admin-data`.
- `npm run web:business-rules`.
- `npm run check:critical`.

## 17. Comandos não executados e motivo
Nenhum comando solicitado foi omitido; os comandos `dotnet` foram iniciados e falharam por limitação do ambiente (`dotnet: command not found`).

## 18. Riscos
- Divergência entre apresentação comercial do legado e schema oficial se novos atributos forem adicionados sem migration formal.
- Homologação real ainda pode revelar dados inconsistentes de migração, SMTP/certificado/relatório e permissões por organização.

## 19. Próximo passo recomendado
Executar homologação real com PostgreSQL e usuários piloto, corrigir bugs de runtime, gerar pacote final de produção e preparar cutover manual conforme `CUTOVER_PLAN.md`.
