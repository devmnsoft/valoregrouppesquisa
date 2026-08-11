const fs = require('fs');
const path = require('path');
const failures=[];
const read=f=>fs.existsSync(f)?fs.readFileSync(f,'utf8'):'';
const ok=(condition,message)=>{if(!condition) failures.push(message);};
const home=read('backend/Valora.Web/Views/Home/Index.cshtml');
const publicLayout=read('backend/Valora.Web/Views/Shared/_PublicLayout.cshtml');
const adminLayout=read('backend/Valora.Web/Views/Shared/_AdminLayout.cshtml');
const nav=read('backend/Valora.Web/Navigation/NavigationCatalog.cs');
const sql=read('backend/database/postgresql/script_completo.sql');
ok(/Valora Group/.test(home),'home não apresenta a marca Valora Group');
ok(/valora-design-system\.css/.test(publicLayout),'layout público não carrega o design system');
ok(/valora-design-system\.css/.test(adminLayout),'layout administrativo não carrega o design system');
ok(['Visão Executiva','Diagnósticos','Inteligência','Administração'].every(x=>nav.includes(x)),'navegação administrativa não está agrupada');
ok(/Valora Group','valora-platform/.test(sql),'seed oficial da marca não está no SQL canônico');
ok(/5591992545353/.test(home),'WhatsApp oficial não está na home');
for(const root of ['backend/Valora.Web/Views','backend/Valora.Web/wwwroot/css','backend/Valora.Web/wwwroot/js']) walk(root).forEach(file=>{
 if(!/\.(cshtml|css|js)$/.test(file)) return;
 const text=read(file); const rel=file.replace(/\\/g,'/');
 ok(!/<img\b[^>]+src=["']https?:\/\//i.test(text),`${rel} usa imagem externa`);
 ok(!/-----BEGIN PRIVATE KEY-----|firebase-adminsdk|"type"\s*:\s*"service_account"/i.test(text),`${rel} contém padrão de segredo`);
});
function walk(dir){if(!fs.existsSync(dir))return[];return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>{const f=path.join(dir,e.name);return e.isDirectory()?walk(f):[f];});}
if(failures.length){console.error('validate-valora-rc2-visual-readiness: FAIL\n'+failures.join('\n'));process.exit(1);}console.log('validate-valora-rc2-visual-readiness: PASS');
