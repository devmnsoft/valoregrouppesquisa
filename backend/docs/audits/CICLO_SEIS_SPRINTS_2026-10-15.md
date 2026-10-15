# Ciclo de evolução — registro de execução de 15/10/2026

## Estado e escopo efetivamente executado

Base local reconfirmada: `c02d28c7` (`novos ites`), árvore inicialmente limpa. A referência remota não foi atualizada. Este registro cobre um incremento da Sprint 1; não constitui aceite da sprint nem conclusão das seis sprints.

## Sprint 1 — robustez dos metadados

1. **Problema reproduzido no código:** `Capture` propagava erros de JSON na seleção; a view de reavaliações desserializava diretamente e aceitava objetos sem campos obrigatórios. Um registro podia interromper a apresentação dos demais. Os testes de regressão exercitam esses caminhos.
2. **Reaproveitamento:** serviços de ciclo, comparador, seleção paginada, DTOs, partial de medição, repositório Dapper e teste PostgreSQL existentes.
3. **Alterações:** JSON malformado preserva a pontuação e a origem, com base indisponível para comparação; opções recebem limitação explícita, inclusive via carregamento paginado. Leitor defensivo de metadados históricos rejeita estruturas incompletas sem fabricar pontuação, identidade ou data. As duas partes de um snapshot são lidas separadamente. Baseline inválida retorna ausência verificável. Avisos de seleção e histórico registram somente identificadores, sem conteúdo do JSON. Marcadores metodológicos `unknown`/`indefinida`, inclusive com versão, impedem comparação. Versão em objeto/array não é aceita como identificação metodológica.
4. **Regras preservadas:** zero medido permanece zero; informação ausente não vira medição; nenhuma escrita corrige silenciosamente histórico; nenhuma fórmula, faixa, contratação ou permissão nova foi introduzida. O leitor exige identidade, data, título, pontuação e objeto de base preservados.
5. **Telas/contratos:** `/Evolution/Cycles`, detalhes e histórico, `/Evolution/MeasurementOptions`; `EvolutionResultOptionDto` ganhou `Limitation` opcional. Leitura do baseline no repositório também usa o leitor defensivo.
6. **Verificação:** ver matriz abaixo. Compilação realizada pelo comando de testes Release. Não há homologação visual/HTTP neste incremento.
7. **Riscos:** documentos históricos incompletos passam a exigir revisão; não há reconstrução automática. Falta executar matriz completa de perfis/contratação e concorrência. Validação do leitor histórico não atesta que uma metodologia seja oficialmente reconhecida.
8. **Condição para avançar:** concluir a homologação da Sprint 1 para os caminhos dependentes. Preparação independente das sprints seguintes pode prosseguir; não existe bloqueio técnico comprovado que justifique classificá-las como bloqueadas.

## Matriz de evidências da primeira entrega

| Cenário | Situação | Evidência |
|---|---|---|
| JSON inválido na seleção, ao lado de registro válido | Executado com sucesso | `EvolutionCycleServiceTests.Malformed_metadata_does_not_remove_other_selection_options` |
| Regra de cálculo com JSON malformado | Executado com sucesso | `Malformed_calculation_rule_preserves_score_without_comparability` |
| Estrutura incompleta/metodologia desconhecida | Executado com sucesso | `Incomplete_snapshot_never_proves_comparability`, `UnknownMethodologyNeverProvidesComparability` |
| Histórico inválido, parcial e zero válido | Executado com sucesso | `EvolutionPreservedMetadataTests` |
| Instalação limpa do SQL canônico | Executado com sucesso | PostgreSQL 16 Alpine descartável, banco `valora_sprints_test`, script até COMMIT, exit 0 |
| Reaplicação do mesmo SQL | Executado com sucesso | Mesmo banco e script, até COMMIT, exit 0 |
| Persistência/replay/versão/isolamento e histórico | Executado com sucesso | `EvolutionPersistencePostgresTests`, 1 teste aprovado na execução final; contém múltiplos cenários, não equivale a homologação HTTP |
| Nova asserção PostgreSQL para metadado incompleto | Executado com sucesso | Baseline indisponível com pontuação original preservada no ciclo |
| Build paralelo inicial | Falhou | MSB4166: processos filhos encerrados; execução sequencial posterior passou |
| Navegador, 390/768/1440, formulários e perfis distintos | Não executado | Sem evidência visual produzida |
| Troca de contratação entre prévia e comando | Não executado | Pendente |
| Sprints 2–6 | Não executado | Não declarar implementadas ou homologadas |

