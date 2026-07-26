# Relatório de aposentadoria do projeto .NET predecessor

| Componente predecessor | Correspondente em backend | Portado | Excluído | Evidência |
| --------------------- | ------------------------- | ------: | -------: | --------- |
| Solution paralela | `backend/Valora.sln` | Não | Sim | Única solution validada por `find . -name "*.sln"`. |
| Projetos Api/Application/Domain/Infrastructure/Web/Tests | `backend/Valora.*` | Conceitos já existentes | Sim | Projetos oficiais permanecem sob `backend/`. |
| Controllers públicos e administrativos | `Valora.Api` e `Valora.Web` | Conceitual | Sim | APIs e telas oficiais já existem na solution oficial. |
| Repositories Dapper | `Valora.Infrastructure` | Conceitual | Sim | Persistência oficial usa PostgreSQL/Dapper na solution oficial. |
| Scripts PostgreSQL | `backend/database/postgresql/banco_completo.sql` | Consolidado | Sim | Banco canônico movido para `backend/database/postgresql`. |
| Documentos históricos | `backend/docs/audits` e `backend/docs/archive` | Classificado | Sim | Documentação útil arquivada; paths executáveis removidos. |
| Validadores próprios | `backend/tools/validation` e `scripts/repository/validate-project-boundaries.js` | Parcial | Sim | Validador global substitui referências ao projeto removido. |
