const {ok,app}=require('./_legacy-final-validators');
ok(/Como cada pessoa usa o diagnóstico/.test(app),'home journey title updated');
ok(/Equipe Valora Group responsável/.test(app)&&/Pessoa que responde ao diagnóstico/.test(app),'persona card texts updated');
ok(!/Pesquisa gratuita da Home: diagnóstico público/.test(app),'old Home survey text removed');
