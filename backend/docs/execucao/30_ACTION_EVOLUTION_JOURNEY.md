# Action, Evolution e Journey

O Action canônico preserva origem em Insight/Inferência e exige evidência, capacidade, indicador, resultado e critério. Os estados de conclusão, cancelamento e replanejamento exigem, respectivamente, aprendizagem ou justificativa. A rota `POST action-plans/{id}/replan` foi exposta pela API e pelo BFF genérico.

O ciclo Evolution separa acompanhamento operacional de reavaliação quantitativa. Acompanhamentos não calculam delta. Reavaliações preservam duas medições e só mostram variação em pontos quando as bases são conhecidas e comparáveis. Journey mantém eventos no escopo da organização, pesquisa e ciclo diagnóstico.

## Referências e histórico

Incremento iniciado em `04004362`. Foram reutilizados o agregador de inteligência, `EvolutionComparisonService`, as tabelas `evolution_measurements` e `evolution_snapshots`, as transações Dapper, a autorização comercial e os componentes Razor.

| Entidade | Referência | Uso |
| --- | --- | --- |
| Pesquisa | `surveys.id` / `SourceSurveyId` | Seleção explícita da coleta de origem |
| Ciclo diagnóstico | `diagnostic_cycles.id` / `cycle_id` | Contexto comprovado pela relação `survey_id` |
| Resultado individual | `results.id` / `ResultId` | Contexto opcional validado; não identifica medição organizacional |
| Processamento individual | `result_scores.id` | Identificador, datas e valores utilizados preservados em `ProcessingSourceJson` |
| Medição organizacional | `evolution_measurements.id` | Leitura histórica com ID próprio |
| Referência inicial | `evolution_cycles.baseline_measurement_id` | FK que também valida organização e ciclo proprietário |

`diagnostic_id` conserva o contrato contextual anterior, que admite pesquisa ou ciclo diagnóstico. A resolução usa relações persistidas; `survey_id` e `cycle_id` são gravados separadamente. Nenhum ID de pesquisa é gravado como resultado individual.

A consulta de inteligência é uma agregação atual. Na confirmação inicial e em cada reavaliação, uma leitura é preservada com valor, unidade de 0 a 100, momento de referência, metodologia, dimensões, escalas, critérios, recorte, respostas pontuadas, valores de processamento e limitações. As consultas que compõem a leitura compartilham uma transação Repeatable Read. Novas respostas ou reprocessamentos não atualizam a leitura histórica.

O SQL canônico acrescenta referência inicial, versão do ciclo, índices de propriedade e idempotência. Registros anteriores sem medição preservada e com `result_id` permanecem com seus IDs e valores originais, identificados como `legacy_review_required`. Não há conversão automática de referência ambígua nem criação de metodologia histórica.

## Comandos e comparação

Seleção usa páginas de 20 pesquisas. Validação usa consulta por referência, sem carregar toda a coleção. Pesquisas publicadas ativas ou encerradas podem fornecer leituras; ausência de nota é excluída, enquanto zero calculado permanece elegível.

Na referência manual, a aplicação define a origem e remove metadados calculados enviados pelo consumidor. Diagnóstico, resultado individual e governança são validados no escopo organizacional. Na calculada, valor e bases vêm da leitura autorizada. O limite de evidência inclui o contexto acrescentado pela aplicação.

Valores e metas aceitam duas casas decimais, coerentes com a persistência. Campos numéricos HTML são vinculados com cultura invariável, evitando interpretar `85.5` como `855` em servidores pt-BR. O formulário preserva os decimais após erro e apresenta notas e variações com a precisão registrada.

Assinaturas ordenam dimensões e critérios, normalizam números independentemente da cultura e ordenam propriedades JSON. Metodologia, escala ou critério ausente impede comparabilidade. Diferenças relevantes preservam as duas leituras e a explicação, com delta nulo. Não se calcula variação percentual, causalidade, classificação nova ou previsão.

`Preview` verifica estado do ciclo, organização, permissão, contratação e referência posterior. `ConfirmReevaluation` confere a mesma prévia e versão. A transação bloqueia o ciclo, preserva a nova medição, registra comparação e evento de histórico e incrementa a versão. O comando repetido retorna o registro anterior; reutilização com outra referência, versão ou fingerprint é rejeitada.

`CloseReviewed` exige versão, síntese e tratamento das pendências. O fechamento registra histórico e não altera planos nem atividades. O comando antigo sem versão é recusado com mensagem explícita. O ciclo encerrado sem reavaliação permanece identificado como tal.

## Navegação e interface

O Action Center recebe `DiagnosticId` como filtro explícito, incluindo os vínculos comprovados entre pesquisa e ciclo e atividades vinculadas em planos mais amplos. Títulos não participam da identificação. Paginação e retorno contextual são preservados.

A página do ciclo apresenta objetivo, referência inicial, planos, acompanhamentos, reavaliações e aprendizados. A criação separa referência manual e calculada; a nota calculada é somente leitura. A prévia apresenta valores, datas, fontes e limitações lado a lado. Datas de eventos usam o fuso organizacional; datas de período conservam sua natureza de data civil.

