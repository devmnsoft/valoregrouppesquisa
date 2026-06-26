(function () {
  'use strict';

  function gatewayBaseUrl() {
    return (
      window.ValoraConfig?.COMMUNICATION_GATEWAY?.baseUrl ||
      window.ValoraConfig?.EXTERNAL_API_BASE_URL ||
      ''
    ).replace(/\/+$/, '');
  }

  async function callGatewayJson(path, options = {}, label = 'Gateway') {
    const baseUrl = gatewayBaseUrl();

    if (!baseUrl) {
      throw new Error('Canal de comunicação indisponível.');
    }

    if (typeof window.safeFetchJson === 'function') {
      return window.safeFetchJson(
        `${baseUrl}${path}`,
        {
          ...options,
          headers: {
            'Content-Type': 'application/json',
            ...(options.headers || {})
          }
        },
        label
      );
    }

    const response = await fetch(`${baseUrl}${path}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...(options.headers || {})
      }
    });

    const text = await response.text();
    const contentType = String(response.headers.get('content-type') || '');

    if (!contentType.includes('application/json')) {
      throw new Error(`${label} retornou formato inválido.`);
    }

    const body = text ? JSON.parse(text) : {};

    if (!response.ok) {
      throw new Error(body.message || `${label} retornou HTTP ${response.status}.`);
    }

    return body;
  }

  window.ValoraGatewayClient = Object.freeze({
    gatewayBaseUrl,
    callGatewayJson
  });
})();
