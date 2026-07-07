# Checklist de homologação visual Valora

- [ ] Desktop Chrome: Home pública, topbar, footer e CTAs.
- [ ] Desktop Edge: diagnóstico, resultado e certificado.
- [ ] Tablet: menu público, cards, formulários e LGPD.
- [ ] Celular: topbar, botões grandes, escala 1 a 5 e WhatsApp.
- [ ] Login: não exibe menu admin antes de autenticação.
- [ ] Dashboard/admin: sidebar, topbar, cards, tabelas, filtros, vazios e responsividade.
- [ ] Perfis: `admin_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `participante` e `convidado_externo`.
- [ ] Resultado: score, maturidade, radar textual, benchmarking, verdade estratégica, risco, próximo nível, e-mail, WhatsApp e certificado.
- [ ] Certificado: marca real ou fallback, validação pública e sem token/hash exposto.
- [ ] LGPD: consentimento antes de responder e links de privacidade.
- [ ] Assets: sem imagem externa como logo e sem imagem quebrada quando binários faltam.

## Sprint Valora Insight™ — correções de feedback do cliente
- Produto público padronizado como `Valora Insight™`.
- Menu público deve exibir `Início`, nunca `HOME`.
- WhatsApp oficial: `+55 91 99254-5353` / `https://wa.me/5591992545353`.
- Contato público: `Fale com a Valora Group`.
- Resultado público com data segura, fallback `Data não informada`, layout mobile sem scroll horizontal e CTAs empilhados.
- Certificado/relatório com CSS de impressão compacto em `backend/Valora.Web/wwwroot/css/valora-print.css`.
- Validação: `npm run web:client-feedback-fixes`.
