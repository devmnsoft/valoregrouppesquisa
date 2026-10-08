(function () {
  const publicPrefixes = ['/account/login', '/account/register', '/account/forgotpassword', '/account/resetpassword', '/s/', '/r/', '/certificates/validate'];
  function isPublicPath(path) {
    const normalized = (path || window.location.pathname || '/').toLowerCase();
    if (normalized === '/') return true;
    return publicPrefixes.some(function (prefix) { return normalized === prefix || normalized.indexOf(prefix) === 0; });
  }
  function requireAuth(options) {
    const redirectTo = (options && options.redirectTo) || '/Account/Login';
    // No fluxo BFF o cookie de sessao e a fonte da verdade: Session.isAuthenticated()
    // lê o marker data-authenticated renderizado no <body>. token() fica como fallback legado.
    var isAuthed = window.Session && (typeof Session.isAuthenticated === 'function' ? Session.isAuthenticated() : Boolean(Session.token()));
    if (!isPublicPath(window.location.pathname) && window.Session && !isAuthed) {
      window.location.href = redirectTo + '?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
      return false;
    }
    return true;
  }
  function handleForbidden(message) { Toast.error(message || 'Você não tem permissão para acessar este recurso.'); }
  function logout() {
    if (window.Session) Session.clear();
    if (window.AjaxClient) AjaxClient.clearToken();
    window.location.href = '/Account/Login';
  }
  window.Guards = { isPublicPath: isPublicPath, requireAuth: requireAuth, handleForbidden: handleForbidden, logout: logout };
  function init() {
    document.querySelectorAll('[data-logout]').forEach(function (el) {
      el.addEventListener('click', function (event) { event.preventDefault(); logout(); });
    });
    requireAuth({ redirectTo: '/Account/Login' });
  }
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
}());
