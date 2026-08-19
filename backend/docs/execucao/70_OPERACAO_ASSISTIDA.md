# Operação assistida

A camada produtiva concentra os recursos operacionais em `AssistedOperationsController`, com escopo por organização, BFF autenticado e persistência Dapper exclusiva da Infrastructure. O dashboard agregado expõe contagens de organizações, onboarding, chamados, incidentes, feedbacks, upgrades, qualidade e última release sem carregar logs completos.

## Rotas entregues nesta consolidação

- `GET /api/v1/operations/dashboard` e `/bff/operations/dashboard`.
- CRUD operacional de backlog e rotas complementares de incidentes, releases, onboarding e Data Quality.
- As telas reutilizam o workspace premium responsivo e os estados de loading, vazio e erro já existentes.
