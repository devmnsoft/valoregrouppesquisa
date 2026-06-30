#!/usr/bin/env node
const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1;}}
assert(/routeParamsFromLocation\(\).*#\(admin\|dashboard\|forms\|surveys\|settings\|clients\|users\)/s.test(app),'routeParamsFromLocation must ignore public params on admin/private hashes');
assert(/function shouldProcessPublicSurveyParams/.test(app)&&/isPrivateRoute\(route\)\)return false/.test(app),'shouldProcessPublicSurveyParams helper missing');
assert(/Carregando sessão segura/.test(app),'private auth timeout secure loading message missing');
assert(!/Auth demorou mais de 5s; liberando modo público visitante\."/.test(app),'unexpected legacy auth timeout string form');
if(!process.exitCode) console.log('Public params/admin route validation passed.');
