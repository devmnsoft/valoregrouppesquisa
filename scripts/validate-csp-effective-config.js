const fs=require('fs');const firebase=fs.readFileSync('firebase.json','utf8'),build=fs.readFileSync('scripts/build-production.js','utf8');const fail=[];
if(!firebase.includes('Content-Security-Policy'))fail.push('CSP ausente no firebase.json');
if(!firebase.includes('https://api.valoragroup.mnsoft.com.br'))fail.push('CSP não inclui api.valoragroup para cutover controlado');
if(!firebase.includes('https://*.cloudfunctions.net'))fail.push('CSP não permite Cloud Functions');
if(!/ValoraBuildInfo/.test(build)||!/caches\.keys/.test(build)||!/serviceWorker/.test(build))fail.push('build não registra info/limpeza cache service worker');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-csp-effective-config: PASS');
