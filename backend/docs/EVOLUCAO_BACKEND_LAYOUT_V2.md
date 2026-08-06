# Evolução Backend e Layout V2 — auditoria e implementação

Data da auditoria: 2026-08-05. Esta leitura considera o código versionado, sem inferir que infraestrutura externa esteja configurada.

## Mapa técnico rápido

| Área | Situação encontrada | Direção adotada nesta entrega |
|---|---|---|
| `functions/index.js` | Backend legado Firebase grande e ativo; mantém pesquisa pública, resultado tokenizado, e-mail, usuários, clientes, planos/pagamentos e suporte. Muitos nomes históricos foram substituídos por operações equivalentes, não por aliases. | Preservado integralmente; nenhuma Function foi removida ou teve contrato alterado. |
| `Valora.Api` | API REST/BFF já cobre autenticação local, planos, organização, pesquisas, respostas, relatórios, certificados, exportações, LGPD, e-mail e operação. A estrutura organizacional estava apenas autenticada, sem autorização granular, e convertia claim inválida com `Guid.Parse`. | Políticas por operação, claim segura, escopo da empresa e erro claro. |
| `Valora.Application` | Serviços reais de entitlement, cálculo, relatório, certificado, e-mail, auditoria, LGPD e unidades/setores. | Unidades/setores agora validam vínculo multiempresa e auditam mutações. |
| `Valora.Domain` | Modelo amplo: organização/grupo/pessoa jurídica/unidade/setor, plano, pesquisa, resposta, certificado, comunicação, consentimento e auditoria. | Sem duplicar entidades existentes. |
| `Valora.Infrastructure` | Repositórios PostgreSQL/Dapper e scripts canônicos; consultas de estrutura já filtram `organization_id`. | Reutilizado; validação de unidade pai fecha a lacuna de escrita entre empresas. |
| `Valora.Web` | MVC SaaS com BFF, design system, shell responsivo, dashboard, pesquisa pública, resultado, certificados e módulos administrativos. | Tela Organização ganhou filtros reais por status/unidade e vínculo setor→unidade, conectados ao BFF/API. |
| `Valora.Tests` | Suíte xUnit extensa para autenticação, entitlement, resultados, certificados, e-mail, LGPD, segurança e contratos REST. | Contrato de estrutura ampliado para autorização, isolamento, auditoria e UI filtrável. |
| `src` | Não existe neste checkout; o frontend oficial está em `backend/Valora.Web`, e o legado está nos assets raiz (`app.js`, `styles.css`, HTML público). | Nenhuma árvore artificial criada. |
| `public` | Assets/HTML do frontend Firebase legado continuam sendo publicados no fluxo antigo. | Preservado para compatibilidade. |

## Matriz de paridade Firebase → .NET

Legenda: **Sim** = implementação direta; **Eq.** = capacidade equivalente com outro nome/rota; **Parcial** = parte do fluxo; **Não** = ausente com esse contrato. “Repo” indica persistência real disponível na Infrastructure. A coluna Web inclui MVC e frontend legado.

