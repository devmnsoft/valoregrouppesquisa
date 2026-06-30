const fs=require('fs'); const layout=fs.readFileSync('backend/Valora.Web/Views/Shared/_Layout.cshtml','utf8')+fs.readFileSync('backend/Valora.Web/Views/Shared/_Topbar.cshtml','utf8'); const side=fs.readFileSync('backend/Valora.Web/Views/Shared/_Sidebar.cshtml','utf8'); const js=fs.readFileSync('backend/Valora.Web/wwwroot/js/admin-mobile-menu.js','utf8'); const css=fs.readFileSync('backend/Valora.Web/wwwroot/css/responsive.css','utf8');
for (const x of ['toggleWebMobileAdminMenu','mobileSidebar','/js/admin-mobile-menu.js']) if(!(layout+side).includes(x)) throw new Error(`Menu Web ausente: ${x}`);
for (const x of ['web-mobile-menu-open','aria-expanded','Escape','offcanvas']) if(!(js+css+side).includes(x)) throw new Error(`Comportamento mobile Web ausente: ${x}`);
if((side.match(/class="nav-link"/g)||[]).length<9) throw new Error('Menu Web tem menos de 9 itens.');
console.log('Menu mobile Valora.Web validado.');
