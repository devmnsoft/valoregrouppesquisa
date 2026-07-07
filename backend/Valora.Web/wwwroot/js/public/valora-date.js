(function (window) {
  'use strict';

  function formatValoraDate(value) {
    if (!value) {
      return 'Data não informada';
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return 'Data não informada';
    }

    return date.toLocaleDateString('pt-BR');
  }

  window.formatValoraDate = formatValoraDate;
})(window);
