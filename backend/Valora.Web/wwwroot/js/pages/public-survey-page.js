function safe(value, fallback) {
  return value === undefined || value === null || Number.isNaN(value) ? (fallback || '—') : String(value);
}
function publicSurveyError(error) {
  const message = formatFriendlyError(error);
  $('.error-state').removeClass('d-none').text(message);
  Toast.error(message);
}
$(async function () {
  const page = $('[data-page="public-survey-page"]');
  if (!page.length) return;
  const surveyId = $('input[name=surveyId]').val() || location.pathname.split('/').pop();
  try {
    Loading.show('Carregando pesquisa...');
    const survey = await PublicSurveyApi.validate(surveyId);
    const questions = survey.questions || survey.items || [];
    if (!questions.length) throw new Error('FORM_WITHOUT_QUESTIONS');
    $('[data-results]').html(`<div class="col-12"><form id="surveyResponseForm"><h2>${safe(survey.title, 'Pesquisa Valora Insight™')}</h2><p>${safe(survey.description, 'Avaliação corporativa')}</p><div class="alert alert-info">LGPD: respostas tratadas para cálculo do diagnóstico e certificado.</div><div class="row g-2 mb-3"><div class="col-md-6"><label class="form-label">Nome</label><input class="form-control" name="name" required autocomplete="name"></div><div class="col-md-6"><label class="form-label">E-mail</label><input class="form-control" type="email" name="email" required autocomplete="email"></div><div class="col-12"><div class="form-check"><input class="form-check-input" type="checkbox" name="lgpd" id="surveyLgpd" required><label class="form-check-label" for="surveyLgpd">Aceito o tratamento LGPD das respostas e o envio do resultado por e-mail.</label></div></div></div>${questions.map((question, index) => `<fieldset class="mb-3"><legend class="h6">${index + 1}. ${safe(question.text || question.title)}</legend><select class="form-select" name="answers" data-question-id="${safe(question.id)}" data-question-type="${safe(question.type || 'likert_1_5')}" required><option value="">Selecione</option><option value="1">Discordo totalmente</option><option value="2">Discordo</option><option value="3">Neutro</option><option value="4">Concordo</option><option value="5">Concordo totalmente</option></select></fieldset>`).join('')}<button class="btn btn-primary btn-lg" type="submit">Enviar respostas</button></form></div>`);
    $('.empty-state').addClass('d-none');
    $('#surveyResponseForm').on('submit', async event => {
      event.preventDefault();
      const name = $('input[name=name]').val();
      const email = $('input[name=email]').val();
      const lgpd = $('input[name=lgpd]').is(':checked');
      if (!name || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email) || !lgpd) {
        publicSurveyError({ message: 'Informe nome, e-mail válido e aceite LGPD.' });
        return;
      }
      const answers = $('select[name=answers]').map((_, element) => ({
        questionId: element.dataset.questionId,
        type: element.dataset.questionType,
        scaleValue: Number(element.value)
      })).get();
      if (answers.some(answer => !answer.scaleValue)) {
        publicSurveyError({ message: 'Responda todas as perguntas obrigatórias.' });
        return;
      }
      try {
        Loading.show('Enviando respostas...');
        const result = await PublicSurveyApi.submit(surveyId, {
          participant: { name, email, phone: null, anonymous: false, consentVersion: '8.0', formStartedAt: new Date(Date.now() - 60000).toISOString() },
          lgpdConsent: true,
          communicationConsent: true,
          idempotencyKey: crypto.randomUUID(),
          answers
        });
        window.location.href = result.resultUrl || `/public/results/${encodeURIComponent(result.responseId)}`;
      } catch (error) {
        publicSurveyError(error);
      } finally {
        Loading.hide();
      }
    });
  } catch (error) {
    publicSurveyError(error);
  } finally {
    Loading.hide();
  }
});
