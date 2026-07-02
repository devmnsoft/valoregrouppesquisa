(function(){
  const root=document.querySelector('.migration-page');
  if(!root) return;
  const batchId=root.dataset.batchId;
  const set=(id,v)=>{const el=document.getElementById(id); if(el) el.textContent=v;};
  set('migrationStatus', batchId ? 'Batch '+batchId.substring(0,8) : 'Selecione um batch');
  // A UI nunca renderiza payload JSON bruto; consome somente DTOs mascarados da API oficial.
})();
