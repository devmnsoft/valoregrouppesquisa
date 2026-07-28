'use strict';
const fs=require('fs');const path=require('path');const root=path.resolve(__dirname,'../..');const failures=[];
const read=p=>fs.readFileSync(path.join(root,p),'utf8');const check=(ok,message)=>{if(!ok)failures.push(message);};
const authLayout=read('Valora.Web/Views/Shared/_AuthLayout.cshtml');
check(!/https?:\/\//i.test(authLayout),'_AuthLayout possui asset remoto/CDN.');
for(const view of ['Login','Register','ForgotPassword','ResetPassword']){const body=read(`Valora.Web/Views/Account/${view}.cshtml`);check(body.includes('Layout = "_AuthLayout"'),`${view} não usa _AuthLayout.`);}
const authJs=read('Valora.Web/wwwroot/js/api/auth-api.js')+read('Valora.Web/wwwroot/js/pages/login-page.js');
check(!/(localStorage|sessionStorage|result\.token|Session\.save)/.test(authJs),'Autenticação do navegador persiste ou manipula token.');
for(const asset of ['lib/bootstrap/bootstrap.min.css','lib/bootstrap/bootstrap.bundle.min.js','img/illustrations/auth-insight.svg'])check(fs.existsSync(path.join(root,'Valora.Web/wwwroot',asset)),`Asset ausente: ${asset}`);
for(const file of ['Views/Shared/_AuthLayout.cshtml','Views/Account/Login.cshtml','Views/Account/Register.cshtml']){const body=read(`Valora.Web/${file}`);for(const tag of body.matchAll(/<img\b[^>]*>/gi)){check(/\balt=/.test(tag[0]),`${file}: imagem sem alt.`);check(/\bwidth=/.test(tag[0])&&/\bheight=/.test(tag[0]),`${file}: imagem sem dimensões.`);}}
if(failures.length){console.error(failures.map(x=>`- ${x}`).join('\n'));process.exit(1);}console.log('Premium template validation passed.');
