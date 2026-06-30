# Sprint 03 — Auditoria final da estabilização .NET v2

## 1. Resumo
A Sprint 03 estabilizou a fundação `backend-v2` sem criar módulos comerciais novos, priorizando organização, testes, Docker, scripts, Web MVC mínimo, seed local, documentação e validação automatizada.

## 2. Diagnóstico inicial
O diagnóstico foi registrado em `SPRINT_03_DOTNET_V2_STABILIZATION_DIAGNOSTIC.md`.

## 3. Build
`dotnet build backend-v2/ValoraPesquisa.sln` foi tentado, mas o container não possui .NET SDK (`dotnet: command not found`). Scripts e README documentam execução em ambiente com .NET SDK 8.

## 4. Testes
`dotnet test backend-v2/ValoraPesquisa.sln` foi tentado, mas não pôde rodar pela ausência do SDK. O validador Node foi executado com sucesso.

## 5. Controllers separados
Controllers administrativos e públicos foram separados em arquivos próprios, mantendo `FoundationControllers.cs` apenas como marcador de compatibilidade da sprint.

## 6. Repositories separados
Repositories Dapper foram separados por responsabilidade, mantendo `DapperRepositories.cs` apenas como marcador de compatibilidade.

## 7. DTOs organizados
DTOs foram movidos para subpastas por módulo sob `Application/DTOs`, preservando o namespace `ValoraPesquisa.Application.DTOs`.

## 8. Services criados
Foram adicionados use cases mínimos para organizações, usuários, formulários, pesquisas, links, validação pública, resposta pública, resultado público e auditoria.

## 9. Web MVC melhorado
`site.js` passou a oferecer CRUD mínimo, status, link público, filtro de auditoria, detalhe de resposta, renderização por tipos de pergunta e resultado público sem JSON bruto/hashes.

## 10. Docker Compose
Foi criado `backend-v2/docker-compose.yml` com PostgreSQL, API e Web, além de Dockerfiles para API e Web.

## 11. Scripts Windows/Linux
Foram criados scripts para subir PostgreSQL, aplicar banco, rodar API, rodar Web e validar build/test/validator.

## 12. README
Foi criado `backend-v2/README.md` com pré-requisitos, execução local, Docker Compose, credenciais demo e troubleshooting.

## 13. Seed local
`scriptbd_completo.sql` recebeu seed idempotente para organização Valora, empresa demo, usuários demo, formulário demo e pesquisa demo publicada. Hashes são apenas de desenvolvimento e documentados como não produtivos.

## 14. Testes unitários
A suíte foi ampliada com testes de segurança textual, token/BCrypt, SQL, seed e presença de arquivos separados.

## 15. Testes integrados
Foi criado `PostgresIntegrationTests.cs` com estrutura xUnit para execução contra PostgreSQL/API reais. O teste fica marcado como Skip no ambiente padrão porque exige serviços externos.

## 16. Validador
Foi criado `backend-v2/tools/validate-backend-v2-foundation.js` e o script raiz `backend-v2:validate`.

## 17. Gaps restantes
Build/test .NET reais ainda precisam rodar em ambiente com SDK 8. O teste integrado deve ser ativado no CI com PostgreSQL/API disponíveis. Os services criados são mínimos e podem absorver mais lógica dos controllers em sprints futuras.

## 18. Riscos
Há risco residual de ajustes de compilação por falta de SDK no container. Hashes BCrypt de seed devem ser substituídos por hashes válidos/gerados no ambiente real caso se deseje login demo operacional imediatamente. Dapper com records deve ser validado em runtime PostgreSQL.

## 19. Próximo passo recomendado
Instalar .NET SDK 8 no CI/agente, executar build/test, ativar teste integrado com PostgreSQL descartável e só então iniciar a Sprint 04 de planos, módulos, limites comerciais e permissões avançadas.
