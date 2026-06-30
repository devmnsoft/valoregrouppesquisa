const {read,assert,done}=require('./public-boot-validator-lib'); const app=read('app.js');
assert(/window\.ValoraRuntimeCache=window\.ValoraRuntimeCache/.test(app),'cache runtime ausente');
assert(/featuredHomeSurveyPromise=Promise\.race/.test(app),'getFeaturedHomeSurvey sem timeout/cache');
assert(/catch\(error=>\{r\.featuredHomeSurveyError=error/.test(app),'getFeaturedHomeSurveyUrl sem catch controlado');
assert(/releasePublicUi\('renderHome'\)/.test(app),'renderHome não libera UI');
assert(!/const featuredResolution=resolveHomeFeaturedSurvey\(state\);/.test(app),'renderHome ainda resolve Firestore/estado diretamente');
done('validate-home-no-featured-crash');
