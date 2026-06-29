# Limitações conhecidas antes de produção

- Validar SMTP real com credenciais de produção antes do cutover.
- Executar Playwright em ambiente com browsers instalados e sem bloqueio de rede.
- Homologar permissões reais de `empresa_admin` com claims de organização.
- Configurar observabilidade externa para falhas recorrentes de e-mail.
