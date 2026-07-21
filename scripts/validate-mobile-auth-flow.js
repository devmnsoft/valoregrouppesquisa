const fs=require('fs');
function fail(msg){console.error(msg);process.exit(1)}
const app=fs.readFileSync('app.js','utf8');
const repo=fs.readFileSync('firebase-repository.js','utf8');
const init=fs.readFileSync('firebase-init.js','utf8');
if(/handleLoginSubmit[\s\S]{0,1200}route\(['"]dashboard['"]\)/.test(app)) fail('handleLoginSubmit voltou a navegar para route("dashboard").');
['resolvePostLoginRoute','navigateAfterLogin','clearPublicRouteParamsBeforePrivateNavigation','redirectAuthenticatedPublicHashOnce','__valoraLoginInProgress'].forEach(x=>{if(!app.includes(x))fail(`${x} ausente em app.js`)});
['surveyId','certificate','resultToken'].forEach(x=>{if(!app.includes(`'${x}'`))fail(`param público ${x} não é limpo`)});
['establishAuthenticatedSession','sessionPromises','isTransientAuthProfileError','getIdTokenResult(false)','cleanValoraLocalState'].forEach(x=>{if(!repo.includes(x))fail(`${x} ausente em firebase-repository.js`)});
if(repo.includes('cleanFirebaseLocalState')) fail('cleanFirebaseLocalState não deve permanecer no boot/logout.');
if(/getIdToken\(true\)/.test(repo)) fail('Renovação forçada getIdToken(true) permanece no fluxo normal.');
['ValoraFirebaseAuthPersistenceReady','configureAuthPersistence','Persistence.LOCAL','Persistence.SESSION','Persistence.NONE'].forEach(x=>{if(!init.includes(x))fail(`${x} ausente em firebase-init.js`)});
console.log('OK mobile auth flow guards');
