const {read,assert,done}=require('./public-boot-validator-lib'); const app=read('app.js');
assert(/isPublicRoute[\s\S]*params\.result/.test(app),'?result não é rota pública');
assert(/if\(result\)return renderResult\(result,false,resultToken\)/.test(app),'routeFromLocation não renderiza resultado público');
assert(/callPublicFunction\('getPublicResult'/.test(app),'resultado público não usa Function pública');
done('validate-public-result-route');
