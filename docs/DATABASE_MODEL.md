# Modelo de dados PostgreSQL

## Contrato canônico

O schema é `valorapesquisa`; `backend/database/postgresql/script_completo.sql` é o bootstrap/evolução canônico. O script usa `CREATE ... IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, blocos condicionais e correção de dados anterior à validação de constraints para suportar banco limpo e instalações parciais.

## Agregados

- **Tenant/acesso**: `organizations`, `users`, `roles`, `permissions`, vínculos e sessões.
- **Diagnóstico**: `forms`, versões, perguntas, opções, surveys, links, participantes, responses e answers.
- **Metodologia**: conceitos, relações, mappings pergunta-conceito/índice e snapshots versionados.
- **Inteligência**: evidence items/packs, runs/jobs, scores, índices, insights, heatmap, benchmark, evolution e journey.
- **Execução**: `valora_actions`, histórico e `one_on_one_sessions` com pautas/notas/vínculos.
- **Entregáveis**: resultados, relatórios, exports e certificados.
- **Plataforma**: planos, assinaturas, uso, notificações, API keys, webhooks, auditoria e governança.

## Invariantes protegidos

- `organization_id` delimita dados de tenant; consultas operacionais devem sempre recebê-lo.
- Pesos metodológicos são positivos; mappings antigos nulos/zero são reparados antes do `CHECK(weight > 0)`.
- Scores normalizados ficam em 0–100 e `score`/`max_score` devem representar a mesma escala declarada.
- `forms.title`, `notifications.message` e `api_keys.key_hash` são preenchidos antes de `NOT NULL` quando aplicável.
- `notifications.read_at`, `one_on_one_sessions.scheduled_at`, `platform_governance_events.deleted_at` e colunas de índices são garantidas antes de índices dependentes.
- Seeds usam chaves realmente únicas; permissões são reconciliadas com o catálogo canônico.
- `created_at`/`updated_at` registram ciclo de vida e `deleted_at` preserva histórico onde adotado.

## Dapper

SELECTs explicitam aliases PascalCase. Read models opcionais refletem nulabilidade SQL. Materializações sensíveis (autenticação e jobs) usam linhas internas com setters/aliases e conversão explícita para records, evitando dependência de construtor posicional implícito.
