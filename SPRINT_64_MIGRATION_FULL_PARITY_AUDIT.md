# Auditoria Sprint 64 — Paridade completa legado para Web/API

Atualizado na Sprint 64.

## Resumo
- Mantém o legado Firebase ativo até a virada final.
- Registra o script único `scriptbd_completo.sql` e a cópia em `database/postgresql/scriptbd_completo.sql`.
- Documenta paridade progressiva do Valora.Api/Valora.Web com o legado.
- Segredos SMTP devem ficar fora do repositório.

## Comandos úteis
```bash
psql -U postgres -d postgres -f scriptbd_completo.sql
psql "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123456" -f scriptbd_completo.sql
npm run db:scriptbd-completo
npm run security:no-secrets
```

## Riscos antes da virada
- Validar em PostgreSQL real de homologação antes de produção.
- Executar testes E2E com navegadores instalados.
- Configurar SMTP por User Secrets, variável de ambiente ou Firebase Secret Manager; nunca commitar senha.

## Respostas objetivas
1. Módulos legados: dashboard, clientes, financeiro, planos, usuários, funcionários, formulários, pesquisas, convites, respostas, relatórios, certificados, suporte, LGPD, integrações, exportações, backup, logs, diagnósticos gratuitos, operação assistida e comunicações.
2. Valora.Web já possui Home, Login, Dashboard, Organização, Planos, Usuários, Formulários, Pesquisas, Links públicos, Pesquisa pública, Resultados, Certificados, Respostas, Comunicações, Operações, Status, Auditoria e Configurações.
3. Valora.Api já expõe controladores de auth, organizações, planos, pesquisas, respostas, certificados, comunicações, operações, públicos, health e migração.
4. Endpoints incompletos: rotas administrativas finas de suporte, relatórios avançados e alguns painéis financeiros permanecem como gaps controlados.
5. Backend espera tabelas core de organizações, usuários, planos, módulos, formulários, pesquisas, respostas, resultados, certificados, comunicações, auditoria e operações.
6. O script único passa a declarar todas as tabelas mínimas listadas para bootstrap local.
7. Legado ainda depende de Firebase direto para autenticação, hosting, Firestore e Cloud Functions.
8. API precisa manter autenticação, cadastros, pesquisas públicas, respostas, resultados, certificados, e-mail, auditoria, suporte e operações.
9. Token da pesquisa gratuita não deve expirar no backend novo quando a pesquisa oficial é válida.
10. SurveyRepository foi ajustado para não bloquear a pesquisa gratuita oficial por expires_at vencido.
11. Certificado existe no novo Web/API com controllers/views e tabelas no script.
12. E-mail de resultado existe no novo Web/API com fila `email_jobs` e templates.
13. Menu mobile legado usa `legacy-admin-mobile-menu-bridge.js`.
14. Valora.Web possui offcanvas mobile e bridge `admin-mobile-menu.js`.
15. Bloqueios de virada: homologação real PostgreSQL/SMTP/E2E, cobertura financeira avançada e suporte completo.
