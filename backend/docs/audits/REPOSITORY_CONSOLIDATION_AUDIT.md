# Auditoria de saneamento e consolidação do repositório

- Data: 2026-07-24.
- Branch: `codex/sanear-repositorio-consolidar-dotnet`.
- SHA inicial encontrado: `617411015235552801508fa6d444d273e63b43fe`.

## Remoções

- Contaminação externa removida por `git revert --no-commit -m 1 617411015235552801508fa6d444d273e63b43fe`.
- Solution predecessora e seus projetos removidos com `git rm -r`.
- SQL completo duplicado da raiz e arquivo TODO de transformação removidos.

## Movimentos

- `global.json` movido para `backend/global.json`.
- `database/postgresql` movido para `backend/database/postgresql`.
- Documentos .NET movidos para `backend/docs/*` conforme categoria.
- Validadores de backend movidos para `backend/tools/validation`.

## Referências corrigidas

Foram removidas referências ao projeto predecessor, paths antigos de banco e scripts de backend no `package.json` da raiz. O comando canônico global é `npm run repository:boundaries`.

## Testes e limitações

Os resultados executados estão registrados na entrega da PR. Qualquer falha de SDK, Docker ou PostgreSQL descartável deve ser tratada como limitação ambiental, não como sucesso funcional.

## Pendências reais

Separação completa de todos os agregadores menores, PostgreSQL descartável real em CI e evolução funcional de autenticação/multiempresa pertencem a fases posteriores se não forem concluídas nesta PR.
