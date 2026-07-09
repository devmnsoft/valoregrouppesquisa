const {has,read,assert}=require('./legacy-final-validator-lib');
has('app.js',/async function shareSurveyWhatsapp[\s\S]*preparePublicSurveyLink[\s\S]*token=/,'shareSurveyWhatsapp must call preparePublicSurveyLink');
const app=read('app.js');
assert(!/url\.searchParams\.set\('token',\s*[^;\n]*publicTokenHash/.test(app),'survey url must not use publicTokenHash');
console.log('ok');
