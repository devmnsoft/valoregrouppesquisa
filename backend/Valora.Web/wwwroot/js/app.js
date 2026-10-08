(function () {
  function init() {
    var badge = document.getElementById('environmentBadge');
    if (badge && window.ValoraWebConfig) { badge.textContent = window.ValoraWebConfig.ENVIRONMENT; }
  }
  // O botao de sair (#logoutButton) e vinculado pelo shell do topbar (topbar-shell.js -> /Account/Logout);
  // nenhum layout carrega jQuery, entao o bootstrap legado em $(...) causava ReferenceError.
  if (document.readyState === 'loading') { document.addEventListener('DOMContentLoaded', init); } else { init(); }
})();
