# Arquitetura do Valora Insight™

## Estado implementado

A solução `backend/Valora.sln` adota arquitetura em camadas e mantém o sistema legado na raiz apenas como ponte de migração:

- **Valora.Domain**: entidades, enums, value objects, políticas puras e o catálogo metodológico canônico. Não depende de persistência ou ASP.NET.
- **Valora.Application**: casos de uso, DTOs/read models, contratos de repositório e serviços, autorização e orquestração da inteligência organizacional.
- **Valora.Infrastructure**: PostgreSQL/Dapper, repositórios, e-mail, arquivos, exportação, integrações e adaptadores concretos.
- **Valora.Api**: REST/BFF, autenticação JWT, autorização por permissão, middleware de correlação/erro e processamento hospedado.
- **Valora.Web**: MVC/Razor, BFF server-side, shell autenticado, portal público, catálogo de navegação, componentes e design system.
- **Valora.Tests**: contratos arquiteturais, SQL, materialização Dapper, rotas, permissões e regras metodológicas.

## Fluxo organizacional

`Diagnóstico → Respostas válidas → Evidências rastreáveis → Scores ponderados → Índices → Inferências → Prioridades → Actions → Journey → Entregáveis`.

A organização e o ciclo diagnóstico são fronteiras de agregação. Controllers não calculam metodologia: delegam aos serviços de aplicação, que usam regras puras do domínio. A infraestrutura sempre filtra o tenant (`organization_id`) e preserva histórico por timestamps/soft delete quando o contrato da tabela os prevê.

## Dependências permitidas

`Web/Api → Application → Domain` e `Infrastructure → Application + Domain`. Domain nunca referencia Application, Infrastructure, Api ou Web. Contratos ficam em Application; implementações externas ficam em Infrastructure.

## Processamento e segurança

O worker hospedado processa jobs de inteligência com estados, tentativas e erro persistido. O BFF não entrega tokens ao navegador. Permissões canônicas são resolvidas pelo `ValoraAccessCatalog`; planos fornecem capabilities adicionais, mas não substituem autorização. Resultados e certificados públicos exigem token/código próprios. `correlation_id` acompanha falhas e operações auditáveis.

## Decisões desta revisão

- Os doze índices oficiais e as quatro faixas oficiais passaram a ter fonte única em Domain.
- Scoring e inteligência usam a mesma classificação, eliminando escalas concorrentes.
- O SQL canônico continua sendo a origem de bootstrap/evolução defensiva e os testes estáticos protegem colunas, constraints e seeds críticos.
