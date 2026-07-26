# Diagnóstico de saneamento e consolidação do repositório

- Data: 2026-07-24.
- Branch: `codex/sanear-repositorio-consolidar-dotnet`.
- SHA inicial encontrado: `617411015235552801508fa6d444d273e63b43fe`.

## Estrutura encontrada

Foram encontrados o legado JavaScript/Firebase na raiz, a solution oficial `backend/Valora.sln`, uma solution .NET predecessora fora da estrutura oficial, banco PostgreSQL canônico em `database/postgresql`, documentação .NET espalhada na raiz, validadores de backend em `tools/` e scripts de migração em `migration/`.

## Riscos

- Ambiguidade de build por múltiplas solutions.
- Divergência de schema por SQL fora do diretório oficial.
- Documentação contraditória na raiz.
- Validadores apontando para caminhos removidos.
- Agregadores C# ainda pendentes de separação fina.

## Plano de remoção

Reverter a contaminação externa, remover a solution predecessora, mover banco e documentos para `backend/`, limpar referências antigas e adicionar validador global de fronteiras.