## Reprodução

- `dotnet test Valora.Tests/Valora.Tests.csproj --filter "FullyQualifiedName~Evolution&FullyQualifiedName!~Postgres" -c Release --no-restore -m:1`: 61 aprovados, zero falhas/ignorados.
- Banco exclusivamente local de testes em container `valora-six-sprints-test`, porta 55434. Aplicar `database/postgresql/script_completo.sql` com `psql -v ON_ERROR_STOP=1 -f` duas vezes.
- Definir `VALORA_TEST_POSTGRES_CONNECTION` para esse banco (nome contendo `test`) e executar o mesmo filtro `FullyQualifiedName~Evolution`, incluindo persistência. Não usar banco de produção.
- Logs operacionais locais: `%TEMP%/valora-sprints-install.log` e `%TEMP%/valora-sprints-reapply.log`.
- Execução final com PostgreSQL: **62 aprovados, zero falhas e zero ignorados** (3 segundos de testes).
- Script canônico, legado e configurações de produção não foram alterados. Nenhum commit, merge ou publicação executado.

## Pendências em ordem na primeira entrega

1. Concluir Sprint 1: matriz canônica de permissões (leitura, gestão e acompanhamento), contratação, HTTP e jornadas de navegador, incluindo prévia desatualizada e repetição.
2. Sprint 2: preparação/publicação/coleta, imutabilidade de versão e contadores de participação.
3. Sprint 3: campos explícitos de evidência, revisão humana e conversão rastreável sem duplicação.
4. Sprint 4: vínculos de execução, transferência/replanejamento, alertas e reavaliação.
5. Sprint 5: inventário de menu e formulários nas três resoluções com interação real.
6. Sprint 6: devolutiva versionada, exportação/revogação, onboarding e demonstração com duas organizações.


## Continuação — permissões, contratação, HTTP e concorrência

### Problema e alterações

A leitura do serviço de ciclos aceitava gestão implicitamente, enquanto controller, serviço de snapshots e consulta de permissões exigiam `evolution.read`. A relação canônica adotada segue as permissões explícitas já exigidas pelo controller e por `PermissionRepository`: leitura, gestão e acompanhamento são autorizações distintas, sem concessão transitiva. Os papéis continuam recebendo suas permissões pelo cadastro existente; nenhuma permissão foi atribuída a usuários ou papéis nesta entrega.

| Operação | Permissões necessárias na rota MVC | Contratação para gravação |
|---|---|---|
| Consultar ciclos, opções e histórico | `evolution.read` | Leitura avaliada pelo middleware comercial para usuário organizacional |
| Abrir, prévia, confirmar, revisar referência, encerrar | `evolution.read` + `evolution.manage` | `evolution`, escrita autorizada e sem modo somente leitura |
| Registrar acompanhamento | `evolution.read` + `evolution.snapshots.generate` | `evolution`, escrita autorizada e sem modo somente leitura |
| Administrador global | Bypass de permissão existente, com organização selecionada | Comandos do serviço continuam avaliando contratação da organização |
| Outra organização/autor forjado | Rejeitado pelo serviço | Não concede acesso |

As telas usam autorização de gestão/acompanhamento e decisão comercial para apresentar formulários de gravação. Os dois itens de menu MVC de evolução agora declaram `evolution.read` e módulo `evolution`, em correspondência com as rotas. O middleware rejeita escrita também quando recebe `Allowed=true, ReadOnly=true`. As regras comerciais e os cadastros de planos permanecem existentes.

