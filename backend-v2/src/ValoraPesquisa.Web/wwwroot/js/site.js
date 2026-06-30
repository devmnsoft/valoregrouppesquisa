(function(){
const apiBase=sessionStorage.getItem('apiBase')||'http://localhost:5000';
function token(){return sessionStorage.getItem('jwt')||''}
function msg(t,cls='info'){$('#toast').removeClass('d-none alert-info alert-danger alert-success').addClass('alert-'+cls).text(t).show();setTimeout(()=>$('#toast').fadeOut(),3500)}
function api(path,method='GET',data){return $.ajax({url:apiBase+path,method,contentType:'application/json',data:data?JSON.stringify(data):undefined,headers:token()?{Authorization:'Bearer '+token()}:{} }).fail(x=>msg(x.responseJSON?.message||'Não foi possível concluir a operação.','danger'))}
function table(sel,items,cols){if(!items?.length){$(sel).html('<div class="alert alert-light">Nenhum registro encontrado.</div>');return} let h='<div class="table-responsive"><table class="table table-sm"><thead><tr>'+cols.map(c=>'<th>'+c[0]+'</th>').join('')+'</tr></thead><tbody>'; items.forEach(i=>h+='<tr>'+cols.map(c=>'<td>'+($('<div>').text(i[c[1]]??'').html())+'</td>').join('')+'</tr>'); $(sel).html(h+'</tbody></table></div>')}
$(function(){
 const page=$('main [data-page]').data('page');
 if(page==='login') $('#loginForm').on('submit',e=>{e.preventDefault();api('/auth/login','POST',{email:$('#email').val(),password:$('#password').val()}).done(r=>{sessionStorage.setItem('jwt',r.token);msg('Login realizado.','success');location.href='/Home/Dashboard'})});
 if(page==='dashboard') api('/me').done(r=>$('#content').html('<div class="alert alert-success">Bem-vindo, '+r.user.name+'</div>'));
 if(page==='organizations') api('/organizations').done(r=>table('#content',r,[['Nome','name'],['Slug','slug'],['E-mail','email'],['Status','status']]));
 if(page==='users') api('/users').done(r=>table('#content',r,[['Nome','name'],['E-mail','email'],['Perfil','role'],['Status','status']]));
 if(page==='forms') api('/forms').done(r=>table('#content',r,[['Título','title'],['Status','status'],['Criado em','createdAt']]));
 if(page==='surveys') api('/surveys').done(r=>table('#content',r,[['Título','title'],['Status','status'],['Formulário','formId']]));
 if(page==='responses') api('/responses').done(r=>table('#content',r,[['Participante','participantName'],['E-mail','participantEmail'],['Status','status'],['Concluída','completedAt']]));
 if(page==='audit') api('/audit/events').done(r=>table('#content',r,[['Ação','action'],['Entidade','entity'],['Correlação','correlationId'],['Data','createdAt']]));
 if(page==='public-survey'){const q=new URLSearchParams(location.search);api('/public/surveys/validate?surveyId='+q.get('surveyId')+'&token='+encodeURIComponent(q.get('token')||'')+'&org='+q.get('org')).done(r=>{let h='<h2>'+r.survey.title+'</h2><form id="respForm">';r.survey.questions.forEach(x=>h+='<div class="mb-3"><label class="form-label">'+x.text+'</label><input class="form-control" name="'+x.id+'"></div>');$('#content').html(h+'<button class="btn btn-primary">Enviar</button></form>');$('#respForm').on('submit',e=>{e.preventDefault();let ans=[];$('#respForm input').each((_,i)=>ans.push({questionId:i.name,answerValue:i.value,answerText:i.value}));api('/public/surveys/'+q.get('surveyId')+'/responses','POST',{token:q.get('token'),org:q.get('org'),answers:ans}).done(x=>location.href='/Survey/Resultado?responseId='+x.responseId+'&token='+encodeURIComponent(x.resultToken))})})}
 if(page==='public-result'){const q=new URLSearchParams(location.search);api('/public/results/'+q.get('responseId')+'?token='+encodeURIComponent(q.get('token')||'')).done(r=>$('#content').html('<div class="alert alert-success"><h2>'+r.level+'</h2><p>'+r.percentage+'% • nota '+r.normalized5+'/5</p></div>'))}
});
})();
