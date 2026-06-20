'use strict';

function nowIso() { return new Date().toISOString(); }

module.exports = {
  async createCustomer(company) {
    return { provider: 'manual', externalCustomerId: `manual_customer_${company.id}`, createdAt: nowIso() };
  },
  async createSubscription(company, plan) {
    return { provider: 'manual', externalSubscriptionId: `manual_subscription_${company.id}_${plan.id}`, status: 'active', createdAt: nowIso() };
  },
  async createInvoice(company, items) {
    const amount = (items || []).reduce((sum, item) => sum + Number(item.total || item.unitPrice || 0) * Number(item.quantity || 1), 0);
    return { provider: 'manual', externalInvoiceId: `manual_invoice_${Date.now()}`, amount, status: 'open', createdAt: nowIso() };
  },
  async createPaymentLink(invoice) {
    return { provider: 'manual', paymentUrl: invoice.paymentUrl || `manual://invoice/${invoice.id}`, status: 'open' };
  },
  async handleWebhook(payload) {
    if (!payload || payload.provider !== 'manual') throw new Error('Webhook manual inválido.');
    return { provider: 'manual', event: payload.type || 'manual.event', accepted: true, receivedAt: nowIso() };
  },
};