Foi detectado por HTTP que a reexecução de erro de um POST sem antiforgery resultava em 404: o endpoint de apresentação de erro aceitava somente GET. A rota de erro passou a aceitar o método original da reexecução, preservando 400. Nenhum comando de negócio é executado pela página de erro. Falhas de autorização dos comandos MVC de evolução passam a retornar `Forbid`, inclusive se a contratação mudar depois da avaliação inicial.

### Funcionalidades reaproveitadas e contratos afetados

Serviços de ciclo e snapshot, contexto de organização, políticas registradas, `CommercialSaasService`, middleware comercial, catálogo de navegação, Razor, antiforgery e teste PostgreSQL canônico foram mantidos. Não houve novo módulo ou alteração SQL. A mudança na rota de erro e no middleware é compartilhada; os testes de páginas de erro e da fundação SaaS integram a verificação direcionada.

### Cenários adicionais

- Executados com sucesso: matriz unitária de leitura/gestão/acompanhamento/global, organização diferente e autoria forjada; contratação revogada entre prévia e confirmação (inclusive replay); fonte alterada, versão alterada e ciclo encerrado; replay autorizado sem nova escrita.
- Executados com sucesso na rodada HTTP inicial corrigida: dez cenários de políticas, contratação e antiforgery, além de seis verificações existentes de páginas de erro. As identidades e decisões comerciais dessa suíte são substitutos de teste; ela não comprova login real nem os vínculos de cada papel no banco.
- Executados com sucesso na rodada seguinte: 100 testes, incluindo renderização HTTP de ciclos para consulta/somente leitura, persistência com duas confirmações simultâneas do mesmo comando (um snapshot), bloqueio de acompanhamento após encerramento e regressões de navegação/erro.
- Nova jornada HTTP manual, com persistência e antiforgery: criação, acompanhamento, reabertura e encerramento, preservando zero e síntese. A primeira execução atingiu o encerramento e falhou na asserção textual por entidades HTML; o teste foi corrigido para decodificar a apresentação. Resultado da repetição final abaixo.
- **Bloqueado:** navegador nesta sessão. `cua.getState()` retornou zero navegadores e `cua.createBrowserTab("iab", ...)` retornou `Browser is not available: iab`. Não foram produzidas capturas nem homologação de interação em 390/768/1440. HTTP renderizado não substitui esse aceite.

### Riscos, pendências e passagem

A Sprint 1 continua sem aceite integral. Faltam login real com usuários organizacionais distintos, matriz de papéis/contratação persistida e percurso calculado completo via HTTP/navegador. A autorização de leitura dos serviços não passou a fazer consulta comercial própria: o contrato de leitura MVC continua no middleware; as gravações mantêm defesa no serviço. Alteração simultânea da contratação após a última consulta do serviço e antes do commit não foi homologada como transação atômica.

Condição para os avanços dependentes: completar essas verificações e a validação visual quando houver navegador. As Sprints 2–6 continuam pendentes e não foram declaradas entregues.


### Resultado final da continuação

- **110 testes aprovados, zero falhas, zero ignorados**, em 6 segundos de testes, com compilação Release bem-sucedida. Permanecem avisos preexistentes de compilação, incluindo CS8603 em `EvolutionComparisonQueryTests`.
- Filtro: `FullyQualifiedName~Evolution|FullyQualifiedName~ErrorPageIntegrationTests|FullyQualifiedName~NavigationRegressionTests|FullyQualifiedName~ModularSaasFoundationTests`, com `VALORA_TEST_POSTGRES_CONNECTION` apontando exclusivamente para `valora_sprints_test`, `--no-restore -m:1`.
- A jornada HTTP manual passou usando o pipeline Web, Razor, antiforgery e persistência reais. Autenticação e decisão comercial foram substituídas por implementações de teste; não equivale a login real nem a homologação visual.
- `git diff --check` passou. Log local: `%TEMP%/valora-pendencias-final-tests.log`.
- Container de testes parado após a verificação; dados de fixture da jornada HTTP removidos no próprio teste. Nenhum serviço de outro projeto foi interrompido.
- Sem alteração no script SQL canônico, preços, papéis, dados de produção ou legado; sem publicação ou merge.


