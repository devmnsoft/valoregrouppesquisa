# Novo Sistema SaaS V5 — diagnóstico e entrega

## Auditoria objetiva

O novo sistema já possuía API e BFF autenticado, serviços de organização, estrutura, usuários, permissões, planos, pesquisas, respostas, resultados, relatórios, certificados e auditoria. As views Razor já adotavam design system e navegação orientada por papéis, mas a experiência estava fragmentada: não havia rotas próprias para comparativos, recomendações e plano de ação; a organização não era apresentada como central da empresa; e o dashboard não orientava o próximo passo. O legado Firebase/JavaScript não foi alterado.

## O que foi evoluído

- Regra testável de **Saúde da Conta**, com dez critérios, score, quatro faixas e ações contextuais com rotas reais.
- Política de entitlement para comparativos: Gratuito, Profissional, Corporativo e Enterprise.
- **Minha Empresa** com central SaaS, atalhos operacionais e contato oficial `+55 91 99254-5353`.
- Dashboard com saúde, pendências e card **O que fazer agora**.
- Novas telas autenticadas de Comparativos, Recomendações e Plano de Ação (Kanban), além de filtros responsivos e modal comercial.
- Menu reorganizado com Visão Geral, Minha Empresa, Comparativos, Recomendações, Plano de Ação e Relatórios, preservando a filtragem existente por papéis e módulos.
- Layout V5 responsivo: cards, filtros, diálogo, Kanban horizontal no mobile e atalhos compactos.

## Telas alteradas

`/Dashboard`, `/Organization`, `/Comparativos`, `/Recomendacoes` e `/PlanoDeAcao`. As centrais existentes de `/Reports` e `/Certificates` permanecem funcionais e ganharam acesso direto no agrupamento de inteligência.

## Regras e segurança

As novas rotas exigem autenticação. A navegação continua limitada pelos papéis do catálogo; os dados operacionais continuam obtidos pelo BFF/API com escopo organizacional. Comparativos por setor começam no Profissional, por unidade no Corporativo e multiempresa apenas no Enterprise. Botões principais apontam para rotas existentes ou para o WhatsApp oficial.

## Como testar

1. Execute `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln` e `dotnet test backend/Valora.sln`.
2. Inicie API e Web conforme `backend/README.md`, autentique um usuário empresarial e abra as cinco rotas acima.
3. Em viewport de 390 px, abra o menu, os filtros e o Kanban; confirme rolagem horizontal sem estouro da página.
4. Em Comparativos, acione um recurso premium e confirme o modal e o link oficial do WhatsApp.