| Operação solicitada | Functions | API .NET | Application | Repo | Web/React | Teste | Pendência real para produção |
|---|---:|---:|---:|---:|---:|---:|---|
| `getPlanCatalog` | Eq. catálogo/checkout | Sim `PlansController` | Sim | Sim | Sim | Sim | Homologar catálogo e preços no provedor. |
| `getCompanyUsage` | Eq. planos/uso | Sim | Sim `UsageService` | Sim | Sim | Sim | Alertas operacionais de limite. |
| `updateSubscriptionPlan` | Eq. upgrade/downgrade | Sim | Sim | Sim | Sim | Sim | Webhooks/retentativas em produção. |
| `createUserByAdmin` | Eq. `createUser` | Sim | Sim | Sim | Sim | Sim | Homologar claims e e-mail transacional. |
| `createClientByMember` | Eq. `createClient` | Parcial (participantes) | Parcial | Sim | Sim | Sim | Formalizar alias de migração se consumidor externo exigir o nome antigo. |
| `updateClientByMember` | Eq. `updateClient` | Parcial | Parcial | Sim | Sim | Sim | Mesma pendência de compatibilidade nominal. |
| `submitSurveyResponse` | Sim | Sim | Sim transacional | Sim | Sim | Sim | Teste de carga/concorrência em homologação. |
| `getSurveyResult` | Eq. `getPublicResult` | Sim | Sim | Sim | Sim | Sim | Monitorar rotação/expiração de token. |
| `getCompanyAnalysis` | Parcial | Sim dashboard/resultados | Sim | Sim | Sim | Sim | Expandir séries comparativas por período. |
| `getCompanySettings` | Parcial | Sim | Sim | Sim | Sim | Sim | Validar migração de chaves legadas. |
| `updateCompanySettings` | Parcial | Sim | Sim | Sim | Sim | Sim | Validar migração de chaves legadas. |
| `updateMemberAdminStatus` | Eq. perfil/roles | Sim | Sim | Sim | Sim | Sim | Sincronizar custom claims no modo híbrido. |
| `disableUser` | Parcial | Sim | Sim | Sim | Sim | Sim | Homologar revogação de sessões Firebase. |
| `reactivateUser` | Parcial | Sim | Sim | Sim | Sim | Sim | Homologar revogação de sessões Firebase. |
| `deleteUserCompletely` | Não (há fluxos de limpeza) | Parcial (LGPD/soft delete) | Sim LGPD | Sim | Sim | Sim | Exclusão física requer política de retenção aprovada. |
| `inviteMember` | Eq. `sendUserInvite` | Sim | Sim | Sim | Sim | Sim | Entregabilidade SMTP e expiração agendada. |
| `acceptInvite` | Parcial | Sim | Sim | Sim | Sim | Sim | E2E com Firebase real. |
| `resendInvite` | Eq. retry/send | Sim | Sim | Sim | Sim | Sim | Métricas de bounce. |
| `cancelInvite` | Parcial | Sim | Sim | Sim | Sim | Sim | E2E de concorrência aceitar/cancelar. |
| `removeMemberFromCompany` | Parcial | Sim | Sim | Sim | Sim | Sim | Sincronizar claims híbridas. |
| `changeMemberRole` | Eq. perfil | Sim | Sim | Sim | Sim | Sim | Sincronizar claims híbridas. |
| `exportCompanyData` | Eq. `exportData` | Sim | Sim | Sim | Sim | Sim | Worker/armazenamento para arquivos grandes. |
| `generateCertificate` | Parcial | Sim | Sim | Sim | Sim | Sim | Renderizador PDF e chave de assinatura em secret store. |
| `downloadCertificate` | Parcial | Sim | Sim | Sim | Sim | Sim | Homologar storage/CDN e token temporário. |
| `generateReportPdf` | Parcial/cliente legado | Sim | Sim | Sim | Sim | Sim | Renderização PDF assíncrona para alto volume. |
| `submitFeedback` | Eq. suporte público | Parcial (comunicações/suporte) | Parcial | Parcial | Parcial | Parcial | Consolidar entidade/painel de feedback na API oficial. |
| `adminListFeedbacks` | Eq. suporte | Parcial | Parcial | Parcial | Parcial | Parcial | Filtros de prioridade/status e resposta interna oficiais. |
| `migrateUserToCompanyMember` | Eq. repair/migration | Sim `MigrationController` | Sim | Sim | Sim | Sim | Executar dry-run e reconciliação por lote antes do cutover. |

## Implementação V2 concluída

1. **Autorização granular:** endpoints de unidades e setores exigem permissões `read/create/update/disable` já existentes no banco, agora também registradas no catálogo que gera as políticas da API.
2. **Isolamento entre empresas:** setor só aceita `unitId` que possa ser lido dentro do `organization_id` autenticado. Uma referência de outra empresa é rejeitada com mensagem segura.
3. **Sessão segura:** claim ausente/inválida não vira erro genérico de parsing; resulta em 401 rastreável pelo middleware/correlation id.
4. **Auditoria:** criação, edição, desativação e reativação de unidade/setor geram eventos sem conteúdo sensível.
5. **Fluxo real no Web:** filtros por status e unidade chegam ao BFF e à API; ao criar/editar setor o usuário escolhe a unidade, em desktop e mobile.

## Riscos e próximos passos honestos

- A validação Firebase JWT e a autenticação própria coexistem; a troca definitiva de emissor deve ser feita com testes de tokens reais e sem remover o modo legado.
- Feedback ainda não tem paridade nominal completa na API oficial; suporte/comunicações cobrem parte do caso, mas merecem módulo consolidado.
- PDF, storage temporário e filas precisam de homologação com serviços externos; credenciais não devem entrar no repositório.
- Comparativos avançados por unidade/setor e período ainda precisam de séries históricas e índices aferidos com volume de produção.
