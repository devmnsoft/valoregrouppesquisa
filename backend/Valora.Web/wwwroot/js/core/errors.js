window.formatFriendlyError=function(e){const id=e?.correlationId||e?.traceId||'';return `${e?.message||'Não foi possível concluir a operação.'}${id?' (correlationId: '+id+')':''}`};
