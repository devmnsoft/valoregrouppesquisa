const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){console.error(m);process.exit(1)}
const actions=s.slice(s.indexOf('function createActions'),s.indexOf('function createFormHandlers'));
['reloadPublicResult','downloadCertificatePdf','downloadCertificatePng'].forEach(x=>{if(!actions.includes(x))fail(`${x} ausente em createActions`)});
if(!/function resendPublicResultEmailSafe[\s\S]*withLoading[\s\S]*catch/.test(s))fail('reenvio sem loading/tratamento');
if(!/function downloadCertificatePdf[\s\S]*withLoading[\s\S]*catch/.test(s))fail('download PDF sem loading/tratamento');
console.log('OK result actions safe');
