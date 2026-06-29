# Guia de Entregabilidade de E-mail

- **SPF**: publicar include/ip autorizado do SMTP oficial e validar com ferramenta DNS antes do cutover.
- **DKIM**: habilitar assinatura no provedor SMTP, rotacionar seletor e registrar chave pública no DNS.
- **DMARC**: iniciar com `p=none`, revisar relatórios agregados e evoluir para quarentena/rejeição.
- **Remetente oficial**: usar `Email.FromEmail` do domínio corporativo; não usar contas pessoais.
- **reply-to**: configurar caixa monitorada para respostas comerciais e suporte.
- **Limites de envio**: respeitar cotas do provedor, fila com retry e bloqueio de rajadas manuais.
- **Diagnóstico de spam**: conferir reputação, conteúdo, links, autenticação e bounces.
- **Diagnóstico SMTP**: validar host, porta, TLS, usuário e senha sem expor segredo em logs.
- **Teste real**: executar `npm run email:real-sender` em homologação com caixa controlada.
- **reenviar e-mail**: usar painel de diagnóstico gratuito ou fila operacional, sempre com justificativa.
- **Verificar fila**: usar `/admin/email-jobs` e `/admin/operations/email` sem retornar senha, host sensível ou usuário completo.
