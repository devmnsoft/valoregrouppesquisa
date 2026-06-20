# Central de Notificações, Alertas Inteligentes e Lembretes

## Modelo e coleção Firebase

A implementação usa a coleção raiz `notifications`. A escolha facilita consultas por usuário, por empresa e globais sem duplicar subcoleções. Todo documento possui `companyId`, `audience`, `role`, `userId` e `department` para segregação multiempresa e por perfil.

Campos principais: `id`, `companyId`, `userId`, `role`, `audience`, `type`, `severity`, `title`, `message`, `entity`, `entityId`, `actionLabel`, `actionRoute`, `read`, `readAt`, `dismissed`, `dismissedAt`, `emailSent`, `emailSentAt`, `createdAt`, `updatedAt`, `expiresAt`.

## Tipos implementados

- `survey_expiring`, `survey_expired`
- `invitation_pending`, `invitation_failed`
- `new_response`, `low_response_rate`
- `action_overdue`, `action_critical`
- `plan_limit_warning`, `plan_limit_exceeded`
- `onboarding_stalled`, `report_ready`, `system_error`

Severidades: `info`, `success`, `warning`, `danger`, `critical`.

## Regras de geração

O arquivo `notification-service.js` gera alertas a partir do estado atual:

- pesquisa ativa vencendo em até 3 dias;
- pesquisa expirada com convites pendentes;
- convite enviado/pendente há mais de 3 dias;
- falha de convite;
- baixa taxa de resposta abaixo de 40%;
- novas respostas recentes;
- ações vencidas e críticas;
- uso do plano acima de 80% e acima de 100%;
- empresa sem administrador, sem funcionários ou sem pesquisa ativa;
- relatório disponível quando existem respostas.

No modo local/demo, a geração ocorre durante normalização/salvamento do estado e persiste no `localStorage`. No Firebase, o frontend lê/grava documentos em `notifications`; em produção, a criação automática deve ser feita por Cloud Functions/Admin SDK para evitar escrita privilegiada no navegador.

## Permissões por perfil

- `admin_valora`: alertas globais, limites, onboarding e falhas.
- `consultor_valora`: clientes travados, baixa adesão, ações vencidas/críticas e relatórios.
- `empresa_admin` e `gestor_pesquisa`: alertas operacionais da própria empresa.
- `analista_resultados`: respostas, relatórios, baixa adesão e dimensões/ações críticas analisáveis.
- `gestor_area`: somente alertas da própria área quando houver `department`.
- `participante`: apenas pesquisas/resultados direcionados; não vê alertas administrativos.
- `convidado_externo`: sem central completa.

## E-mail e lembretes

A estrutura já possui `emailSent` e `emailSentAt`. Lembretes automáticos devem respeitar plano, `receivesEmail=false`, e-mail válido, finalidade LGPD, deduplicação e auditoria.

Funções Firebase planejadas: `scheduledGenerateNotifications`, `scheduledSendPendingReminders`, `scheduledExpireInvitations`, `scheduledMarkOverdueActions`. A implementação segura deve usar Admin SDK e provider de e-mail configurado no backend.

## Auditoria

A interface registra em logs: notificação lida, dispensada e marcação em lote. Cloud Functions devem registrar criação de notificação, lembrete enviado, falha de e-mail, alerta crítico gerado e alerta resolvido.
