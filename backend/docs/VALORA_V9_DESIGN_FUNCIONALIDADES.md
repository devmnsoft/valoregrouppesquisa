# Valora Insight™ V9 — auditoria de design e funcionalidades

## Diagnóstico inicial

A auditoria das views MVC, scripts por página, navegação e estilos do novo sistema identificou: design system premium existente, porém não carregado no shell autenticado; dashboard sem prioridades acionáveis; listagem de pesquisas com visual técnico; ausência de rotas dedicadas para cockpit, templates, campanhas e ajuda; fluxo de implantação espalhado; e tabelas que dependiam de rolagem horizontal no mobile.

## Evoluções aplicadas

- Design system premium carregado globalmente e camada V9 com tokens, hierarquia visual, foco visível, skeletons, dialogs, KPIs, badges, timeline, gráficos e estados vazios.
- Cockpit Executivo com indicadores provenientes das APIs reais de pesquisas/respostas e geração real de relatório.
- Biblioteca oficial pesquisável com dez templates; o uso cria um formulário isolado pela organização e registra auditoria.
- Campanhas operacionais sobre pesquisas e links públicos existentes: criar rascunho, iniciar, pausar, encerrar, acompanhar respostas, copiar link, QR Code e WhatsApp.
- Dashboard com jornada guiada persistível, progresso e alertas com rotas funcionais.
- Central de ajuda com busca, atalhos e WhatsApp oficial.
- Breakpoints específicos: grades viram uma coluna, ações tornam-se compactas e dialogs ocupam a base da tela.

## Pendências recomendadas

Evoluir o modelo persistente de campanhas para armazenar meta, canais e lembretes separados da pesquisa; trocar o provedor remoto de QR Code por geração local; ampliar séries históricas do cockpit por unidade/setor; executar testes visuais automatizados com dados de homologação; e aplicar os componentes V9 gradualmente às telas administrativas menos acessadas.
