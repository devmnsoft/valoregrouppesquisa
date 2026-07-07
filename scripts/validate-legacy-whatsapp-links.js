const {ok,app}=require('./_legacy-final-validators');
ok(/https:\/\/wa\.me\/\$\{WHATSAPP_NUMBER\}/.test(app)&&/5591992545353/.test(app),'WhatsApp wa.me official number');
ok(/function whatsappLink/.test(app)&&/<a class="btn btn-success" href/.test(app),'WhatsApp link is anchor');
