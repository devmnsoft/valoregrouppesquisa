#!/usr/bin/env node
const fs=require('fs'); const path=require('path');
const root=process.cwd(); const read=p=>fs.existsSync(p)?fs.readFileSync(p,'utf8'):''; const fail=[]; const ok=[]; const req=(cond,msg)=>cond?ok.push(msg):fail.push(msg);
const web='backend/Valora.Web'; const pubLayout=`${web}/Views/Shared/_PublicLayout.cshtml`; const adminLayout=`${web}/Views/Shared/_AdminLayout.cshtml`; const home=`${web}/Views/Home/Index.cshtml`;
req(fs.existsSync(pubLayout),'_PublicLayout.cshtml existe'); req(fs.existsSync(adminLayout),'_AdminLayout.cshtml existe'); req(read(home).includes('_PublicLayout'),'Home pública usa _PublicLayout');
const publicViews=[home,`${web}/Views/PublicSurvey/Take.cshtml`,`${web}/Views/Results/Public.cshtml`,`${web}/Views/Certificates/Details.cshtml`,`${web}/Views/Lgpd/Index.cshtml`,`${web}/Views/PublicPages/Contact.cshtml`,`${web}/Views/PublicPages/FreeDiagnostic.cshtml`];
req(publicViews.every(p=>!read(p).includes('_Sidebar')&&!read(p).includes('app-shell')),'Rotas públicas não usam sidebar nas views');
req(fs.existsSync(`${web}/wwwroot/css/valora-public.css`),'CSS público existe');
req(fs.existsSync(`${web}/wwwroot/js/public/valora-public.js`),'JS público existe');
const webFiles=require('child_process').execSync(`find ${web} -type f`,{encoding:'utf8'}).trim().split(/\n/).filter(Boolean); req(!webFiles.some(f=>/firebase/i.test(read(f))),'Não há Firebase em backend/Valora.Web');
const h=read(home).toLowerCase(); req(h.includes('hero'),'Home contém hero'); req(h.includes('diagnóstico gratuito')||h.includes('diagnostico gratuito'),'Home contém diagnóstico gratuito'); req(h.includes('whatsapp')||h.includes('contato'),'Home contém WhatsApp/contato'); req(read(pubLayout).includes('_PublicFooter') || h.includes('footer'),'Home contém footer público via layout'); req(h.includes('lgpd'),'Home contém LGPD');
req(fs.existsSync(`${web}/Views/Results/Public.cshtml`),'View de resultado existe'); req(fs.existsSync(`${web}/Views/Certificates/Details.cshtml`),'View de certificado existe'); req(fs.existsSync('LEGACY_PUBLIC_JOURNEY_TO_ASPNET_PARITY.md'),'Documento de paridade de jornada existe'); req(fs.existsSync('LEGACY_PUBLIC_LAYOUT_TO_ASPNET_PARITY.md'),'Documento de paridade visual existe'); req(read(pubLayout).includes('valora-public.css')&&!read(pubLayout).includes('/css/app.css'),'app.css administrativo não é o único CSS da Home pública');
if(fail.length){ console.error('Falhas public legacy parity:\n- '+fail.join('\n- ')); process.exit(1); } console.log('OK public legacy parity:\n- '+ok.join('\n- '));
