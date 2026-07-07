const fs=require('fs');const s=fs.readFileSync('app.js','utf8');const fail=[];
['function normalizeActionName','lastEmptyActionClick','Clique sem ação ignorado','if(el?.tagName===\'A\'&&el.href)return true'].forEach(k=>{if(!s.includes(k))fail.push(`ausente: ${k}`)});
if(/data-action=["']\s*["']/.test(s)) fail.push('template ainda contém data-action vazio');
if(/Ação não registrada: \$\{actionName\}/.test(s)&&!/if\(!actionName\)/.test(s)) fail.push('ação vazia ainda pode cair em missingAction');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-legacy-empty-action-ignored: PASS');
