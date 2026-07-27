# Modelo de segurança da Fase 2

Segredos pertencem a variáveis de ambiente. O detector diferencia PEM com material real de regex/documentação sanitizada e continua bloqueando senha SMTP literal. Tokens persistidos são hashes. Tenant deve vir de claims validadas, com policy no controller e escopo novamente no service. O catálogo nega capabilities não contratadas explicitamente. Testes de IDOR, CSRF, brute force, reuse de refresh token e mass assignment ainda são gates pendentes.
