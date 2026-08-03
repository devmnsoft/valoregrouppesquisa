(function () {
  'use strict';
  const labels = { success: 'Sucesso', danger: 'Erro', warning: 'Atenção', info: 'Informação' };
  function show(message, tone = 'info') {
    const container = document.getElementById('toastContainer');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = `toast valora-toast text-bg-${tone}`;
    toast.setAttribute('role', tone === 'danger' ? 'alert' : 'status');
    toast.setAttribute('aria-live', tone === 'danger' ? 'assertive' : 'polite');
    toast.innerHTML = '<div class="d-flex"><div class="toast-body"><strong></strong><span></span></div><button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Fechar notificação"></button></div>';
    toast.querySelector('strong').textContent = `${labels[tone] || labels.info}. `;
    toast.querySelector('span').textContent = message;
    toast.addEventListener('hidden.bs.toast', () => toast.remove(), { once: true });
    container.append(toast);
    bootstrap.Toast.getOrCreateInstance(toast, { delay: tone === 'danger' ? 8000 : 5000 }).show();
  }
  window.ValoraToast = Object.freeze({ show, success: m => show(m, 'success'), error: m => show(m, 'danger'), warning: m => show(m, 'warning'), info: m => show(m, 'info') });
  window.Toast = window.ValoraToast;
})();
