const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
for(const x of ['function isExternalBrowserNoise','A listener indicated an asynchronous response by returning true','the message channel closed before a response was received','chrome-extension://','recordExternalBrowserNoise',"window.addEventListener('unhandledrejection'",'e.preventDefault()'])if(!s.includes(x))throw new Error('filtro de ruído externo ausente: '+x);
if(!/if\(isExternalBrowserNoise\(reason\)\)\{recordExternalBrowserNoise\(reason\);e\.preventDefault\(\);return false;\}/.test(s))throw new Error('unhandledrejection não ignora ruído externo conhecido antes do handler real');
console.log('legacy external browser noise: PASS');
