const fs=require('fs');
function fail(msg){console.error(msg);process.exit(1)}
const app=fs.readFileSync('app.js','utf8');
const repo=fs.readFileSync('firebase-repository.js','utf8');
const init=fs.readFileSync('firebase-init.js','utf8');
if(/handleLoginSubmit[\s\S]{0,1200}route\(['"]dashboard['"]\)/.test(app)) fail('handleLoginSubmit voltou a navegar para route("dashboard").');
['resolvePostLoginRoute','navigateAfterLogin','clearPublicRouteParamsBeforePrivateNavigation','redirectAuthenticatedPublicHashOnce','__valoraLoginInProgress'].forEach(x=>{if(!app.includes(x))fail(`${x} ausente em app.js`)});
['surveyId','certificate','resultToken'].forEach(x=>{if(!app.includes(`'${x}'`))fail(`param público ${x} não é limpo`)});
['establishAuthenticatedSession','sessionPromises','isTransientAuthProfileError','isTerminalAuthProfileError','getIdTokenResult(false)','cleanValoraLocalState','session.profile=profile','session.lastError=null'].forEach(x=>{if(!repo.includes(x))fail(`${x} ausente em firebase-repository.js`)});
const loadProfileMatch=/async function loadProfile\(user\)\{[\s\S]*?\n\}/.exec(repo);
if(!loadProfileMatch)fail('loadProfile ausente em firebase-repository.js');
const loadProfileBody=loadProfileMatch[0];
if(/await\s+\w+\.set\s*\(\s*\{\s*lastLoginAt/.test(loadProfileBody)||/await\s+\w+\.update\s*\(\s*\{\s*lastLoginAt/.test(loadProfileBody)||/lastLoginAt[\s\S]{0,80}\{\s*merge\s*:\s*true/.test(loadProfileBody)){
  fail('loadProfile não pode conter escrita obrigatória de lastLoginAt.');
}
if(!/session\.claims\s*=\s*claims[\s\S]*session\.profile\s*=\s*profile[\s\S]*session\.lastError\s*=\s*null[\s\S]*recordAuthFlow\(['"]profile_loaded/.test(loadProfileBody)){
  fail('loadProfile deve definir claims/profile, limpar lastError e registrar profile_loaded após validações.');
}
['permission-denied','unauthenticated','profile-missing','inactive-user','invalid-role','missing-role-scope','unavailable','deadline-exceeded','auth/network-request-failed'].forEach(x=>{if(!repo.includes(x))fail(`classificação de erro ${x} ausente`)});
if(repo.includes('cleanFirebaseLocalState')) fail('cleanFirebaseLocalState não deve permanecer no boot/logout.');
if(/getIdToken\(true\)/.test(repo)) fail('Renovação forçada getIdToken(true) permanece no fluxo normal.');
['ValoraFirebaseAuthPersistenceReady','configureAuthPersistence','Persistence.LOCAL','Persistence.SESSION','Persistence.NONE'].forEach(x=>{if(!init.includes(x))fail(`${x} ausente em firebase-init.js`)});
if(!/__valoraLoginInProgress\|\|window\.ValoraFirebaseServices\?\.auth\?\.currentUser\|\|currentUserSafe\(\)/.test(app)) fail('timeout público deve checar login em andamento, Firebase currentUser e currentUserSafe antes de liberar visitante.');
console.log('OK mobile auth flow guards');
