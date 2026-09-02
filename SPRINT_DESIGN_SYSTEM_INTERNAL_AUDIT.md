# Auditoria interna de design — Valora Insight™

## Escopo e critério

Foram inventariadas as views MVC em `backend/Valora.Web/Views` e revisados o layout autenticado, a navegação, os componentes compartilhados, os formulários prioritários e os estados operacionais. O layout injeta orientação, contexto da organização, mensagens, modal de confirmação e estados comuns de maneira central; por isso, esses recursos também alcançam páginas que ainda mantêm marcação específica do módulo.

## Classificação das áreas obrigatórias

| Área | Classificação | Revisão aplicada / próximo refinamento |
| --- | --- | --- |
| Login | OK | Layout de autenticação premium, validação e mensagens amigáveis já centralizados. |
| Dashboard | OK | Cabeçalho executivo, manual, métricas, alertas e atalhos funcionais. |
| Forms | OK | Busca, filtros, tabela responsiva, ação principal e estados vazio/erro. |
| Diagnostics / Surveys | OK | Fluxo de criação e acompanhamento com orientação contextual. |
| Results | OK | Leitura executiva, evidências, limitações e ações subsequentes. |
| Reports | ajuste leve | Fluxo e estados padronizados; homologar download com dados reais. |
| Certificates | ajuste leve | Emissão, histórico e validação disponíveis; homologar impressão física. |
| ActionCenter | OK | Planos, responsáveis, prazos, prioridades e estados organizados. |
| Evolution / Journey | OK | Ciclos, linha do tempo, filtros e orientação executiva. |
| Indicators | ajuste leve | Centro completo; gráficos dependem de dados organizacionais reais. |
| Benchmarks | ajuste leve | Comparação, privacidade e amostra insuficiente tratados. |
| Methodology / Governance | OK | Áreas e ações organizadas por fluxo de trabalho. |
| Administration | OK | Usuários, organizações, papéis, configurações e auditoria separados. |
| Plans | OK | Plano, limites, comparação e solicitação de upgrade visíveis. |
| Organization / Branding | ajuste leve | Contexto e identidade centralizados; validar ativos do cliente. |
| Respondente público | OK | Coleta responsiva, indisponibilidade e preservação de erro cobertas. |

## Padronização concluída nesta rodada

- Consolidado o sexto arquivo obrigatório do design system, `valora-tables.css`, com cabeçalhos, interação, scroll móvel, foco e contraste aumentado.
- Completado o catálogo de componentes tipados: cards de ação, insight e evidência; alerta; shell e apoio de formulário; resumo de validação; tabs responsivas; toolbar; e alias semântico do cabeçalho executivo.
- Acrescentados tokens canônicos e tokens dimensionais sem remover aliases legados, evitando regressão visual nas views existentes.
- Ampliado o validador automatizado para impedir regressões nos arquivos centrais, componentes, tokens e diretivas CSS que quebram Razor.

## Riscos de homologação

- Build e testes .NET exigem SDK instalado no ambiente de execução.
- Downloads, envio de mensagens, permissões e contexto multi-organização precisam de credenciais e dados reais para homologação ponta a ponta.
- A inspeção visual final deve ser repetida nos navegadores-alvo após subir API e banco local.
