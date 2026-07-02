const {read,must}=require('./legacy-public-final-validator-common');const a=read('app.js'),c=read('style.css');
must('loading helpers exist',/function setGlobalLoading/.test(a)&&/function setButtonLoading/.test(a)&&/async function withLoading/.test(a));
must('submit loading message exists',/Aguarde, estamos processando sua requisição/.test(a));
must('loading css exists',/\.global-loading-overlay/.test(c)&&/\.global-loading-overlay\.is-active/.test(c)&&/\.global-loading-card/.test(c)&&/\.global-loading-spinner/.test(c)&&/\.btn-spinner/.test(c)&&/@keyframes valoraSpin/.test(c));
