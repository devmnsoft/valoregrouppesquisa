const {read,must}=require('./legacy-public-final-validator-common');const a=read('app.js');
must('success handler exists',/function handlePublicSubmitSuccess\(result,payload\)/.test(a));
must('diagnostics saved',/lastSubmitSuccess/.test(a));
must('result route written',/\?result=\$\{encodeURIComponent\(result\.responseId\)\}&rt=\$\{encodeURIComponent\(result\.resultToken\)\}/.test(a));
must('render result called',/renderResult\(result\.responseId,true,result\.resultToken,result\)/.test(a));
