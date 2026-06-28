(function () {
  const publicPrefixes = ['/account/login', '/account/register', '/account/forgotpassword', '/account/resetpassword', '/s/', '/r/', '/certificates/validate'];
  function isPublicPath(path) {
    const normalized = (path || window.location.pathname || '/').toLowerCase();
    if (normalized === '/') return true;
    return publicPrefixes.some(function (prefix) { return normalized === prefix || normalized.indexOf(prefix) === 0; });
  }
  function requireAuth(options) {
    const redirectTo = (options && options.redirectTo) || '/Account/Login';
    if (!isPublicPath(window.location.pathname) && window.Session && !Session.token()) {
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
  $(function () {
    $('[data-logout]').on('click', function (event) { event.preventDefault(); logout(); });
    requireAuth({ redirectTo: '/Account/Login' });
  });
}());
