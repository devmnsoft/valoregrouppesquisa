# Matriz de módulos e regras

> “Não homologado” significa que a existência de código não foi confundida com validação funcional.

| Módulo | Regra principal | Permissão/plano | Entidade/tabela | API / BFF / View-JS | Governança/auditoria | Estado |
|---|---|---|---|---|---|---|
| Configuração/Saúde | segredo fraco/placeholder e configuração obrigatória bloqueiam produção | plataforma | n/a | configuração e System Health | loga apenas códigos | Reforçado; execução pendente |
| Login | inativo/suspenso/sem tenant não pode obter acesso | autenticado | users/roles | Auth / Login | login/logout sem segredo | Não homologado |
| Organização/Usuários | isolamento por `organization_id` | administração; plano ativo | organizations/users/roles | API/BFF/Web existentes | alteração sensível | Não homologado |
| Planos | backend aplica entitlement e retorna bloqueio amigável | Professional/Enterprise | plans/subscriptions/usage | API/BFF/Web | bloqueio e alteração | Não homologado |
| Diagnóstico | só publicado/coletando recebe resposta | permissão e limite | diagnostics/surveys | Diagnostics/PublicSurvey | publicação/fechamento | Não homologado |
| LGPD/Resposta | aceite e resposta transacionais; token não vaza | público validado | consents/responses | PublicSurvey | aceite/recebimento | Não homologado |
| Inteligência | evidência com origem; menos de 3 é insuficiente | avançado | evidence/metrics/indices/inferences | Intelligence | processamento | Não homologado |
| Action/Journey | Action exige origem; conclusão exige aprendizagem | avançado | valora_actions/journey_events | Intelligence | criação/conclusão | Não homologado |
| Reports/Certificates | nenhum PDF ou benchmark fictício | plano/permissão | executive_reports/certificates | API/BFF/Web | emissão/exportação | Não homologado |
| Notificações | tenant, `message`, destino real e leitura | autenticado | notifications | API/BFF/topbar | auditoria de acesso | Esquema localizado; não homologado |
| Integrações | hash apenas; conectividade real | Enterprise | api_keys/webhooks | Integrations | criação/revogação | Esquema localizado; não homologado |
