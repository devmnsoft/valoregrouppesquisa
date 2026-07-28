# Diagnóstico da fase 02G.1

Baseline: `73381f6a4a8f2658dab1eb0fef18552903e27064`. O clone fornecido não contém remoto nem branch `main`; por isso a branch de entrega foi criada diretamente desse SHA confirmado.

Os contratos estáticos falhavam principalmente porque reconstruíam caminhos a partir do diretório de saída e ainda buscavam `OperationalServices.cs`. A regra preservada é verificar o backend oficial; os testes passam a usar `RepositoryPaths` e `OperationalFeatureServices.cs`. O bootstrap também continha duas declarações incompatíveis das tabelas de consumo.

A imagem de execução não possui o SDK .NET nem PostgreSQL. Assim, resultados de build, testes e homologação de banco precisam ser produzidos pelo workflow, e não são declarados como executados localmente.
