# Pendências reais da fundação

- Executar clean, restore e build em host com .NET SDK.
- Subir API e Web contra PostgreSQL e registrar o smoke autenticado completo.
- Executar o script duas vezes em banco novo e em cópia de banco existente; este contêiner não possui `psql`.
- Confirmar com conta bootstrap login válido/inválido/inativo e auditoria produzida.
- Confirmar visualmente Dashboard e System Health, incluindo estados degradados, em navegador; não houve screenshot porque a aplicação não pôde ser iniciada sem `dotnet`.
- Homologar leitura e marcação individual/coletiva de notificações.
- Validar backup, SMTP e geradores PDF no ambiente alvo; permanecem honestamente `not_configured`/`disabled` até receberem configuração externa.

Próxima etapa recomendada: executar a matriz de startup/SQL acima em ambiente de integração provisionado e corrigir somente falhas observadas antes de iniciar módulos adicionais.
