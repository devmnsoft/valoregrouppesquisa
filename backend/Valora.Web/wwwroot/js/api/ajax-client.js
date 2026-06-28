(function () {
  const tokenKey = 'valora.jwt';
  const defaultTimeout = 20000;

  function apiBaseUrl() {
    return (window.ValoraWebConfig && window.ValoraWebConfig.API_BASE_URL ? window.ValoraWebConfig.API_BASE_URL : '').replace(/\/$/, '');
  }

  function generateCorrelationId() {
    if (window.crypto && window.crypto.randomUUID) {
      return 'web-' + window.crypto.randomUUID();
    }
    return 'web-' + Date.now() + '-' + Math.random().toString(16).slice(2);
  }

  function getToken() {
    return window.Session && typeof window.Session.token === 'function'
      ? window.Session.token()
      : sessionStorage.getItem(tokenKey);
  }

  function setToken(token) {
    if (!token) return;
    sessionStorage.setItem(tokenKey, token);
  }

  function clearToken() {
    if (window.Session && typeof window.Session.clear === 'function') {
      window.Session.clear();
      return;
    }
    sessionStorage.removeItem(tokenKey);
  }

  function normalizeApiError(xhr, correlationId) {
    const body = xhr && xhr.responseJSON && typeof xhr.responseJSON === 'object' ? xhr.responseJSON : {};
    const status = xhr && typeof xhr.status === 'number' ? xhr.status : 0;
    const responseText = xhr && xhr.responseText ? String(xhr.responseText) : '';
    const getHeader = xhr && typeof xhr.getResponseHeader === 'function' ? xhr.getResponseHeader.bind(xhr) : function () { return ''; };
    const contentType = getHeader('content-type') || '';
    let message = 'Não foi possível comunicar com a API.';

    if (responseText.trim().startsWith('<') || contentType.indexOf('text/html') >= 0) {
      message = 'A API retornou uma página HTML inesperada. Verifique publicação, proxy ou rota.';
    } else if (status === 0) {
      message = 'API offline ou indisponível no momento.';
    } else if (status === 401) {
      clearToken();
      message = 'Sessão expirada. Entre novamente.';
    } else if (status === 403) {
      message = 'Você não tem permissão para executar esta ação.';
    } else if (status === 404) {
      message = 'Recurso não encontrado na API.';
    } else if (status === 409) {
      message = body.message || 'Conflito de dados detectado pela API.';
    } else if (status === 422) {
      message = body.message || 'Verifique os dados informados antes de continuar.';
    } else if (status >= 500) {
      message = 'Erro interno na API. Informe o suporte com o correlationId.';
    } else if (body.message) {
      message = body.message;
    }

    return {
      ok: false,
      status: status,
      code: body.code || 'API_ERROR',
      message: message,
      correlationId: body.correlationId || body.correlationID || correlationId,
      traceId: body.traceId || body.traceID || ''
    };
  }

  function requestJson(method, path, data, options) {
    const correlationId = generateCorrelationId();
    const headers = { 'X-Correlation-Id': correlationId };
    const token = getToken();
    if (token) headers.Authorization = 'Bearer ' + token;

    return $.ajax({
      url: apiBaseUrl() + path,
      method: method,
      headers: headers,
      timeout: (window.ValoraWebConfig && window.ValoraWebConfig.API_TIMEOUT_MS) || defaultTimeout,
      contentType: 'application/json; charset=utf-8',
      dataType: (options && options.dataType) || 'json',
      data: data === undefined || data === null ? undefined : JSON.stringify(data)
    }).catch(function (xhr) {
      throw normalizeApiError(xhr, correlationId);
    });
  }

  function requestBinary(method, path, data) {
    const correlationId = generateCorrelationId();
    const headers = { 'X-Correlation-Id': correlationId };
    const token = getToken();
    if (token) headers.Authorization = 'Bearer ' + token;

    return $.ajax({
      url: apiBaseUrl() + path,
      method: method,
      headers: headers,
      timeout: (window.ValoraWebConfig && window.ValoraWebConfig.API_TIMEOUT_MS) || defaultTimeout,
      contentType: data ? 'application/json; charset=utf-8' : false,
      data: data ? JSON.stringify(data) : undefined,
      xhrFields: { responseType: 'blob' }
    }).catch(function (xhr) {
      throw normalizeApiError(xhr, correlationId);
    });
  }

  window.AjaxClient = {
    get: function (path, options) { return requestJson('GET', path, null, options); },
    post: function (path, data, options) { return requestJson('POST', path, data, options); },
    put: function (path, data, options) { return requestJson('PUT', path, data, options); },
    patch: function (path, data, options) { return requestJson('PATCH', path, data, options); },
    delete: function (path, data, options) { return requestJson('DELETE', path, data, options); },
    requestJson: requestJson,
    requestBinary: requestBinary,
    setToken: setToken,
    getToken: getToken,
    clearToken: clearToken,
    normalizeApiError: normalizeApiError,
    generateCorrelationId: generateCorrelationId
  };
}());
