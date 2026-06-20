'use strict';

const manualProvider = require('./providers/manual-provider');

function providerFor(name = 'manual') {
  switch (name) {
    case 'manual':
    case '':
    case undefined:
      return manualProvider;
    case 'stripe':
      return require('./providers/stripe-provider');
    case 'mercadopago':
      return require('./providers/mercadopago-provider');
    default:
      throw new Error(`Provedor de pagamento não suportado: ${name}`);
  }
}

module.exports = {
  providerFor,
  createCustomer: (company, provider) => providerFor(provider).createCustomer(company),
  createSubscription: (company, plan, provider) => providerFor(provider).createSubscription(company, plan),
  createInvoice: (company, items, provider) => providerFor(provider).createInvoice(company, items),
  createPaymentLink: (invoice, provider) => providerFor(provider).createPaymentLink(invoice),
  handleWebhook: (payload, provider, context = {}) => providerFor(provider).handleWebhook(payload, context),
};
