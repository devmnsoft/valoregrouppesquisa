# Relatório de Bugs de Homologação — RC2

| ID | Severidade | Área | Cenário | Esperado | Atual | Causa | Correção | Status | Evidência |
|---|---|---|---|---|---|---|---|---|---|
| HML-RC2-001 | Média | Web segurança | `npm run web:no-sensitive-ui` | Validador sem alerta de UI sensível | Falha por uso de `<pre>` e termos técnicos proibidos em views operacionais | Markup genérico de payload/resultado e textos contendo termos sensíveis em inglês | Substituído `<pre>` por `<div>` estilizado e removidos termos `connection string`/`stack trace` das mensagens da UI | corrigido | Validador executado novamente |
| HML-RC2-002 | Alta | Ambiente | `dotnet restore/build/test` | SDK .NET disponível | `dotnet: command not found` | Container sem SDK .NET | Documentado; requer ambiente com SDK .NET 8 para homologação runtime final | pendente ambiente | `dotnet --info` |
| HML-RC2-003 | Alta | Banco | PostgreSQL local descartável | Docker/PostgreSQL disponível | `docker: command not found` | Container sem Docker | Documentado; requer ambiente de homologação com Docker ou PostgreSQL instalado | pendente ambiente | `docker --version` |
