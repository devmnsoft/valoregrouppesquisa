const fs=require('fs');
const path=require('path');
const root=process.cwd();
const sourceFiles=['app.js','functions/index.js','report-service.js','style.css','communication-gateway/src/templates/result-email-template.js','package.json'];
const existing=sourceFiles.filter(f=>fs.existsSync(path.join(root,f)));
const read=f=>fs.readFileSync(path.join(root,f),'utf8');
const combined=existing.map(f=>`\n/* ${f} */\n${read(f)}`).join('\n');
const fail=[];
function ok(cond,msg){if(!cond)fail.push(msg)}
function no(re,msg){ok(!re.test(combined),msg)}
function has(re,msg){ok(re.test(combined),msg)}
no(/Baixar certificado/,'Não pode existir "Baixar certificado" no código fonte operacional.');
no(/Certificado simples/,'Não pode existir "Certificado simples" no código fonte operacional.');
has(/Entrar no Valora Insight™/,'Login deve conter título oficial.');
no(/Valora Pulse/i,'Marca legada não pode existir no código operacional.');
has(/Acesse a gestão do Valora Insight™\./,'Login deve conter subtítulo oficial.');
has(/Fale com o Valora Group/,'CTA oficial de WhatsApp ausente.');
no(/Fale com a Valora Group/,'CTA antigo encontrado.');
['buildPublicResultUrl','adminCreateResultShareLink','preparePublicSurveyLink','requestNewResultLink','radarBarPdfSafe','toPdfSafeText','renderAdminResponsesMobileCards','shareResultWhatsapp','shareSurveyWhatsapp'].forEach(name=>has(new RegExp(name.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')),`${name} ausente.`));
has(/CERTIFICATE_FEATURE_ENABLED\s*=\s*true/,'CERTIFICATE_FEATURE_ENABLED deve estar ativo.');
const pdf=read('report-service.js');
ok(!/[█░]/.test(pdf),'PDF não pode usar blocos Unicode.');
no(/\? Estruturada|\? Em estruturação|\? Crítico|\? Alta maturidade/,'Artefato de ? em enquadramento encontrado.');
no(/\?{8,}/,'Sequência ???????? encontrada.');
const actions=(combined.match(/function createActions\(\)\{return \{[\s\S]*?\n\};/ )||[''])[0];
['openWhatsapp','shareResultWhatsapp','sendResultWhatsapp','whatsappResult','adminShareResultWhatsapp','shareSurveyWhatsapp','sendSurveyWhatsapp','whatsappSurvey','adminShareSurveyWhatsapp','goLogin','openLogin','login','adminViewResponse','adminReportResponsePdf','adminAnonymizeResponse','adminDeleteResponse','sendResultEmail'].forEach(a=>ok(actions.includes(a),`Handler ${a} ausente em createActions.`));
const dist=path.join(root,'dist');
if(fs.existsSync(dist)){
  const files=[]; const walk=d=>fs.readdirSync(d,{withFileTypes:true}).forEach(e=>{const p=path.join(d,e.name); if(e.isDirectory())walk(p); else files.push(p);}); walk(dist);
  const distText=files.filter(f=>/\.(html|js|css)$/.test(f)).map(f=>fs.readFileSync(f,'utf8')).join('\n');
  ok(!/Baixar certificado|Certificado simples|Fale com a Valora Group|\? Estruturada|\?{8,}/.test(distText),'Dist contém texto proibido.');
  ok(/app\.[a-f0-9]{8,}\.js|style\.[a-f0-9]{8,}\.css|assets\/.*[a-f0-9]{8,}/.test(distText)||files.some(f=>/[a-f0-9]{8,}\.(js|css)$/.test(path.basename(f))),'Dist não contém hash novo nos assets.');
}else{
  console.warn('WARN: dist ausente; execute npm run build:prod e rode novamente.');
}
if(fail.length){console.error(fail.map(x=>'FAIL: '+x).join('\n'));process.exit(1)}
console.log('validate-final-regression-lockdown: PASS');
