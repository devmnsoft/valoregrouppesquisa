const {read,assert,done}=require('./public-boot-validator-lib'); const repo=read('firebase-repository.js'); const app=read('app.js');
assert(/if\(!session\.authUser\)\{[\s\S]*public_firestore_blocked[\s\S]*return \{ok:false,url:''/.test(repo),'resolveFeaturedHomeSurveyPublic deve bloquear fallback Firestore sem auth');
assert(/if\(!session\.authUser\)return \{ok:false,errorCode:err\?\.code\|\|'public_validation_unavailable'/.test(repo),'validatePublicSurveyPublic não deve lançar/consultar Firestore sem auth');
assert(!/onSnapshot\(/.test(repo+app),'onSnapshot privado encontrado sem validação explícita');
done('validate-no-private-firestore-before-auth');