## Continuação — autenticação real, medição calculada e contrato do workspace

### Reconfirmação e correções

O banco canônico revelou o mapeamento `saas_modules.code=evolution` → `access_module_code=organizational_intelligence`. O menu usa os módulos de acesso emitidos pelo login, enquanto o middleware/serviço comercial avalia o código comercial. A mudança anterior do menu para módulo `evolution` estava incorreta e foi corrigida para `organizational_intelligence`, preservando `evolution.read`. Esta constatação substitui a descrição anterior que equiparava os dois códigos. Nenhum cadastro de módulo ou contratação foi modificado para contornar a diferença.

A captura de uma medição passa a exigir escala e critérios preservados para todas as dimensões medidas antes de permitir comparação. Uma cobertura parcial não pode provar equivalência por mera igualdade das partes presentes. A correção se aplica a novas capturas; não regrava bases históricas.

No workspace diagnóstico, os dois emissores de POST enviavam `RequestVerificationToken`, embora `Program.cs` configure `X-CSRF-TOKEN`. Ambos foram alinhados ao cabeçalho canônico. O script passou a ser referenciado com `asp-append-version` para que uma versão em cache não mantenha o defeito. A proteção antiforgery não foi relaxada.

### Como a nova homologação é executada

`EvolutionAuthenticatedJourneyTests` inicializa API e Web reais com `WebApplicationFactory`, ligando o cliente HTTP do BFF ao transporte do TestServer da API. Não substitui autenticação, cookies, autorização, serviço comercial ou repositórios por implementações falsas. Usa BCrypt real, sessão persistida, dois registros de organização, assinaturas nos contratos existentes e usuários sintéticos com papéis e permissões explícitas no banco descartável.

Os papéis básicos existentes são preservados. Permissões extras são atribuídas somente a papéis de fixture próprios da organização de teste; não representam nova política padrão para administradores ou gestores. Os títulos dos diagnósticos são iguais entre organizações para verificar que isolamento não depende de título. Os dados de respostas pontuadas são fixtures sintéticas de persistência, não respostas coletadas em navegador nem homologação do cálculo a partir do questionário público.

As tentativas intermediárias falharam em asserções de apresentação/expectativa HTTP. O contrato correto para o POST de recurso não suportado na API é 405; o cabeçalho antigo é rejeitado antes disso com 400. Não se trata de sucesso de um comando de coleta. A asserção inicial da prévia também pressupunha ponto decimal; a resposta autenticada usa vírgula decimal. O teste passou a aceitar a representação numérica de apresentação, mantendo a verificação exata dos valores persistidos. O percurso inicial completo passou em 17 segundos antes da ampliação para comparação incompatível e revisão histórica.

### Pendências preservadas

A nova consulta de `cua.getState()` continua retornando zero navegadores. Validação visual e interação em 390/768/1440 continuam bloqueadas, sem evidência visual fabricada. O teste autenticado HTTP não substitui a execução de formulários no navegador.

A Sprint 2 recebeu somente a correção independente do cabeçalho/cache; preparação, publicação, campanha e participação ainda não receberam aceite completo. Sprints 3–6 permanecem pendentes.


### Sprint 2 — correção independente do envio de comandos

1. **Problema:** cabeçalho antiforgery enviado pelo workspace divergia da configuração do servidor, bloqueando POSTs antes da API.
2. **Reaproveitado:** token Razor existente, proteção antiforgery e encaminhamento BFF canônicos.
3. **Alterações:** correção dos dois cabeçalhos e versionamento do asset no Razor.
4. **Regras preservadas:** token continua obrigatório; autenticação, permissões e contratação não são substituídas pela correção de cabeçalho.
5. **Telas/contratos afetados:** workspace diagnóstico e chamadas da campanha originadas nele; cabeçalho `X-CSRF-TOKEN` já configurado no servidor.
6. **Evidências:** `node --check Valora.Web/wwwroot/js/pages/diagnostic-workspace.js` passou. O teste autenticado envia o cabeçalho antigo e recebe 400; com o canônico, chega à API e recebe 405 para um recurso propositalmente sem POST. Este cenário verifica o contrato de transporte, não a preparação/publicação/envio de campanha.
7. **Riscos/pendências:** não executada a interação JavaScript em navegador; demais critérios da Sprint 2 continuam pendentes.
8. **Passagem:** esta correção é independente dos aceites visuais pendentes da Sprint 1; não autoriza declarar a coleta homologada.


