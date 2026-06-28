# Sprint 36 — Auditoria Valora.Web UX Real

## Diagnóstico
Foram encontrados placeholders nas views Razor obrigatórias, handlers com JSON bruto e formulários com campos genéricos. As telas foram substituídas por UX específica para login, cadastro, recuperação, dashboard, planos, pesquisa pública, resultado, certificado, status, comunicação, auditoria, migração e configurações.

## Respostas objetivas
1. Views genéricas: todas as telas operacionais ASP.NET estavam em modelo único; foram refinadas.
2. Uso de texto de ação genérica: removido das views.
3. Texto Bootstrap genérico: removido das views.
4. Campos genéricos id/identificador: removidos de formulários finais.
5. Handlers sem payload real: login, cadastro, reset e pesquisa pública foram corrigidos.
6. JSON bruto: removido dos handlers de página.
7. Componentes reais: cards, estados, formulários e downloads foram adicionados.
8. Empty state: substituído por mensagem de carregamento/ativação útil.
9. Error state: `.error-state` exibe mensagem amigável e correlationId via formatador.
10. Validação real: aplicada em autenticação, cadastro, reset e pesquisa pública.
11. Endpoints reais: `/auth/*`, `/me`, `/plans/public`, `/health*`, `/public/surveys/*`, `/public/results/*`, certificados e comunicações.
12. APIs faltantes: métricas administrativas de pesquisas ativas/respostas mensais e endpoints dedicados para usuários/forms/settings ainda são gaps.
13. Endpoints existentes pouco usados: rotas administrativas dependem de contrato final e permissões.
14. Mobile: sidebar offcanvas, cards empilhados e botões touch foram reforçados.
15. Loading infinito: handlers usam `finally`.
16. Telas não finais: módulos administrativos sem contrato dedicado mostram estado de ativação controlado.
