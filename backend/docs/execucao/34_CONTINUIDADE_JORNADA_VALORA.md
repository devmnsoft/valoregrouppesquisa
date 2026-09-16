# Continuidade da jornada Valora

Atualizado em 16/09/2026. Este registro descreve somente o que foi observado no
checkout atual; documentos anteriores não foram tratados como prova de execução.

## Base técnica observada

- Solução ASP.NET Core em .NET 10 (`global.json` fixa o SDK 10.0.100), com
  projetos Domain, Application, Infrastructure, Api, Web MVC/BFF e Tests.
- O frontend da jornada autenticada é Razor MVC com JavaScript e CSS servidos por
  `Valora.Web`; a API e o BFF são processos ASP.NET separados.
- PostgreSQL/Dapper é a persistência canônica do backend. O schema consolidado
  está em `database/postgresql/script_completo.sql`, acompanhado por migrations
  incrementais e testes de integração condicionados por
  `VALORA_TEST_POSTGRES_CONNECTION`.
- O checkout iniciou limpo na branch `work`. Não há remoto Git configurado.

## Estado verificável da jornada prioritária

| Fluxo | Classificação neste ambiente | Evidência e limite |
|---|---|---|
| Login e organização ativa | Implementado parcialmente | Cookie/BFF, renovação e contexto organizacional existem; sem runtime autenticado não foi possível provar continuidade de sessão. |
| Criar e publicar formulário | Implementado parcialmente | Contratos, repository, Builder, revisão e publicação existem; execução SQL e renderização Razor ficaram bloqueadas. |
| Criar e abrir diagnóstico | Implementado parcialmente | A pesquisa persiste `form_version_id`, exige versão publicada e usa transições allowlist; corrida transacional não foi executada. |
| Participação pública | Implementado parcialmente | Validação server-side, versão fixa e transação de resposta existem; não houve submissão HTTP/PostgreSQL real. |
| Apuração e resultado | Implementado parcialmente | Resultado preserva resposta, diagnóstico e versão; reprocessamento versionado continua pendente. |
| Resultado para plano e acompanhamento | Implementado parcialmente | Criação idempotente vinculada ao resultado e Action Center existem; banco e navegador autenticado não foram executados. |

## Incremento deste checkout

- As views MVC de Indicators e Command Center deixaram de usar a variável local
  `page`, reservando `currentPage` para impedir ambiguidade com a diretiva Razor
  `@page`.
- Um contrato estático agora impede novas declarações locais `page` e diretivas
  `@page` dentro de `Views`, sem afetar diretivas legítimas que venham a existir
  em uma pasta `Pages`.

## Evidências e bloqueios

- `dotnet build Valora.sln --no-restore` não foi executável porque o contêiner não
  possui o comando `dotnet`; portanto, este incremento não declara compilação
  Razor nem testes xUnit aprovados.
- Não estão disponíveis `psql`, `VALORA_TEST_POSTGRES_CONNECTION`, aplicação
  iniciada ou sessão autenticada. Assim, isolamento entre duas organizações,
  concorrência, gravações e os cinco viewports solicitados permanecem não
  verificáveis neste ambiente.
- A inspeção estática dos riscos nomeados encontrou contratos de regressão
  para materialização de Forms/Branding, filtros opcionais e o parâmetro `wide`;
  isso não substitui a execução das consultas no PostgreSQL.

## Próximo incremento

Provisionar SDK .NET 10 e PostgreSQL descartável, executar a solução e os testes
marcados para PostgreSQL, e percorrer com duas organizações a sequência
Formulário → Diagnóstico → Resposta → Resultado → Plano. Somente depois
registrar capturas responsivas e promover os fluxos de "parcial" para
"implementado e verificado".
