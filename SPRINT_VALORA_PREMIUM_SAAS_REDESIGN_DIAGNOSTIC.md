# Sprint Valora Premium SaaS Redesign — Diagnóstico inicial

## 1. Estado atual do layout público
A Web oficial já usa MVC/Razor em `backend/Valora.Web`, com layout público dedicado, topbar/footer parciais, Bootstrap e CSS próprio. A home possui CTA de diagnóstico gratuito, WhatsApp, LGPD e linguagem Valora Insight™.

## 2. Estado atual do layout admin
O layout administrativo existe, porém estava muito condensado em uma única linha, com baixa legibilidade, sem design system centralizado e com menu lateral duplicando itens.

## 3. Pontos visuais pobres
Cards e seções dependiam de estilos dispersos, com pouca hierarquia premium, pouco uso consistente de tokens e aparência menos SaaS.

## 4. Pontos quebrados no mobile
Risco de overflow por botões e tabelas sem wrappers padronizados; sidebar mobile existia, mas precisava de agrupamento e espaçamento.

## 5. Páginas com aparência de protótipo
Dashboard, certificado e módulos administrativos tinham textos de carregamento simples e ausência de estados vazios premium.

## 6. Problemas de sidebar/topbar
Sidebar tinha links repetidos, ausência de grupos visuais e classes inteiras em uma linha. Topbar precisava expor melhor título, breadcrumb, ambiente, usuário, perfil e ação rápida.

## 7. Problemas de dashboard
Dashboard mostrava cards genéricos e aguardando API, sem saudação executiva, KPIs estratégicos e ações rápidas.

## 8. Problemas em resultado/devolutiva
Resultado tinha estrutura correta, mas precisava de hierarquia visual executiva, score centralizado, radar textual e CTAs com respiro mobile.

## 9. Problemas em certificado/relatório
Certificado precisava de moldura premium, campos obrigatórios visíveis e CSS print compacto para evitar página branca extra.

## 10. Problemas em tabelas/forms
Tabelas e forms não tinham padrão visual SaaS central; filtros, responsividade e estados vazios precisavam ser padronizados.

## 11. Estado atual dos CSS
Existiam `valora-public.css`, `valora-admin.css`, `valora-print.css`, `app.css` e `responsive.css`, sem arquivo único de design system premium.

## 12. Estado atual dos assets da marca
Há referências para `/img/brand/valora-symbol.jpeg` e `/img/brand/valora-logo-full.jpeg`, com fallback textual quando o asset não carrega.

## 13. Estado atual dos validadores visuais
Existiam validadores de feedback, RC2, paridade pública, jornada Valora Insight™ e menu por perfil, mas faltava validador específico premium.

## 14. Estado atual do script SQL
O script completo já continha bloco de compatibilidade e seeds oficiais, mas a sprint exige reforçar validação e garantir compatibilidade modular.

## 15. Erro `plan_limits.users`
A causa é banco antigo com tabela `plan_limits` criada sem a coluna `users`; `CREATE TABLE IF NOT EXISTS` não altera tabela existente. O plano é manter `ALTER TABLE ... ADD COLUMN IF NOT EXISTS users` antes dos seeds.

## 16. Plano objetivo da sprint
Criar design system premium, carregar nos layouts, reestruturar home/admin/sidebar/topbar/dashboard/resultado/certificado/diagnóstico, reforçar validadores visual e SQL, atualizar documentação e executar gates disponíveis.
