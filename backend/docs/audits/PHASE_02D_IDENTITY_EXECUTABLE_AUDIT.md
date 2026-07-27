# Auditoria da vertical de identidade — Fase 2D

## Evidências entregues

- Migration aditiva e transacional `20260729_004_transactional_identity_auth.sql`.
- Bootstrap canônico contém o mesmo conjunto de colunas, tabelas e índices da migration.
- E-mail ativo possui unicidade global case-insensitive por índice parcial.
- Sessões, famílias e refresh tokens possuem relações explícitas; o contrato armazena somente `token_hash`.
- Jobs de e-mail possuem estados finitos, tentativas, backoff agendável e idempotência.
- `RepositoryPaths` elimina dependência do diretório corrente nos testes corrigidos.
- O teste do questionário confirma as 25 perguntas oficiais.
- O validador estático falha diante das violações de segurança ainda existentes.

## Resultado honesto

Esta alteração estabelece o contrato de dados e os gates de regressão, mas **não declara a vertical completa**. A imagem não oferece .NET 10/PostgreSQL e o código de referência ainda contém repositories dinâmicos e fluxos simulados que o novo validador reprova. Build, autenticação rotativa, BFF e homologação real continuam bloqueadores antes do merge.
