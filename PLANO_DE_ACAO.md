# Plano de Ação e Acompanhamento de Melhorias

O módulo **Plano de ação** fecha o ciclo consultivo do Valora Pulse: diagnóstico, resultado, recomendação, ação, acompanhamento e evolução.

## Modelo de dados

A implementação usa a coleção Firestore de topo **`actionPlans`**. A escolha mantém compatibilidade com o padrão atual do projeto (`surveys`, `responses`, `invitations`) e usa `companyId` obrigatório para segregação por empresa nas regras de segurança.

Campos principais: `companyId`, `surveyId`, `responseId`, `reportId`, `dimensionId`, `dimensionName`, `questionId`, `title`, `description`, `source`, `priority`, `status`, `responsibleUserId`, `responsibleName`, `department`, `dueDate`, `startDate`, `completedAt`, `progress`, `evidence`, `comments`, `createdAt`, `updatedAt`, `createdBy` e `updatedBy`.

## Perfis e permissões

- **Admin Valora**: visualiza e gerencia ações de todas as empresas.
- **Consultor Valora**: visualiza e sugere ações para clientes.
- **Empresa Admin**: cria, edita, conclui, exclui e acompanha ações da própria empresa.
- **Gestor de Pesquisa**: cria e edita ações da própria empresa, vinculadas a pesquisas e dimensões.
- **Analista de Resultados**: visualiza ações e relatórios, sem exclusão.
- **Gestor de Área**: visualiza e atualiza ações do próprio departamento.
- **Participante** e **Convidado Externo**: não acessam o módulo administrativo.

## Prioridades

- `low`: baixa
- `medium`: média
- `high`: alta
- `critical`: crítica

## Status

- `pending`: pendente
- `in_progress`: em andamento
- `waiting`: aguardando
- `completed`: concluída
- `cancelled`: cancelada
- `overdue`: vencida calculada quando o prazo passa sem conclusão

## Geração automática

A função `generateActionSuggestionsFromResults(metrics)` cria sugestões para dimensões abaixo de 2,5/5, baixa adesão, convites pendentes, comentários críticos e uso próximo ao limite do plano.

## Relatórios e dashboards

Relatórios executivos passam a carregar ações sugeridas e ações já criadas. Dashboards e a tela do módulo exibem KPIs de total, pendentes, em andamento, concluídas, vencidas, críticas, progresso médio e ações por dimensão.

## Limites por plano

O módulo `actionPlans` pode ser habilitado ou desabilitado por plano em `MODULE_DEFINITIONS`/módulos comerciais. O plano gratuito mantém o recurso habilitado para demonstração, com governança preparada para limite comercial futuro.
