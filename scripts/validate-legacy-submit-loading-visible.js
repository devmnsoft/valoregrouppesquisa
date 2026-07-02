const {read,assert,ok}=require('./legacy-public-submit-validator-lib');const app=read('app.js'), css=read('style.css');
assert(app.includes('withLoading(\'Aguarde, estamos processando sua requisição...\'')||app.includes('withLoading("Aguarde, estamos processando sua requisição..."'),'submit sem withLoading correto');
assert(app.includes('globalLoadingOverlay')&&css.includes('.global-loading-overlay')&&css.includes('global-loading-card'),'overlay loading ausente');
assert(css.includes('valoraSpin')&&css.includes('.btn-spinner'),'spinner loading ausente');ok('loading visível no submit');