### Resultado final desta continuação

**113 testes aprovados, zero falhas, zero ignorados**, em 24 segundos de testes, com compilação Release bem-sucedida. Filtro de reprodução: `FullyQualifiedName~Evolution|FullyQualifiedName~ErrorPageIntegrationTests|FullyQualifiedName~NavigationRegressionTests|FullyQualifiedName~ModularSaasFoundationTests`, conexão `VALORA_TEST_POSTGRES_CONNECTION` exclusivamente para `valora_sprints_test`, opções `-c Release --no-restore -m:1`. Log: `%TEMP%/valora-pendencias-final-tests.log`.

| Cenário autenticado | Resultado e limite |
|---|---|
| Login API → BFF, senha BCrypt, cookie e sessão persistida | Sucesso com quatro usuários sintéticos; nenhum substituto de autenticação/permissão/contratação |
| Duas organizações, diagnósticos com títulos iguais | Sucesso: opções não incluem pesquisas alheias e detalhe de ciclo alheio retorna 404 |
| Gestão autorizada, consulta e usuário sem leitura | Sucesso: consulta não apresenta comandos; comando direto de gestão rejeitado; usuário sem leitura recebe acesso negado |
| Menu com módulo canônico de acesso | Sucesso: item de ciclos aparece no login organizacional com `organizational_intelligence` e `evolution.read` |
| Criação calculada com valor numérico forjado no POST | Sucesso: servidor preserva o 40 calculado e ignora o 99 enviado |
| Prévia e alteração da fonte antes da confirmação | Sucesso: comando antigo exige nova prévia e não grava snapshot |
| Confirmação repetida | Sucesso: um único registro quantitativo, delta exato 35 após nova prévia da pontuação 75 |
| Acompanhamento autenticado | Sucesso: registro preservado e separado da medição |
| Nova medição com escala incompatível | Sucesso: limitação apresentada, reavaliação registrada sem delta e `current_score` preservado em 75 |
| Reabrir e encerrar com síntese | Sucesso: inicial 40 e posterior preservados; síntese e pendências persistidas |
| Revisão histórica inconclusiva repetida | Sucesso: um snapshot de revisão, referência original 37,5 preservada e próximo passo consultável |
| Contratação persistida alterada para somente leitura | Sucesso: próximo comando recebe 403 / `MODULE_READ_ONLY` |
| Escala/critério ausente em somente parte das dimensões | Sucesso nos dois casos unitários novos: sem comparação e sem delta |

`git diff --check` e verificação sintática JavaScript passaram. O container descartável foi parado após os testes. As fixtures autenticadas são mantidas no banco descartável como evidência sintética; usuários/organizações são desativados e assinaturas comerciais canceladas no `finally`. Não há mudanças operacionais em bancos externos.

Permanecem pendentes: administrador global e responsável exclusivo por acompanhamento em login real; verificação visual/interação nos três tamanhos; jornada completa de publicação/coleta a partir de respostas públicas; instalação/atualização representativa com dados históricos além da reaplicação já registrada. A suíte usa medições pontuadas de fixture; não constitui validação metodológica do processamento de um questionário público. Sprints 2–6 continuam sem aceite completo. Não houve publicação, commit ou merge.


## Continuação — perfis restantes e publicação/coleta da Sprint 2

### Sprint 1 — administrador global e acompanhamento exclusivo

