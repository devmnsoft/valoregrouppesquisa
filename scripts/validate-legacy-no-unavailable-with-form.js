const {read,must}=require('./legacy-public-final-validator-common');const a=read('app.js');
must('clearPublicSurveyDomArtifacts helper exists',/function clearPublicSurveyDomArtifacts\(reason\)/.test(a));
must('unavailable clears DOM artifacts',/function renderPublicSurveyUnavailable[\s\S]*clearPublicSurveyDomArtifacts\('render_unavailable'\)/.test(a));
must('unavailable sets state unavailable with null context',/setPublicSurveyState\(\{status:'unavailable',context:null/.test(a));
must('unavailable replaces app HTML',/const app=\$\('#app'\); if\(app\)app\.innerHTML=html/.test(a));
must('runtime assert detects stale public form',/BUG: formulário público renderizado durante estado indisponível/.test(a));
