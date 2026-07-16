const fs = require('fs');
function readApp(){return fs.readFileSync('app.js','utf8');}
function readCss(){return fs.readFileSync('style.css','utf8');}
function fail(msg){console.error(msg);process.exit(1);}
function count(haystack,needle){return (haystack.match(new RegExp(needle.replace(/[.*+?^${}()|[\]\\]/g,'\\$&'),'g'))||[]).length;}
function functionBlock(src,name){const re=new RegExp('function\\s+'+name+'\\s*\\(');const m=re.exec(src);if(!m)return '';const start=m.index;const rest=src.slice(start+m[0].length);const next=rest.search(/\nfunction\s+[A-Za-z0-9_$]+\s*\(/);return next<0?src.slice(start):src.slice(start,start+m[0].length+next);}
module.exports={readApp,readCss,fail,count,functionBlock};
