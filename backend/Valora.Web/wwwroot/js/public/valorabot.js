(function () {
  'use strict';
  const form = document.querySelector('[data-bot-form]');
  const messages = document.querySelector('[data-bot-messages]');
  if (!form || !messages) return;
  let sessionId = sessionStorage.getItem('valora.valorabot.session');
  const add = (text, role) => { const item = document.createElement('p'); item.className = `bot-message ${role}`; item.textContent = text; messages.append(item); messages.scrollTop = messages.scrollHeight; return item; };
  form.addEventListener('submit', async event => {
    event.preventDefault();
    const input = form.querySelector('textarea');
    const question = input.value.trim();
    if (!question) return;
    add(question, 'user'); input.value = ''; input.disabled = true;
    const loading = add('Consultando a base de orientação…', 'assistant loading');
    try {
      const result = await window.ValoraPublic.api('/api/valorabot/ask', { method: 'POST', body: JSON.stringify({ question, sessionId, context: location.pathname }) });
      loading.textContent = result.answer;
      loading.classList.remove('loading');
      sessionId = result.sessionId;
      sessionStorage.setItem('valora.valorabot.session', sessionId);
      (result.suggestedActions || []).forEach(action => { const link = document.createElement('a'); link.className = 'bot-action'; link.href = action.url; link.textContent = action.label; if (action.url.startsWith('http')) { link.target = '_blank'; link.rel = 'noopener noreferrer'; } messages.append(link); });
    } catch { loading.textContent = 'Não consegui consultar a central agora. Tente novamente ou fale pelo WhatsApp +55 91 99254-5353.'; loading.classList.remove('loading'); }
    finally { input.disabled = false; input.focus(); }
  });
})();
