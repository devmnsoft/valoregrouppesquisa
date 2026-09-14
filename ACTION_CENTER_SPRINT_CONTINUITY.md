# Continuidade — ActionCenter operacional

Revisão realizada a partir do `HEAD` `0dc7419` (PR #568), preservando o incremento anterior.

## Matriz de implementação

| Funcionalidade | Evidência no código | Lacuna encontrada | Alteração | Teste/evidência |
|---|---|---|---|---|
| Filtro por responsável | `ActionCenterController` e `ActionItemRepository` | O filtro de páginas de leitura chamava o catálogo de atribuição protegido por `Action.Manage` | Endpoint de leitura próprio e consulta limitada aos responsáveis presentes nos planos/atividades que o usuário pode ver, sempre isolada por organização | Teste JS de componentes; consulta parametrizada revisada |
| Atribuição | `ResponsibleOptions` e `Create` do repositório | Era compartilhada indevidamente com o filtro | Endpoint de atribuição continua exigindo `Action.Manage`; gravação continua revalidando usuário ativo da mesma organização dentro da transação | Contratos de execução existentes e isolamento na instrução `INSERT ... SELECT` |
| Seletor paginado | `responsible-selector.js` | Seleção inicial imutável, retry ambíguo e busca global no documento | Estado por componente, seleção atual preservada, abort/versionamento, retry da página exata, mensagens distintas, 401/403 e acessibilidade | `node --test tests/action-center/responsible-selector.test.js` |
| Navegação pós-comando | `ActionCenterController.ExecuteCommand` | Sucesso descartava retorno e erro voltava para uma lista fixa | Contexto tipado com retorno/histórico, allowlist local e propagação no sucesso/erro | Inspeção dos redirecionamentos e build pendente por SDK ausente |
| Criação de atividade | `CreateItem` e `PlanDetails.cshtml` | O retorno do plano era confundido com o retorno da atividade e podia apontar o cabeçalho para o próprio detalhe | Separados retorno da lista, URL corrente do plano e retorno da atividade; página interna preservada | Fluxo codificado nos campos ocultos e redirect |
| Duplo envio | `form-submit-state.js` | Estado podia permanecer ao restaurar via histórico e interferir com validação cancelada | Bloqueio posterior aos validadores, restauração em `pageshow` e reativação apenas dos controles bloqueados pelo módulo | Verificação de sintaxe JavaScript |

## Validação e limitações desta execução

- O SDK exigido é .NET `10.0.100`, conforme `backend/global.json`.
- O contêiner desta execução não possui o executável `dotnet`; portanto restore, build, testes .NET e compilação Razor permanecem **não validados**, e não foram simulados nem contornados.
- Não há cliente PostgreSQL nem instância de teste configurada no ambiente; a execução real das consultas permanece **não validada**.
- Os testes comportamentais do seletor e as verificações de sintaxe JavaScript foram executados localmente com Node.js.
- A validação visual autenticada nos cinco viewports permanece **não validada**, pois não há aplicação compilada/executável nem credenciais/fixture autenticada neste contêiner.

## Incremento — dashboard comercial e criação confiável (2026-09-14)

Linha de base confirmada no `HEAD` `4ca5fb7` (PR #569); seletores, navegação e proteção de envio existentes foram preservados.

| Problema observado | Alteração necessária | Teste de aceitação |
|---|---|---|
| Cards não navegavam ou aproximavam a consulta (críticas podiam incluir encerradas e ausência de responsável não tinha representação própria) | Filtros tipados `Open`, `Active` e `Assigned/Unassigned`, usados nos links, contagens e SQL sem UUID sentinela | Abrir cada indicador e comparar o total da listagem com o card |
| Dashboard calculava atraso por instante (`due_at < now()`), enquanto listas trabalhavam por dia | Comparar o dia civil no `time_zone` da organização e persistir prazo de formulário no fim do dia civil local | Ontem atrasa; hoje e amanhã não; nulo não atrasa; concluída/cancelada não entra no indicador |
| Criação direta validava somente responsável e não era idempotente | Validar ator, módulo, organização, origem/referências, datas e responsável dentro da transação; registrar chave+hash+resultado por organização, autor e operação | Repetição equivalente retorna o plano original; payload divergente gera conflito e preserva o formulário |
| Dashboard e formulário tinham baixa hierarquia operacional | Cabeçalho compacto, cinco KPIs acionáveis, consultas rápidas, agenda, acompanhamento e formulário em seções responsivas com revisão | Navegação por teclado e uso sem rolagem horizontal da página nos cinco viewports-alvo |

### Validação deste incremento

- JavaScript novo e scripts associados passaram em verificação de sintaxe; os testes comportamentais do seletor passaram.
- O SDK .NET `10.0.100` continua indisponível neste contêiner (`dotnet: command not found`), portanto restore, build, testes .NET e compilação Razor não puderam ser executados.
- Não há `psql`, Docker ou instância PostgreSQL configurada; a migração e os cenários de integração SQL não puderam ser executados contra banco real.
- Sem aplicação compilável, credenciais e navegador automatizado disponível, o percurso autenticado e as capturas dos cinco viewports permanecem pendentes; a responsividade foi tratada em CSS sem `zoom`, `scale` ou ocultação global.
