# Pendências reais de QA

1. Executar a suíte e os publishes em agente com SDK .NET 10; ambientes sem `dotnet` não validam compilação.
2. Executar a aplicação dupla do SQL em PostgreSQL efêmero/isolado configurado explicitamente. O fluxo local não exige Docker.
3. Realizar o smoke funcional autenticado em homologação com SMTP/PDF/webhooks substituídos por sandboxes ou fakes controlados.
4. Medir cobertura por linha/branch no CI e definir baseline sem transformar percentual em substituto para asserts comportamentais.
5. Completar testes de API por permissão e organização para cada rota crítica à medida que fixtures de host isolado forem estabilizadas.

Nenhuma dessas pendências autoriza uso de produção, redução de LGPD/JWT/permissões, ou teste que dependa de serviço externo real.
