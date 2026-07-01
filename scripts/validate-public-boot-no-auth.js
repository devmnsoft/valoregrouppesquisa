const {read,assert,done}=require('./public-boot-validator-lib'); const app=read('app.js');
assert(/function isPublicRoute\(/.test(app),'isPublicRoute ausente');
assert(/state\.isPublicMode=\!state\.user/.test(app),'init não seta modo público');
assert(/Promise\.race\(\[authReadyPromise,authTimeout\]\)/.test(app)||/Promise\.race\(\[window\.ValoraFirebaseAuth\.waitUntilReady\(\),authTimeout\]\)/.test(app),'auth timeout público ausente');
assert(/releasePublicUi\('init_public_no_auth'\)/.test(app),'UI pública não liberada no init');
assert(!/\['admin','empresa','participante'\]\.includes\(view\)&&!currentUser\(\)/.test(app),'guard legado bloqueia antes de isPublicRoute');
done('validate-public-boot-no-auth');
