document.addEventListener('DOMContentLoaded', () => {
  const form = document.querySelector('[data-demo-form]');
  if (!form) return;
  form.addEventListener('submit', async event => {
    event.preventDefault();
    const feedback = form.querySelector('.form-feedback');
    const button = form.querySelector('button');
    if (!form.reportValidity() || form.website.value) return;
    button.disabled = true;
    feedback.hidden = true;
    try {
      const payload = { name: form.name.value.trim(), email: form.email.value.trim(), phone: form.phone.value.trim(), companyName: form.companyName.value.trim(), segment: form.interest.value, companySize: form.companySize.value, roleTitle: form.roleTitle.value.trim() || null, consentAccepted: form.consent.checked, communicationConsent: true, source: 'public_demo_request' };
      const leadResponse = await fetch('/bff/public/diagnostic/start', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '' }, body: JSON.stringify(payload) });
      const lead = await leadResponse.json();
      if (!leadResponse.ok) throw new Error(lead.message || 'Não foi possível registrar a solicitação.');
      const contactResponse = await fetch('/bff/public/contact-requests', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '' }, body: JSON.stringify({ leadId: lead.leadId, sessionId: lead.sessionId, requestType: 'contact', requestedPlan: null, notes: form.message.value.trim() || `Interesse: ${form.interest.value}` }) });
      if (!contactResponse.ok) throw new Error('O cadastro foi recebido, mas o pedido de contato não foi concluído.');
      form.reset(); feedback.textContent = 'Solicitação recebida. Nossa equipe comercial entrará em contato para combinar o melhor horário.'; feedback.classList.add('success'); feedback.hidden = false;
    } catch (error) { feedback.textContent = error.message || 'Não foi possível enviar agora. Tente novamente.'; feedback.hidden = false; }
    finally { button.disabled = false; }
  });
});
