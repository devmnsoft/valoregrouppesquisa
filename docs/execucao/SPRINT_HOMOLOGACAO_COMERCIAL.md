# Continuidade — Sprint de homologação comercial

Atualizado em 2026-09-10. Este é o documento único de continuidade desta sprint. Os estados abaixo distinguem **implementado**, **testado** e **não verificado**; build isolado não equivale à homologação do fluxo.

## Baseline

- Branch local: `work`; HEAD inicial `77f0520` (merge do PR #553), exatamente a referência auditada solicitada.
- O checkout não possui remoto configurado. O comando `git fetch --all --prune` foi executado, mas não havia remoto a consultar; portanto não foi possível comparar commits posteriores, fazer `push` ou integrar a branch-base.
- Árvore inicial sem alterações locais. Nenhum `AGENTS.md` foi encontrado no repositório, em seu diretório pai ou nos caminhos globais inspecionados.
- Referências metodológicas consultadas: `docs/VALORA_METHODOLOGY_ALIGNMENT.md` e os contratos/testes existentes. O próprio documento registra que os PDFs metodológicos citados anteriormente não estão no workspace; conteúdo ausente não foi presumido.
- Banco de produção não foi acessado. `VALORA_TEST_POSTGRES_CONNECTION` não estava configurada e o SDK `dotnet` não está instalado neste container.

## Bloco A — parâmetro `wide`

### Achado confirmado

**Implementado:** `WorkspaceRepository.MyDayAsync` usava `@wide` no SQL, mas o helper criava um objeto contendo apenas `o` e `u`. O PostgreSQL recebia um placeholder sem parâmetro Dapper correspondente e o interpretava como identificador, produzindo `42703: column "wide" does not exist`.

### Decisão e correção

**Implementado:** o helper passou a receber explicitamente `object parameters`. Todas as consultas sujeitas à regra de visibilidade enviam `{ o, u, wide }`. Não foi criada coluna, removido filtro, concatenado booleano nem convertido erro em lista vazia. O predicado de organização permanece obrigatório. Itens sem proprietário continuam compartilhados porque essa é a regra já expressa pelo SQL vigente.

**Implementado:** a API não lê mais `organization_id` diretamente no controller do Workspace. Ela usa `ICurrentRequestContext`, inclusive a organização selecionada pelo mecanismo central para o administrador global. A visão ampla é derivada do contexto autenticado (`IsGlobalAdministrator` ou papel `admin_cliente`); não existe parâmetro HTTP `wide` controlado pelo navegador. Requisições sem usuário válido são recusadas.

**Implementado:** a mesma expressão de visibilidade (item próprio, compartilhado ou visão ampla autorizada) passou a proteger Meu Dia, fixação, fixados e recentes. Fixar um UUID privado de terceiro não cria registro e retorna acesso negado; fixados e recentes são reavaliados a cada leitura, de modo que uma troca posterior de proprietário revoga a exposição. As ordenações tocadas ganharam desempate por UUID.

### Evidência de teste

- **Implementado, não executado localmente:** teste PostgreSQL exercita visão restrita e ampla, itens próprios/de terceiros/sem proprietário, isolamento entre duas organizações, recusa de pin privado, revalidação de fixados/recentes depois da troca de proprietário e pin/unpin repetidos. O teste recusa nomes de banco sem marcador `test`, `teste`, `homolog` ou `qa` e remove apenas os UUIDs aleatórios criados por ele.
- **Implementado:** sem `VALORA_TEST_POSTGRES_CONNECTION`, o teste agora produz skip explícito em vez de retornar como aprovado. O workflow PostgreSQL define e verifica a variável e executa obrigatoriamente a categoria `DatabaseContract` depois de aplicar o schema canônico.
- **Implementado, não executado localmente:** contrato estático protege o binding de `wide`, a assinatura do helper e a derivação server-side no controller.
- **Não verificado:** endpoint completo autenticado contra PostgreSQL, perfil não autorizado tentando elevar escopo e execução do teste PostgreSQL. Esses cenários permanecem gate obrigatório de CI/homologação; a ausência de infraestrutura não é aprovação.

## Revisão dirigida das regressões conhecidas

- **Não verificado nesta entrega:** materialização de `FormRow`, `FormListItemResponse` e `SubscriptionRow`; tipos opcionais de `CompaniesAsync`; duplicidade em `ProcessRepository`; DI de `BenchmarkRepository`; strings de `FormalDeliverableRepositories`; Razor/CSS de Benchmarks e Indicators. Devem ser confirmados por consulta executada e testes específicos, não apenas busca textual.
- **Não verificado nesta entrega:** BFF público, sessão autenticada, downloads binários, concessões por aba, exportações, leases de workers e transições de campanhas.
- **Não verificado nesta entrega:** padronização visual, formulários, responsividade e Playwright nas cinco viewports. Nenhuma tela foi alterada nesta entrega e, por isso, não se declara evolução visual concluída.

## Blocos B–F e seed de homologação

- **Não implementado:** seed sintético canônico. Antes de incluí-lo em `backend/database/postgresql/script_completo.sql`, ainda é necessário mapear integralmente constraints, mecanismo seguro de provisionamento de identidade, capacidades contratadas e efeitos externos. Não foi criado SQL concorrente nem pseudoseed inseguro.
- **Não testado:** primeira e segunda execução idempotente, referências, isolamento, processamento metodológico, publicação/campanha/resposta/resultado, entregáveis e exportação.
- **Não verificado:** PostgreSQL e Redis locais, hosts `Valora.Api`/`Valora.Web` e Playwright integrado.

## Como executar a regressão PostgreSQL com segurança

1. Provisione banco descartável já migrado pelo script canônico, cujo nome contenha `test`, `teste`, `homolog` ou `qa`; nunca use produção.
2. Defina `VALORA_TEST_POSTGRES_CONNECTION` somente no processo de teste.
3. Execute `dotnet test backend/Valora.Tests/Valora.Tests.csproj -c Release --filter WorkspaceRepositoryPostgresTests`.
4. Confirme no relatório que o teste `Workspace_queries_enforce_visibility_tenant_and_idempotent_pins` foi realmente executado. Sem a variável, ele não toca banco e a homologação PostgreSQL continua pendente.

## Bloqueios, riscos e próximo passo

1. **P0:** disponibilizar .NET SDK 10 e PostgreSQL descartável migrado; executar a regressão e acrescentar teste de host autenticado para contexto ausente, troca de cliente e tentativa de ampliar escopo.
2. **P0:** configurar remoto/autorização para comparar a branch-base, fazer `push` e abrir PR remoto. Não há evidência local de commits posteriores a `77f0520`.
3. **P1:** desenhar e revisar o seed transacional opt-in no SQL canônico, incluindo trava inequívoca de ambiente e desativação de integrações externas, antes de inserir fixtures.
4. **P1:** executar os blocos C e D (sessão/BFF/downloads; campanhas/exportações/workers) com testes de host e PostgreSQL.
5. **P2:** somente então concluir Workspace/formulários/responsividade e executar Playwright real com API, Web, PostgreSQL e Redis.

Próximo passo concreto: instalar o SDK exigido, configurar um PostgreSQL descartável, aplicar `script_completo.sql` e executar o filtro `WorkspaceRepositoryPostgresTests`; falha nesse gate bloqueia o avanço da homologação.
