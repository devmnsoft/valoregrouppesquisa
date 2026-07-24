# SuperAdmin Real Flows
Todas as ações sensíveis devem exigir role `SuperAdmin`, antiforgery, motivo obrigatório, alteração transacional no banco, `superadmin_audit_logs` e mensagem amigável. Pagamentos aprovados atualizam invoice, cliente, entitlement event e comunicação; inadimplência bloqueia apenas benefícios pagos, nunca login ou dados.
