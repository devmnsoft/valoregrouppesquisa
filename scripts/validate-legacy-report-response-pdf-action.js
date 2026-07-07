const {ok,app}=require('./_legacy-final-validators');
ok(/async function reportResponsePdf/.test(app),'reportResponsePdf exists');
ok(/downloadResultReport[\s\S]*reportResponsePdf/.test(app)&&/downloadReport[\s\S]*reportResponsePdf/.test(app),'report aliases exist');
ok(/createActions[\s\S]*reportResponsePdf/.test(app),'report action mapped');
