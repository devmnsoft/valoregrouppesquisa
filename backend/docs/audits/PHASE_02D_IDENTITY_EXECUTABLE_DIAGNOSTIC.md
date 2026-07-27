# Diagnóstico executável — Fase 2D

## Referência e ambiente

- SHA analisado no prompt: `5307f07143adcc9c08c31e6fcce85d9c462d8afc`.
- SHA inicial efetivo do checkout fornecido: `3ccfb8a6c2b64c5b33e144e37741b52340dd76df`.
- O checkout fornecido não possuía remoto Git nem o CLI `gh`; portanto os logs dos runs `30304796512` e `30304796150` não puderam ser obtidos.
- O SDK `dotnet` não está instalado na imagem (`bash: command not found: dotnet`). Restore, build, testes e format devem ser executados no runner .NET 10.

## Causa raiz observável no código

O schema canônico usava `deleted_at`, enquanto repositories antigos consultavam `is_deleted`, `role`, `plan_code`, `document` e colunas antigas de planos. Também havia caminhos de teste calculados a partir do diretório de execução e uma asserção do seed antigo com cinco perguntas.

## Matriz inicial do contrato

| Repository | Query/tabela | Colunas usadas anteriormente | Contrato existente/canônico | Correção desta entrega |
|---|---|---|---|---|
| OrganizationRepository | organizations | document, plan_code, is_deleted | public_name, slug, deleted_at; plano em subscriptions | repository tipado lista apenas campos canônicos e não consulta plano na organização |
| UserRepository | users | role, role_id, is_deleted, SELECT * | deleted_at e roles/user_roles | records tipados, projeções explícitas, deleted_at e vínculo exclusivamente por user_roles |
| PlanRepository | plans/limits/capabilities | status, preços e colunas agregadas | is_active, limit_key/value, capability_key | divergência registrada; refatoração permanece necessária |
| SubscriptionRepository | subscriptions | billing_status, expires_at, is_deleted | starts_at, ends_at, deleted_at | divergência registrada; refatoração permanece necessária |
| CommunicationRepository | communications/email_jobs | is_deleted, pending | deleted_at; queued/retrying | novo contrato de fila criado; adaptação do repository permanece necessária |
| AuditRepository | audit_logs | user_id/entity_type/message/is_deleted | actor_id/entity_name/deleted_at | divergência registrada; adaptação permanece necessária |

## Erros/gates

A inspeção local encontrou os desalinhamentos acima, `dynamic`, `SELECT *`, `tokenPreview`, sucesso simulado e fallback de e-mail não implementado. O validador da fase os transforma em falhas explícitas. Nenhum erro foi escondido por remoção de projeto, `continue-on-error` ou teste ignorado.
