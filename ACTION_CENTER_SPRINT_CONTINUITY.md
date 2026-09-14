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
