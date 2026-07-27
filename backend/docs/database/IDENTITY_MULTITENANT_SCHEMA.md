# Schema de identidade multiempresa

A migration `20260727_003_identity_multitenant_core.sql` acrescenta contadores e reservas de uso, escopos hierárquicos e histórico de assinatura. FKs preservam referências, checks protegem quantidades/status e chaves únicas fornecem idempotência. `banco_completo.sql` contém o mesmo contrato para bootstrap vazio. A migration é aditiva e transacional; sua aplicação deve ser validada duas vezes em PostgreSQL 16 antes de produção.
