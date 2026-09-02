# Auditoria UX das páginas internas

Auditoria estática de **190 páginas renderizáveis** em `Views` (partials compartilhados são avaliados separadamente como componentes). O inventário completo e rastreável está em [`internal-pages-ux-audit.csv`](internal-pages-ux-audit.csv).

## Resultado da triagem

| Classificação | Quantidade | Próxima ação |
|---|---:|---|
| Boa | 10 | Preservar o padrão e validar em uso real. |
| Precisa refinamento | 128 | Adotar `PageHeader`, ajuda contextual e estados padronizados. |
| Bagunçada | 52 | Priorizar revisão estrutural e ação principal. |

## Critérios reproduzíveis

- **Boa:** usa o cabeçalho compartilhado, ajuda contextual, ação e estado/mensagem.
- **Precisa refinamento:** possui título e ação, mas ainda não reúne toda a composição premium.
- **Bagunçada:** não apresenta simultaneamente título identificável e ação visível na marcação estática.
- As colunas auxiliares identificam formulários, ações, mensagens/empty states e CSS centralizado; “sem formulário” não é defeito em páginas somente de leitura.
- A inspeção de execução em desktop, tablet e celular continua obrigatória antes da homologação, pois conteúdo e permissões alteram a renderização.

## Correções transversais desta sprint

1. CSS inline foi removido das views e consolidado em `valora-pages.css`, eliminando escapes Razor `@@media`.
2. Formulários ganharam shell, grid, feedback inválido, ações fixas e estado de envio compartilhados.
3. Tabelas, diálogos, cabeçalhos, ações e cards receberam proteções responsivas para 900, 600 e 390 px.
4. O enhancement compartilhado impede envio nativo inválido, anuncia a mensagem padrão e bloqueia submissões duplicadas.

## Validação manual recomendada

Percorrer, com perfis e organizações reais: Login → Dashboard → Forms → Diagnostics/Surveys → Results → Reports → Certificates → ActionCenter → Evolution/Journey → Indicators/Benchmarks → Administration/Plans. Conferir teclado, erros por campo, filtros sem resultados, confirmação crítica e retorno do histórico do navegador.
