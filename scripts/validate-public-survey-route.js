const {read,assert,done}=require('./public-boot-validator-lib'); const app=read('app.js'); const repo=read('firebase-repository.js');
assert(/isPublicRoute[\s\S]*params\.survey/.test(app),'?survey não é rota pública');
assert(/if\(sid&&token\).*renderTakeSurvey/.test(app),'routeFromLocation não renderiza pesquisa pública');
assert(/callFunction\('validateSurveyLink'/.test(repo),'validatePublicSurveyPublic não usa Function');
assert(/if\(!session\.authUser\)return \{ok:false/.test(repo),'pesquisa pública falha controlada sem auth');
done('validate-public-survey-route');
