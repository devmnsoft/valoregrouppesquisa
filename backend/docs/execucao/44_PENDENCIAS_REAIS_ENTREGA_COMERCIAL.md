# Pendências reais da entrega comercial

- Implementar a fila de envio somente quando houver armazenamento recuperável, consentido e protegido dos destinatários e SMTP válido.
- Integrar templates customizados aos controllers e à interface, incluindo sanitização HTML.
- Concluir services e endpoints genéricos de compartilhamento e acesso público para cada entidade autorizada.
- Substituir o exportador legado de payload vazio por consultas agregadas autorizadas antes de disponibilizar novos escopos.
- Configurar geração real de PDF/Excel e armazenamento de objetos antes de habilitar downloads.
- Executar build e smoke do ambiente com .NET SDK, PostgreSQL e credenciais locais válidas; o container desta execução não contém o comando `dotnet`.
- Validar visualmente o Workspace em navegador autenticado após disponibilizar o runtime.
