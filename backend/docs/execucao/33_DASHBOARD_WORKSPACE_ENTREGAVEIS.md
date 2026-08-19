# Dashboard e Workspace de entregáveis

Dashboard e workspaces reutilizam os endpoints de Inteligência Organizacional. O fluxo de criação por Insight agora envia `insightId` e `sourceType=insight`, permitindo que a API valide e preserve a evidência rastreável. O gateway `/bff/intelligence/{resource}` encaminha as rotas autenticadas sem substituir a autorização da API.