O percurso autenticado foi ampliado para seis usuários. O administrador global recebe 403 sem organização selecionada, seleciona contexto pelo BFF/API canônico, recebe 404 ao tentar abrir um ciclo da outra organização e acessa o ciclo após selecionar a organização correta. Mesmo global, a gravação de acompanhamento é rejeitada depois da restrição contratual, sem criar snapshot. O responsável com somente `evolution.read` + `evolution.snapshots.generate` registra acompanhamento com autoria persistida e não recebe formulário nem autorização para encerrar ou comparar.

Esses cenários passaram em execução autenticada PostgreSQL/API/BFF; não dependem de substitutos de autenticação. O cenário global mantém usuário vinculado a uma organização de fixture, conforme o contrato de login existente; não foi criada uma política nova de usuário global sem organização de origem.

### Sprint 2 — entrega incremental

1. **Problemas reconfirmados por execução:** POST JSON sem `Content-Length` era descartado no BFF (415 na API); criação de pergunta omitindo `questions.position` obrigatório produzia falha PostgreSQL; rotas BFF de campanha usavam o parâmetro reservado `action` e encaminhavam recurso incorreto (404); atualização de perguntas/seções/opções e exclusão estrutural continham referência SQL ambígua a `version`; comando de campanha não enviava corpo/versão e o servidor ignorava a versão esperada; acesso direto aos endpoints administrativos de diagnóstico não aplicava permissão específica nem contratação; taxa de campanha sem destinatários era apresentada como zero.
2. **Reaproveitado:** FormAdministration, publicação e revisão existentes, versões imutáveis, cadastros/permissões, gerador e validação canônicos de links, campanha, serviço comercial, PostgreSQL/Dapper e transporte BFF. Nenhum módulo SaaS ou mecanismo paralelo de link foi criado.
3. **Alterações:** o BFF lê JSON também em requisições com tamanho desconhecido; parâmetro interno de rota foi renomeado para `command` sem mudar as URLs; espelhos novos de perguntas recebem posição e tipo/qualificação compatíveis, inclusive ao clonar; SQL qualifica a versão pela tabela atualizada; campanha transmite versão esperada e a verifica no serviço e sob lock no repositório; ativação manual exige link público elegível; a elegibilidade do link também considera revogação e período do diagnóstico. O fluxo manual não passa a preencher `sent_at`. Taxa sem denominador passa a `null` nos contratos de campanha e métricas.
4. **Regras preservadas/ajustadas:** autorização e contratação continuam distintas. A API de formulários avalia módulo `forms`; campanha e diagnósticos/distribuição avaliam `surveys`, usando o mesmo repositório comercial. Criação, edição, publicação/encerramento e distribuição de diagnóstico usam as respectivas permissões canônicas. O token público continua retornado somente na criação; a interface conserva a URL apenas em memória e solicita novo link ao reabrir. Copiar não registra envio. Filas não representam entrega confirmada. O SMTP não configurado mantém o envio bloqueado. Nenhum valor histórico foi recalculado ou corrigido em lote.
5. **Telas/contratos afetados:** estúdio de formulários, diagnósticos/links, workspace/campanha; `/bff/forms`, `/bff/surveys`, `/bff/diagnostics/{id}/campaign` e APIs correspondentes. Os cabeçalhos de capacidade para gestão/distribuição são derivados das políticas canônicas e o modo somente leitura é propagado à interface. `RequireCommercialModuleAttribute` é um filtro transversal da API que reutiliza o avaliador existente, não um módulo comercial novo. `CompletionRate` agora admite `null`: consumidores externos que pressupunham um número precisam tratar a ausência de denominador.
6. **Testes/evidências executadas:** percurso HTTP autenticado cria formulário qualitativo vazio, comprova bloqueio de publicação, adiciona seção/pergunta, consulta revisão com aviso de limitação metodológica, publica e cria/ativa diagnóstico. Gera link pelo endpoint canônico e valida o acesso público anônimo. Cria/ativa campanha manual, verifica zero envios e taxa ausente, rejeita comando desatualizado e comprova que duas solicitações simultâneas de cancelamento produzem um sucesso e um conflito. Repete a configuração com WhatsApp manual. E-mail duplicado/inválido é rejeitado; destinatário válido fica em rascunho, sem envio e com job retido, quando SMTP está desabilitado. Usuário de consulta não cria/publica diagnóstico, não recebe capacidades de gestão/distribuição da campanha; outra organização recebe 404. Depois de criar novo rascunho, edita e exclui seções/perguntas/opções e confirma que o link público continua lendo a versão publicada original. Restrições comerciais bloqueiam distribuição, comandos de campanha e publicação de formulário. O percurso completo passou individualmente em 7 segundos antes da rodada de regressão final.
7. **Riscos/pendências:** sem navegador conectado, não foram executados cliques, clipboard real, responsividade ou formulários nas três resoluções. A leitura pública do formulário foi validada, mas o envio de resposta pública/consentimento, seu resultado e os contadores de respostas válidas/pontuadas ainda estão pendentes nesta jornada. Preparação consolidada com público/responsável/período e revisão metodológica quantitativa não receberam aceite integral. E-mail real não foi enviado nem homologado; o host de teste configura `Email:Enabled=false`. Ainda falta definir/verificar, pelos contratos existentes, retomada de criação de campanha após perda de resposta e aplicação dos limites comerciais de volume; não foi criada regra nova de limite ou de campanha única por diagnóstico.
8. **Condição para avançar:** a criação/publicação qualitativa e a operação manual foram comprovadas por HTTP/persistência, permitindo seguir com pendências independentes. O aceite da Sprint 2 continua condicionado a resposta pública, participação, preparação consolidada, limites existentes e interação visual. Sprints 3–6 permanecem sem aceite.

