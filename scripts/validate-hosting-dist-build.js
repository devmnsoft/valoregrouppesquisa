const fs=require('fs'),path=require('path');
if(!fs.existsSync('dist'))throw new Error('dist ausente; execute npm run build:prod');
function walk(d,p=''){return fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(d,e.name),path.join(p,e.name)):[path.join(p,e.name)]);} const files=walk('dist');
if(!files.includes('index.html'))throw new Error('dist sem index.html');
if(!files.some(f=>/(^|\/)app\.[a-f0-9]+\.js$|(^|\/)app\.js$/.test(f)))throw new Error('dist sem app javascript versionado');
if(!files.some(f=>/(^|\/)style\.[a-f0-9]+\.css$|(^|\/)style\.css$/.test(f)))throw new Error('dist sem style css versionado');
const html=fs.readFileSync(path.join('dist','index.html'),'utf8');
if(/survey_demo|empresa-exemplo|tokenHash=/.test(html))throw new Error('dist contém link demo proibido');
console.log('hosting dist build: PASS');
