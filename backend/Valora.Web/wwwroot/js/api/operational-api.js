(function(){
  const endpoints={Reports:'/reports/generated',Exports:'/exports',Lgpd:'/lgpd/privacy-requests',Email:'/email/status'};
  $(document).on('click','[data-load-operational]',function(){const box=$(this).closest('[data-module]'); const module=box.data('module'); const url=endpoints[module]||'/email/status'; const out=box.find('[data-result]'); out.removeClass('d-none').text('Carregando...'); $.ajax({url,method:'GET'}).done(r=>out.text(JSON.stringify(r,null,2))).fail(()=>out.text('Não foi possível carregar os dados agora.'));});
})();
