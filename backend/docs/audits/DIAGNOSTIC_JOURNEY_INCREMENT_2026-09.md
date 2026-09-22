# Incremento integrado da jornada diagnóstica — setembro de 2026

## Estado reconfirmado

O trabalho partiu do commit `cac78b5632ab43bca6fe2a944b9fb5444db4d692`, sem alterações locais. O contrato continua em .NET 10 e o bootstrap canônico permanece `database/postgresql/script_completo.sql`.

O checkout não contém arquivos PDF (`find . -type f -iname '*.pdf'` não retornou itens), portanto os dois documentos metodológicos solicitados não puderam ser lidos e nenhuma regra foi inventada em substituição. O ambiente também não contém o executável `dotnet`; restauração, build, testes .NET e inicialização ficaram bloqueados. Este registro não declara homologação.

## Rotas e serviços canônicos afetados

| Jornada | Rota efetiva | Serviço/repositório | Decisão |
|---|---|---|---|
| Dashboard/leitura | `GET /api/organizational-intelligence/dashboard` | `OrganizationalIntelligenceService` / `OrganizationalIntelligenceRepository` | O servidor devolve a classificação profissional; JavaScript somente apresenta. |
| Evolução | `GET /api/organizational-intelligence/evolution` | `OrganizationalIntelligenceService.EvolutionAsync` | Continua sem previsão futura. Comparabilidade completa permanece pendente. |
| Processamento | worker de `intelligence_processing_jobs` | `IntelligenceProcessingOrchestrator` / `OrganizationalIntelligencePipeline` | Job identifica operação lógica; `attemptId` identifica tentativa. |
| Atalho de relatório | `POST /api/diagnostics/{id}/report/preview` e `/generate` | `DiagnosticWorkspaceService` / `DiagnosticWorkspaceRepository` | Não fabrica relatório nem marca o ciclo como pronto; orienta a Central de Entregas. |
| Entregável oficial | `/Reports/Prepare` e API de formal deliverables | `ExecutiveDeliveryService` | Preparação idempotente, artefato, revisão e publicação são o fluxo canônico. |
| One-on-One | `/OneOnOne/...` | `OneOnOneSessionService` / `OneOnOneRepository` | Leitura exige participação ou permissão; nota privada mantém permissão própria. |

## Causas corrigidas

- Faixas próprias no cliente divergiam da metodologia e pontos eram exibidos como percentual.
- Retry recebia outro `PipelineRunId`; agora a operação usa o ID estável do job e cada tentativa tem ID separado.
- Métricas e índices gravavam todos os IDs do lote; agora cada grupo agrega apenas suas evidências e um hash dos valores, pesos, polaridade e mapeamento relevantes.
- Mapeamentos globais e organizacionais podiam multiplicar linhas; a seleção agora é cardinalidade um, com precedência organizacional.
- Projeções usavam tamanho bruto para suficiência; agora dependem de resultados agrupados elegíveis.
- O workspace criava metadado de relatório sem artefato e promovia o ciclo para pronto; esse caminho foi eliminado.
- One-on-One listava sessões de toda a organização; agora aplica vínculo de participante e permissões específicas, separando notas privadas.

## Regras preservadas

`null` continua não avaliado, zero medido continua zero, projeções permanecem isoladas por organização/pesquisa/execução, previsão futura não foi reintroduzida, publicação continua uma decisão humana e notas privadas não entram nas projeções nem nas sugestões de IA.

## Backlog e dependências

- Executar instalação limpa, reaplicação e atualização representativa em PostgreSQL descartável.
- Integrar `EvolutionComparisonService` com assinaturas persistidas de metodologia, dimensões, escala, critérios e população.
- Completar paginação e revisão metodológica da Central de Evidências.
- Completar estados editoriais dos insights e conversão idempotente ao Action Center.
- Expandir o workspace para as oito etapas e consolidar alertas operacionais.
- Executar smoke autenticado e validar 1440, 768 e 390 px quando o runtime estiver disponível.
- Ler os dois PDFs assim que forem incluídos no checkout.
