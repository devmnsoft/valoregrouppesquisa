# Auditoria — Fase 1 ASP.NET Core 10 e Banco Canônico

## Corrigido nesta fase

- Criada fundação de build centralizada com `global.json`, `backend/Directory.Build.props`, `backend/Directory.Packages.props` e `.editorconfig`.
- Atualizados os projetos oficiais para `net10.0` e Central Package Management.
- Entidades estruturais mínimas do domínio foram expandidas com identidade, tenant, status, timestamps e exclusão lógica por meio de `AuditableEntity` quando aplicável.
- Criado `backend/database/postgresql/banco_completo.sql` como bootstrap canônico não destrutivo e idempotente.
- Preservados scripts históricos com aviso apontando para o bootstrap canônico.
- Criados testes estáticos de arquitetura e inspeção do script canônico.
- Criado validador consolidado `npm run backend:phase1-validate`.

## Reaproveitado

- Solution oficial `backend/Valora.sln`.
- Projetos oficiais existentes.
- Script histórico `backend/database/postgresql/banco_completo.sql` como referência de estruturas e compatibilidade.
- Validadores Node já existentes relacionados ao backend oficial.

## Não concluído nesta fase

- A execução real de `dotnet restore`, `dotnet build`, `dotnet test` e `dotnet format` não pôde ser validada neste container porque `dotnet` não está instalado.
- Testcontainers/PostgreSQL não foi executado localmente pelo mesmo bloqueio de SDK e pela necessidade de runtime Docker disponível.
- A separação total de todos os agregadores antigos permanece como trabalho incremental das próximas fases; esta fase removeu a compressão das entidades estruturais alteradas e criou testes para impedir regressão.

## Riscos

- Pacotes .NET 10 foram definidos conforme versões estáveis esperadas, mas devem ser restaurados em ambiente com SDK .NET 10 para confirmar disponibilidade no feed NuGet no momento da homologação.
- O bootstrap canônico é amplo e idempotente, mas deve ser executado em banco descartável antes de homologação com dados reais.

## Próximos passos

1. Instalar SDK .NET 10 no agente de CI.
2. Executar `npm run backend:phase1-validate`.
3. Executar o script canônico duas vezes em PostgreSQL descartável.
4. Substituir gradualmente agregadores operacionais remanescentes por arquivos por responsabilidade.
5. Evoluir migrations versionadas incrementais após o bootstrap canônico.
