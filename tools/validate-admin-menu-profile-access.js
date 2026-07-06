#!/usr/bin/env node
const fs=require('fs');function read(p){return fs.readFileSync(p,'utf8')}function ok(c,m){if(!c){console.error('FAIL:',m);process.exit(1)}console.log('OK:',m)}
const pub=read('backend/Valora.Web/Views/Shared/_PublicLayout.cshtml');const admin=read('backend/Valora.Web/Views/Shared/_AdminLayout.cshtml');const sidebar=read('backend/Valora.Web/Views/Shared/_Sidebar.cshtml');const roles=read('backend/Valora.Web/wwwroot/js/security/role-definitions.js');
ok(!pub.includes('_Sidebar')&&!/Admin\/Dashboard|\/Admin/i.test(pub),'layout público não renderiza menu administrativo');
ok(admin.includes('_Sidebar')&&admin.includes('auth-session')&&admin.includes('guards'),'layout admin carrega sessão, guardas e sidebar');
['admin_valora','consultor_valora','empresa_admin','gestor_pesquisa','analista_resultados','gestor_area','participante','convidado_externo'].forEach(r=>ok(roles.includes(r),`perfil ${r} definido`));
ok(sidebar.includes('data-allowed-roles')||sidebar.includes('admin_valora'),'menu possui metadados de perfil');
ok(!/participante[^\n]+Dashboard|convidado_externo[^\n]+Dashboard/.test(sidebar),'participante/convidado não recebem painel administrativo estático');
