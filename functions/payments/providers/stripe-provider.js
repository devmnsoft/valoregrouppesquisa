'use strict';

function notConfigured() {
  throw new Error('Stripe ainda não configurado. Use secrets em Cloud Functions e valide assinatura do webhook.');
}
module.exports = { createCustomer: notConfigured, createSubscription: notConfigured, createInvoice: notConfigured, createPaymentLink: notConfigured, handleWebhook: notConfigured };
