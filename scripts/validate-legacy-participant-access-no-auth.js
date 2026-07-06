const fs=require('fs');const app=fs.readFileSync('app.js','utf8');const repo=fs.readFileSync('firebase-repository.js','utf8')+fs.readFileSync('repository.js','utf8');
for(const x of ['accessPassword: getFormValue(formEl, \'accessPassword\') || \'\'','function renderParticipantResultAccess','ValoraRepository.getParticipantResultsByPassword','getParticipantResultsByPasswordFirebase','getParticipantResultsByPassword:(email,password)=>read'])if(!(app+repo).includes(x))throw new Error('acesso participante ausente: '+x);
const participantFlow=app.slice(app.indexOf('function renderParticipantResultAccess'),app.indexOf('function validatePublicAnswers'));
if(/signInWithPassword|signInWithEmailAndPassword|renderLogin|identitytoolkit/.test(participantFlow))throw new Error('fluxo público de participante usa Auth');
console.log('legacy participant access no auth: PASS');