A entrada `/Evolution` conduz à lista de ciclos, sem selecionar implicitamente um ciclo ativo para apresentar nota ou tendência. O workspace mostra a situação real do diagnóstico e não atribui percentual a etapas fixas.

O layout administrativo usa a distribuição local completa do Bootstrap 5.3.3, com licença incluída, para disponibilizar os controles e o menu móvel que faltavam no subconjunto anterior. Os SVGs do catálogo compartilhado usam abertura e fechamento explícitos, preservando seus caminhos ao renderizar tags autocontidas.

Referência da dependência: [Offcanvas, documentação oficial do Bootstrap](https://getbootstrap.com/docs/5.3/components/offcanvas/).

## Verificação reproduzível

Execute `dotnet build Valora.sln --no-restore -m:1` e `dotnet test Valora.Tests/Valora.Tests.csproj --no-restore -m:1`, com `VALORA_TEST_POSTGRES_CONNECTION` apontando para banco descartável e `VALORA_REQUIRE_POSTGRES_TESTS=true`.

`EvolutionPersistencePostgresTests` executa consultas e transações reais: referências, isolamento, zero, preservação após mudança dos resultados, pesquisas encerradas, nomes coincidentes, pesquisa/ciclo diagnóstico, escopo de atividades, reavaliação, repetição, conflito de versão e fechamento sem concluir atividades.

Para o ensaio visual, aplique `Valora.Tests/Fixtures/evolution-journey.sql` exclusivamente em banco de teste e execute `node Valora.Tests/Browser/evolution-journey.cjs`, com API e Web locais ativos. A massa é sintética e não faz parte do bootstrap de produção. O script usa Playwright já instalado no workspace e Edge headless, registra capturas e verifica a jornada e os controles em 1440, 768 e 390 pixels. `VALORA_WEB_URL` e `VALORA_VISUAL_OUTPUT` permitem configurar destino e artefatos.

A validação local não substitui homologação com dados representativos e perfis reais. Referências históricas ambíguas continuam dependentes de revisão documentada. O script canônico conserva um aviso anterior de transação sem bloco ativo em sua reaplicação; execução com `ON_ERROR_STOP=1` deve permanecer bem-sucedida.

## Resultado local de 14/10/2026

- Base: `04004362`; alterações locais, sem publicação em produção.
- Build da solução e 531 testes aprovados, sem falhas ou testes ignorados, com PostgreSQL 16 descartável.
- Instalação limpa e reaplicação do script canônico aprovadas com `ON_ERROR_STOP=1`.
- Playwright/Edge: criação calculada, meta fracionária, acompanhamento, prévia, confirmação, recuperação, retorno pelo Action Center, fechamento e continuidade pelo workspace aprovados.
- Referência manual: valor decimal preservado após erro de período e fechamento sem reavaliação quantitativa aprovados.
- Capturas em 1440, 768 e 390 pixels, incluindo criação móvel. Verificados limites dos controles, ausência de rolagem horizontal da página, menu móvel por teclado e ausência de erros JavaScript.
- Artefatos locais em `C:/Users/NCELL-DEV-020/.codex/tmp/valora-evolution`: `creation-desktop.png`, `creation-mobile.png`, `comparison-desktop.png`, `cycle-1440.png`, `cycle-768.png`, `cycle-390.png` e `diagnostic-evolution.png`.
- API local: `http://localhost:5180`; Web: `http://localhost:5188/Evolution`. Banco sintético e perfil de teste; homologação de produção e perfis reais não executada.
- Próximas prioridades: revisar referências históricas pendentes e homologar com população e permissões representativas.

## Etapa seguinte: revisão histórica documentada

- O ciclo pendente dispõe de comando específico de revisão inconclusiva, protegido por permissão `evolution.manage`, política comercial, antiforgery e versão esperada.
- Evidências verificadas e próximo passo são obrigatórios; o registro guarda autor, momento e cópia dos valores, vínculos e metadados originais.
- A gravação de snapshot, evento da jornada e incremento de versão ocorre na mesma transação. Repetir o mesmo comando devolve o registro existente; reutilizá-lo com outros dados ou enviar uma versão obsoleta é rejeitado.
- A revisão não altera notas, referências, metodologia, estado dos planos ou medição inicial. A pendência permanece explícita e comparações quantitativas continuam bloqueadas sem medição histórica preservada.
- Revisões aparecem junto à referência inicial, separadas de acompanhamentos operacionais e reavaliações. Não foi criada tabela ou módulo paralelo: reutilizados snapshots, histórico e índice de comandos canônicos.
- 532 testes aprovados em PostgreSQL real, sem falhas ou ignorados, incluindo preservação de valores antigos, isolamento entre organizações, repetição e conflitos.
- Playwright/Edge aprovou o formulário de revisão, a recuperação do registro e a preservação do valor anterior, além da jornada existente; nenhum erro JavaScript. Capturas `reference-review-1440.png` e `reference-review-390.png` no diretório de artefatos local, com inspeção visual móvel e ausência de rolagem horizontal.
- Conversão de referências históricas comprovadas ainda depende de evidência imutável com origem, processamento e bases metodológicas; identificar somente a pesquisa não basta. Homologação com dados e perfis reais permanece pendente.
