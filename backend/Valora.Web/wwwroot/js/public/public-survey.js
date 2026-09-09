(function () {
  'use strict';
  const vp = window.ValoraPublic;
  if (!vp) return;
  const page = document.querySelector('[data-page]')?.dataset.page || '';
  const safe = (value, fallback = '') => String(value ?? fallback).replace(/[<>&"]/g, character => ({ '<': '&lt;', '>': '&gt;', '&': '&amp;', '"': '&quot;' }[character]));
  const normalizeType = value => ({ likert_1_5: 'likert', escala: 'scale', unica_escolha: 'single_choice', multipla_escolha: 'multiple_choice', numero: 'number', text_short: 'short_text', text_long: 'long_text', boolean: 'yes_no' }[String(value || '').toLowerCase()] || String(value || '').toLowerCase());

  if (page === 'public-survey') {
    const form = document.querySelector('[data-public-survey-form]');
    const surveyId = document.querySelector('[name=surveyId]')?.value;
    const renderInput = question => {
      const type = normalizeType(question.type);
      const base = `data-question="${safe(question.id)}" data-answer-type="${safe(type)}" ${question.required ? 'required' : ''}`;
      if (type === 'short_text') return `<input ${base} maxlength="500">`;
      if (type === 'long_text') return `<textarea ${base} maxlength="5000"></textarea>`;
      if (type === 'number') return `<input ${base} type="number" min="0" max="${safe(question.maxScore, 100)}" step="any">`;
      if (type === 'yes_no') return `<select ${base}><option value="">Selecione</option><option value="true">Sim</option><option value="false">Não</option></select>`;
      if (type === 'multiple_choice') return `<div class="public-options">${(question.options || []).map(option => `<label><input type="checkbox" ${base} value="${safe(option.id)}"> ${safe(option.text)}</label>`).join('')}</div>`;
      if (type === 'single_choice') return `<select ${base}><option value="">Selecione</option>${(question.options || []).map(option => `<option value="${safe(option.id)}">${safe(option.text)}</option>`).join('')}</select>`;
      return `<select ${base}><option value="">Selecione</option>${[1, 2, 3, 4, 5].map(value => `<option value="${value}">${value}</option>`).join('')}</select>`;
    };
    const answerFor = question => {
      const type = normalizeType(question.type);
      const fields = [...form.querySelectorAll(`[data-question="${CSS.escape(String(question.id))}"]`)];
      const first = fields[0];
      const answer = { questionId: question.id, type };
      if (type === 'scale' || type === 'likert') answer.scaleValue = first?.value ? Number(first.value) : null;
      else if (type === 'single_choice') answer.optionId = first?.value || null;
      else if (type === 'multiple_choice') answer.optionIds = fields.filter(field => field.checked).map(field => field.value);
      else if (type === 'number') answer.numberValue = first?.value ? Number(first.value) : null;
      else if (type === 'yes_no') answer.booleanValue = first?.value === '' ? null : first.value === 'true';
      else answer.textValue = first?.value || null;
      return answer;
    };
    PublicSurveyApi.validate(surveyId).then(payload => {
      const survey = payload.survey || payload;
      const questions = payload.form?.questions || [];
      if (!questions.length) throw new Error('FORM_WITHOUT_QUESTIONS');
      document.querySelector('.empty-state')?.remove();
      form.innerHTML = `<div class="public-progress" aria-label="Progresso"><span style="width:0" data-progress></span></div><h2>${safe(survey.title, 'Pesquisa Valora Insight™')}</h2><p>${safe(survey.description, 'Sua participação apoia decisões melhores.')}</p><div class="public-consent"><h3>Privacidade e consentimento</h3><p>Usaremos as respostas para calcular o diagnóstico. Consulte a política de privacidade antes de continuar.</p><label>Nome (opcional em pesquisa anônima)<input name="name" autocomplete="name"></label><label>E-mail (opcional em pesquisa anônima)<input name="email" type="email" autocomplete="email"></label><label><input type="checkbox" name="anonymous"> Quero responder anonimamente</label><label><input type="checkbox" name="lgpd" required> Li e aceito o termo de consentimento LGPD <strong>versão 8.0</strong>.</label></div>${questions.map((question, index) => `<fieldset><legend>${index + 1}. ${safe(question.text)}${question.required ? ' *' : ''}</legend>${renderInput(question)}</fieldset>`).join('')}<p class="public-feedback" role="status" aria-live="polite" data-submit-feedback></p><button class="btn-public primary" type="submit">Enviar respostas com segurança</button>`;
      const updateProgress = () => {
        const answered = questions.map(answerFor).filter(answer => answer.scaleValue != null || answer.optionId || answer.optionIds?.length || answer.numberValue != null || answer.textValue || answer.booleanValue != null).length;
        form.querySelector('[data-progress]').style.width = `${Math.round(answered / questions.length * 100)}%`;
      };
      form.addEventListener('input', updateProgress);
      vp.bindOnce(form, async data => {
        const anonymous = data.get('anonymous') === 'on';
        const email = String(data.get('email') || '');
        if (!anonymous && !email.includes('@')) {
          vp.toast('Informe um e-mail válido ou selecione resposta anônima.', 'error');
          return;
        }
        form.querySelector('[data-submit-feedback]').textContent = 'Salvando sua resposta com segurança…';
        const result = await PublicSurveyApi.submit(surveyId, {
          participant: { name: anonymous ? null : data.get('name'), email: anonymous ? null : email, phone: null, anonymous, consentVersion: '8.0' },
          answers: questions.map(answerFor),
          lgpdConsent: true,
          communicationConsent: !anonymous,
          idempotencyKey: crypto.randomUUID()
        });
        if (!result.responseId || !result.resultUrl) throw new Error('RESPONSE_NOT_CONFIRMED');
        location.assign(result.resultUrl);
      });
    }).catch(error => {
      const incomplete = error?.message === 'FORM_WITHOUT_QUESTIONS';
      form.innerHTML = `<div class="empty-state" role="alert"><strong>${incomplete ? 'Diagnóstico em preparação' : 'Pesquisa indisponível'}</strong><p>${incomplete ? 'O formulário ainda não possui perguntas publicadas. Tente novamente mais tarde.' : 'Confira se o link está completo e se o período de respostas continua aberto.'}</p></div>`;
    });
    return;
  }

  if (page !== 'public-result') return;
  const id = document.querySelector('[name=responseId]')?.value;
  ResultsApi.public(id).then(result => {
    const response = result?.response || result || {};
    const score = result?.result || result || {};
    const dimensions = result?.dimensions || [];
    const setText = (selector, value) => { const element = document.querySelector(selector); if (element) element.textContent = value; };
    setText('[data-result-date]', (window.formatValoraDate || (value => value || 'Data não informada'))(response.completedAt || response.createdAt));
    setText('[data-result-score]', safe(score.percentage ?? score.score ?? '--'));
    setText('[data-result-level]', safe(score.maturityLabel || score.level || 'Nível de maturidade em processamento'));
    setText('[data-executive-reading]', safe(score.executiveSummary || score.reading || 'Leitura executiva em processamento.'));
    if (dimensions.length) setText('[data-dimensions-text]', dimensions.map(dimension => `${dimension.dimensionName || dimension.name}: ${dimension.percentage ?? dimension.score ?? 'em análise'}`).join(' • '));
  }).catch(() => {
    const element = document.querySelector('[data-executive-reading]');
    if (element) element.textContent = 'Resultado em preparação. Tente novamente em instantes ou fale com a Valora Group.';
  });
})();