### Banco e recuperação

Não foi necessário alterar `database/postgresql/script_completo.sql`, criar migrations ou relaxar constraints. As correções alinham as gravações e consultas ao schema canônico já instalado. Fixtures permanecem exclusivamente no banco descartável `valora_sprints_test`, com usuários/organizações desativados ao final. Nenhum serviço de outro projeto foi alterado. Para reverter código desta rodada, revisar o diff dos arquivos citados; não executar correção retroativa de dados históricos como parte da reversão.


### Regressão final da rodada

- **578 testes aprovados, zero falhas, zero ignorados**, em 38 segundos de testes; build Release concluído. Comando: `dotnet test Valora.Tests/Valora.Tests.csproj -c Release --no-restore -m:1`, com `VALORA_TEST_POSTGRES_CONNECTION` configurada exclusivamente para o PostgreSQL descartável local. Log: `%TEMP%/valora-six-sprints-regression.log`.
- `git diff --check` e `node --check Valora.Web/wwwroot/js/pages/diagnostic-workspace.js` passaram. A formatação foi aplicada somente aos arquivos C# alterados listados nesta rodada.
- A execução de toda a suíte amplia a evidência de regressão; não comprova jornadas não exercitadas em integração, funcionamento visual, envio real de e-mail ou prontidão comercial integral.
- A nova consulta de superfícies disponíveis retornou `apps=[]`, `browsers=[]`; a validação visual segue bloqueada por falta de navegador conectado.
- Script SQL canônico preservado. Container de testes parado ao encerrar a rodada. Sem commit, publicação, merge ou alterações em produção.

### Backlog atual, em ordem

1. Exercitar envio público de resposta com consentimento e idempotência pela cadeia Web/BFF/API/PostgreSQL, depois conferir separadamente respostas recebidas, válidas, pontuadas e pendências. A validação pública de leitura já passou; não inferir submissão a partir disso.
2. Completar preparação do diagnóstico (objetivo, público/recorte, responsável, período e pendências com ação) e verificar limites de volume existentes nos comandos de coleta.
3. Executar formulários e clipboard real em navegador nas resoluções 390/768/1440 quando houver superfície disponível; não substituir por leitura de HTML.
4. Sprint 3: evidências/achados explícitos, revisão e plano rastreável sem duplicação.
5. Sprint 4: responsabilidades, replanejamento e alertas; Sprint 5: navegação/formulários e validação visual restante.
6. Sprint 6: devolutiva versionada, compartilhamento/exportação, onboarding baseado em registros reais e homologação comercial final.
