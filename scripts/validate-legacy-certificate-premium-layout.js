const {ok,app,pdf}=require('./_legacy-final-validators');
ok(/safeBuildCertificateData/.test(app)&&/Valora Insight™ — Diagnóstico Estratégico/.test(app),'certificate data uses Insight');
ok(!/Valora Pulse™/.test(pdf),'public pdf certificate does not use Pulse');
ok(/const W=842,H=595/.test(pdf)&&/Valora Insight™/.test(pdf),'certificate is landscape and Insight branded');
