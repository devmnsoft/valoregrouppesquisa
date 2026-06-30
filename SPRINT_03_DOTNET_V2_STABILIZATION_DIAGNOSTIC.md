# Sprint 03 — Diagnóstico inicial da estabilização .NET v2

## 1. Build
`dotnet build backend-v2/ValoraPesquisa.sln` não executou neste container porque o binário `dotnet` não está instalado (`dotnet: command not found`).

## 2. Testes
`dotnet test backend-v2/ValoraPesquisa.sln` não executou pelo mesmo motivo: ausência do .NET SDK no ambiente.

## 3. Erros de compilação conhecidos
Não foi possível obter erros reais de compilação no container. O risco principal era concentração de tipos em arquivos únicos e possíveis falhas de mapeamento Dapper/records em runtime.

## 4. Testes falhando
Não houve execução real por falta do SDK. A suíte existente era majoritariamente textual/unitária e foi ampliada.

## 5. Controllers concentrados
Antes da correção, `FoundationControllers.cs` concentrava organizações, usuários, formulários, pesquisas, links, público, resultados, respostas e auditoria.

## 6. Repositories concentrados
Antes da correção, `DapperRepositories.cs` concentrava `Scope`, `AuditService`, `OrganizationRepository`, `UserRepository`, `FormRepository`, `SurveyRepository`, `SurveyLinkRepository` e `ResponseRepository`.

## 7. DTOs a separar
`FoundationDtos.cs` concentrava DTOs de organizações, usuários, formulários, pesquisas, links, público, respostas, resultados e auditoria.

## 8. Telas Web MVC simples
As views MVC possuíam cards genéricos com listagem alimentada pelo `site.js`, mas sem CRUD visual mínimo para criação/edição/status/link.

## 9. Fluxos sem formulário visual
Organizações, usuários, formulários, pesquisas e links precisavam de formulário visual. Respostas e auditoria precisavam de detalhe/filtro simples. Pesquisa pública precisava renderizar tipos de pergunta.

## 10. Riscos Dapper/Npgsql com records
Records posicionais exigem compatibilidade entre aliases SQL e nomes dos parâmetros. Campos snake_case precisam de aliases explícitos; entidades com listas aninhadas devem ser carregadas por consultas separadas; hashes devem ficar restritos às entidades/repositories.

## 11. Endpoints sem testes integrados reais
Fluxo completo de organização, usuário, login, formulário, pesquisa, publicação, link, validação pública, resposta, resultado e auditoria precisava de teste integrado contra PostgreSQL.

## 12. Plano da sprint
1. Separar controllers e repositories por responsabilidade.
2. Organizar DTOs em pastas por módulo mantendo namespace.
3. Criar services/use cases mínimos.
4. Ampliar testes unitários, textuais e SQL.
5. Criar esqueleto de teste integrado PostgreSQL.
6. Criar Docker Compose, Dockerfiles e scripts locais.
7. Melhorar Web MVC com CRUD mínimo via Razor/jQuery.
8. Ajustar seed SQL local.
9. Criar README e validador automatizado.
