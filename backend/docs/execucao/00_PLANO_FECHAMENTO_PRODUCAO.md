# Plano de fechamento para produção

## Objetivo geral
Fechar o fluxo produtivo do Valora Insight sem criar caminhos paralelos: fundação, primeiro acesso, base SaaS, diagnóstico público, inteligência rastreável, entregáveis, operação, Enterprise, go-live e refinamento visual. Testes automatizados permanecem na última fase.

## Ordem, dependências e estado
| Fase | Dependência | Status em 2026-08-19 | Aceite da fase |
|---|---|---|---|
| 0. Build, startup, configuração, SQL e saúde | SDK .NET e PostgreSQL | Parcial: validação de produção reforçada; SDK indisponível neste ambiente | clean/restore/build, hosts e SQL executados |
| 1. Bootstrap e login | Fase 0 e banco migrado | Não homologada | login/logout, auditoria e dashboard reais |
| 2. Base SaaS | Identidade e tenant válidos | Não homologada | organização, usuários, RBAC e plano sem 404 |
| 3. Diagnóstico público | Base SaaS e entitlement | Não homologada | publicar, LGPD e resposta transacional |
| 4. Inteligência | Respostas e mapeamento metodológico | Não homologada | evidência até Action/Journey, rastreável |
| 5. Entregáveis | Inteligência processada | Não homologada | previews e exportações reais/honestas |
| 6. Operação | Eventos dos fluxos anteriores | Não homologada | governança, auditoria, notificações e saúde |
| 7. Enterprise | Entitlements e governança | Não iniciada nesta execução | integrações reais ou estado não configurado |
| 8. Go-live | Fases produtivas homologadas | Não iniciada | publish, backup/restore, segurança e rollback |
| 9. Design final | Fluxos funcionais estáveis | Não iniciada | responsividade e estados consistentes |
| 10. Testes | Todas as anteriores estáveis | Adiada deliberadamente | suíte definida no fim do ciclo |

## Riscos e controles
- **SDK ausente:** impede comprovar compilação/startup; executar em agente com .NET 8.
- **Banco não disponibilizado:** impede validar o script canônico; executar em clone anonimizado antes de produção.
- **Configuração:** em produção, toda ocorrência marcada `IsBlocking` agora impede startup; somente códigos, nunca valores, são registrados.
- **Escopo amplo:** concluir uma prioridade e seu smoke antes de iniciar a seguinte.
