# Vertical transacional de identidade

## Limite transacional

O cadastro empresarial deve abrir uma conexão e uma transação, reservar a chave de idempotência e criar organização, pessoa jurídica, endereço, unidade principal, administrador, `user_roles`, escopo, assinatura gratuita, módulos, settings, branding, onboarding, consentimentos, auditoria e outbox. A resposta e qualquer token somente podem ser emitidos após `COMMIT`; exceções executam `ROLLBACK`.

## Contratos de segurança

- `organizations` não referencia plano; `subscriptions` é a fonte oficial.
- Exclusão lógica usa somente `deleted_at`.
- Login por e-mail depende do índice parcial global `lower(email)`.
- Papéis são obtidos por `user_roles`; nunca por campo enviado pelo cliente.
- Refresh token bruto existe apenas na memória/resposta, é armazenado como hash e rotacionado uma vez.
- Reuso revoga família e sessão e gera auditoria de segurança.
- Recuperação persiste somente hash; o link bruto é material transitório do job protegido.
- Outbox e jobs usam chaves de idempotência diferentes da credencial.

## Dispatch

O consumidor seleciona `queued`/`retrying` cujo `next_attempt_at <= now()`, marca `processing`, envia por SMTP e finaliza `sent`. Falhas sanitizadas recebem backoff até `max_attempts`; depois tornam-se `dead_letter`.
