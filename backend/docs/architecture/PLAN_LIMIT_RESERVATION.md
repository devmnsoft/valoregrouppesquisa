# Reserva transacional de limites

Uma operação limitada abre transação, bloqueia `plan_usage_counters` com `SELECT ... FOR UPDATE`, valida assinatura/capability/limite e cria uma reserva idempotente. Após a operação, confirma a reserva e transfere `reserved` para `consumed`; falhas liberam a reserva. Reservas expiradas são compensadas por job idempotente. Nunca executar `COUNT`, comparar e inserir fora da mesma transação.
