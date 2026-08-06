# Evolução operacional, comercial e layout V4

## Auditoria inicial

O frontend continua sendo uma aplicação JavaScript progressiva, com Firebase compatível e repositórios alternáveis. A auditoria encontrou uma base funcional ampla para login, dashboard, empresas, usuários, planos, pesquisas, resultados, certificados, notificações e planos de ação. A estratégia V4 foi, portanto, **evoluir sem substituir**: preservar `app.js`, Cloud Functions e a solução .NET, adicionando um núcleo operacional puro e testável.

Pontos observados:

- o onboarding anterior misturava implantação e operação posterior, com nove itens, sem representar literalmente unidade principal, setores e link público;
- permissões legadas eram amplas por capacidade e não documentavam todas as ações da matriz solicitada;
- o catálogo tinha aliases comerciais históricos (`Essencial` e `Growth`) e precisava de uma visão normalizada dos quatro níveis V4;
- notificações, planos de ação e recomendações já possuíam implementação, mas regras importantes estavam acopladas ao arquivo principal;
- dashboard e plano já apresentavam indicadores, embora cards, modais, tabelas e login precisassem de uma camada visual mais consistente no mobile;
- mensagens técnicas ainda podem existir em rotas operacionais antigas; a camada V4 centraliza as cinco mensagens públicas obrigatórias para adoção gradual;
- o menu mobile possui bridge própria e foi preservado para evitar regressão de clique e autenticação.

## Implementação funcional

`operational-v4.js` fornece contratos sem dependência de DOM para:

1. jornada de oito etapas, progresso, próxima etapa e conversão automática em checklist opcional depois da conclusão;
2. planos Gratuito, Profissional, Corporativo e Enterprise, limites de sete recursos e resolução amigável de entitlement/upgrade;
3. matriz de Administrador Geral, Administrador da Empresa, Gestor de Unidade, Gestor de Setor, Analista e Respondente;
4. recomendações por menor dimensão, baixa adesão, diferença entre unidades e queda de score;
5. conversão de recomendação em ação com prazo, responsável, unidade, setor, status e histórico;
6. objetos de auditoria sem inclusão automática de dados pessoais ou sensíveis;
7. catálogo único de mensagens amigáveis.

A camada `operational-v4.css` refina dashboard, KPIs, onboarding, notificações, modal, login, tabelas e comportamento em telas pequenas. Ela é carregada depois do estilo legado, de forma aditiva e reversível.

## Compatibilidade e limitações

- nenhuma Cloud Function foi removida ou renomeada;
- contratos da API .NET e persistência Firebase não foram alterados;
- aliases de planos existentes são normalizados, sem migrar dados automaticamente;
- o núcleo V4 está pronto para persistência remota, mas ações continuam utilizando os repositórios legados existentes;
- comparativos dependem de unidades e respostas reais; sem dados, deve ser exibida a mensagem amigável de ausência de dados;
- envio de e-mail e WhatsApp depende da configuração do ambiente, mantendo `+55 91 99254-5353` como contato comercial oficial.

## Validação manual sugerida

1. entrar como administrador da empresa em desktop e viewport móvel;
2. conferir dashboard, checklist, central de notificações e plano contratado;
3. abrir um recurso acima do plano atual e validar modal, plano recomendado e CTA comercial;
4. gerar uma recomendação com dimensão abaixo de 3 e aceitá-la no plano de ação;
5. alternar perfis e confirmar que ações administrativas não aparecem para Analista e Respondente;
6. executar pesquisa pública, resultado e certificado para confirmar ausência de regressão.
