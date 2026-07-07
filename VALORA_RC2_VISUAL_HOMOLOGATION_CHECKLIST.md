# Checklist de Homologação Visual — Valora RC2

## Dispositivos e navegadores

- [ ] Desktop em Chrome.
- [ ] Desktop em Edge.
- [ ] Tablet em Chrome/Edge.
- [ ] Celular em Chrome/Edge.

## Público

- [ ] Home abre sem sidebar administrativa.
- [ ] Hero exibe logo oficial ou fallback premium.
- [ ] CTA de diagnóstico gratuito funciona.
- [ ] CTA de WhatsApp abre rota/link correto.
- [ ] CTA Entrar leva ao login.
- [ ] Seções de como funciona, 5 dimensões, devolutiva, certificado e LGPD estão legíveis.
- [ ] Diagnóstico exibe LGPD, progresso, escala 1 a 5 e bloqueio de duplo clique.
- [ ] Erros são amigáveis e não exibem token/hash.

## Resultado e certificado

- [ ] Resultado exibe marca/fallback, score, nível, radar, leitura executiva, dimensões, benchmarking e CTAs.
- [ ] Certificado exibe marca/fallback, Valora Group, participante/empresa quando disponível, pontuação, nível, código e link de validação.
- [ ] Nenhuma página pública exibe JSON bruto, token ou hash.

## Administrativo

- [ ] Login é exigido para rotas administrativas.
- [ ] Sidebar não aparece em páginas públicas.
- [ ] Topbar admin exibe usuário/ambiente/logout.
- [ ] Menu por perfil validado para `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante` e `convidado_externo`.
- [ ] Cards, badges, tabelas responsivas, estados vazios e erros amigáveis estão consistentes.

## Segurança e release

- [ ] Não há service account, private key, `firebase-adminsdk` ou `.env` real versionados.
- [ ] `npm run web:brand-assets` passa após os binários reais.
- [ ] `npm run web:rc2-visual-readiness` passa.
- [ ] Homologação com .NET SDK, PostgreSQL e comparação visual foi anexada ao RC2.
