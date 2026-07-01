# Sprint 03 — Diagnóstico inicial da estabilização .NET v2

## 1. Existência de auditoria final
No início desta execução, `SPRINT_03_DOTNET_V2_STABILIZATION_AUDIT.md` já existia no repositório e foi revisado para refletir a validação atual da sprint.

## 2. Build executa?
`dotnet build backend-v2/ValoraPesquisa.sln` foi executado em 2026-07-01, mas não iniciou porque o ambiente não possui o binário `dotnet` instalado (`dotnet: command not found`). Portanto, o build real permanece pendente para um ambiente com .NET SDK 8.

## 3. Testes executam?
`dotnet test backend-v2/ValoraPesquisa.sln` também não pode ser executado neste container pela ausência do .NET SDK. A impossibilidade é ambiental, não uma aprovação dos testes.

## 4. Erros de compilação existentes
Não foi possível coletar erros de compilação reais sem o SDK. Os riscos de compilação a validar em ambiente .NET 8 são:
- compatibilidade de nomes/aliases SQL com records posicionais;
- DI dos services/use cases adicionados;
- referências entre projetos após separação de arquivos;
- serialização/desserialização dos DTOs usados pela API e Web.

## 5. Testes falhando
Não há lista de testes falhando porque a suíte xUnit não executou sem `dotnet`. A suíte foi mantida/ampliada para cobrir cálculo, segurança textual, SQL, hashes e estrutura da sprint.

## 6. Controllers ainda concentrados em `FoundationControllers.cs`
Nenhum controller funcional permanece concentrado em `FoundationControllers.cs`. O arquivo existe apenas como marcador informando que os controllers foram separados em `ApiBase.cs`, `OrganizationsController.cs`, `UsersController.cs`, `FormsController.cs`, `SurveysController.cs`, `SurveyLinksController.cs`, `PublicSurveysController.cs`, `PublicResultsController.cs`, `ResponsesController.cs` e `AuditController.cs`.

## 7. Repositories ainda concentrados em `DapperRepositories.cs`
Nenhum repository funcional permanece concentrado em `DapperRepositories.cs`. O arquivo existe apenas como marcador informando que os repositories foram separados em `Scope.cs`, `AuditService.cs`, `OrganizationRepository.cs`, `UserRepository.cs`, `FormRepository.cs`, `SurveyRepository.cs`, `SurveyLinkRepository.cs` e `ResponseRepository.cs`.

## 8. DTOs ainda concentrados em `FoundationDtos.cs`
Nenhum DTO funcional permanece centralizado em `FoundationDtos.cs`. O arquivo preserva o namespace/compatibilidade e os DTOs foram organizados em subpastas de organizações, usuários, formulários, pesquisas, públicos, respostas, resultados e auditoria.

## 9. Telas Web ainda apenas listagem simples
As telas administrativas foram evoluídas para cards/formulários/ações via MVC/Razor + jQuery. As áreas que permanecem deliberadamente mínimas, porém funcionais, são respostas e auditoria, com foco em listagem segura, detalhe/filtro simples e não exposição de tokens/hashes.

## 10. Fluxos Web ainda sem criação/edição
Os fluxos principais possuem criação/edição/status/link mínimo via UI. Melhorias pendentes para sprints futuras: UX avançada de edição de perguntas/opções, validações ricas client-side, paginação e filtros avançados.

## 11. Endpoints ainda sem teste integrado
Existe esqueleto de teste integrado para o fluxo vertical, mas ele está marcado como `Skip` por depender de PostgreSQL/API reais. Os endpoints que ainda exigem execução integrada ativa no CI são: organizações, usuários, login, formulários, pesquisas, links públicos, validação pública, envio de resposta, resultado público e auditoria.

## 12. Riscos em Dapper + records
Records posicionais exigem correspondência entre aliases SQL e nomes de parâmetros do construtor. Colunas `snake_case` devem ser projetadas com alias compatível; listas aninhadas devem ser carregadas em consultas separadas; gravações compostas devem usar transação; hashes devem ficar restritos a entidades internas/repositories e nunca retornar nos DTOs públicos.

## 13. Plano objetivo de correção
1. Rodar build/test em ambiente com .NET SDK 8 e corrigir eventuais erros reais.
2. Ativar teste integrado em CI com PostgreSQL descartável e API/Web em execução.
3. Continuar reduzindo lógica de controllers para services/use cases.
4. Ampliar testes integrados para isolamento multiempresa e auditoria sanitizada.
5. Evoluir a UX MVC sem introduzir SPA nem dependências fora do escopo da fundação.
