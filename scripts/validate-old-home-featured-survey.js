const fs=require('fs');
const path=require('path');
function read(file){return fs.existsSync(file)?fs.readFileSync(file,'utf8'):'';}
function fail(msg){errors.push(msg);}
const errors=[];
const app=read('app.js');
const repo=read('firebase-repository.js');
const build=read('scripts/build-production.js');
const pkg=JSON.parse(read('package.json')||'{}');
const home=(app.match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)||[''])[0];
if(!home)fail('renderHome não encontrado como fonte da home antiga.');
if(/survey_demo/.test(home))fail('CTA/bloco da home contém survey_demo.');
if(/empresa-exemplo/.test(home))fail('CTA/bloco da home contém empresa-exemplo.');
if(/aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d|demo-token|tokenHash=/.test(home))fail('CTA da home contém token demo ou tokenHash hardcoded.');
if(!/async function resolveFeaturedHomeSurvey\(/.test(app)||!/async function getFeaturedHomeSurveyUrl\(/.test(app))fail('resolveFeaturedHomeSurvey/getFeaturedHomeSurveyUrl ausente em app.js.');
if(!/resolveFeaturedHomeSurveyPublic/.test(repo)||!/featuredOnHome/.test(repo)||!/isFeatured/.test(repo))fail('firebase-repository não consulta destaque featuredOnHome/isFeatured.');
if(!/resolveFeaturedHomeSurveyPayload/.test(app)||!/legacy_demo_payload_blocked/.test(app)||!/isDemoPublicSurveyLink\(submitPayload\)/.test(app))fail('submit ainda pode enviar payload legado survey_demo/empresa-exemplo.');
if(!/currentUser\(\)\|\|null/.test(app)||!/value="\$\{esc\(user\?\.email/.test(app))fail('usuário logado não parece ser aceito/preenchido na pesquisa pública.');
if(!/publicSurveyCache\.set\(linked\.id/.test(app))fail('payload resolvido da pesquisa destacada não é anexado ao cache público.');
if(!/"public"\s*:\s*"dist"/.test(read('firebase.json')))fail('Firebase Hosting não está configurado para publicar dist.');
if(!/localScripts/.test(build)||!/indexPath/.test(build)||!/config\.production\.js/.test(build)||!/configFileForDist/.test(build))fail('build-production não empacota scripts locais, index.html e config.production.js como config.js.');
if(pkg.scripts?.['home:old-featured-survey']!=='node scripts/validate-old-home-featured-survey.js')fail('script npm home:old-featured-survey ausente/incorreto.');
if(fs.existsSync('dist')){
  const distIndex=read('dist/index.html');
  const asset=(fs.existsSync('dist/assets')?fs.readdirSync('dist/assets').find(f=>/^app\..*\.js$/.test(f)):'');
  const distApp=asset?read(path.join('dist','assets',asset)):read('dist/app.js');
  if(!distApp||!distIndex)fail('dist não contém bundle app.*.js e index.html.');
  const distHome=(distApp.match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)||[''])[0];
  if(distHome&&/survey_demo|empresa-exemplo|demo-token|tokenHash=/.test(distHome))fail('dist contém CTA legado/demo na home.');
}
if(errors.length){console.error(errors.map(e=>`- ${e}`).join('\n'));process.exit(1);}console.log('validate-old-home-featured-survey: PASS');
