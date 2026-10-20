(() => {
  'use strict';

  function initExplainability() {
    const modal = document.getElementById('valoraExplainabilityModal') || document.getElementById('explainabilityModal');
    if (!modal) return;

    function openModal() {
      if (typeof modal.showModal === 'function') {
        modal.showModal();
      } else {
        modal.setAttribute('open', '');
      }
    }

    function closeModal() {
      if (typeof modal.close === 'function') {
        modal.close();
      } else {
        modal.removeAttribute('open');
      }
    }

    document.querySelectorAll('[data-action="close-explainability-modal"]').forEach(btn => {
      btn.addEventListener('click', closeModal);
    });

    modal.addEventListener('click', (e) => {
      if (e.target === modal) closeModal();
    });

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && modal.open) {
        closeModal();
      }
    });

    async function handleTrigger(trigger) {
      const insightId = trigger.dataset.insightId;
      const observationEl = modal.querySelector('[data-explainable="observation"]');
      const evidenceListEl = modal.querySelector('[data-explainable="evidence-list"]');
      const evidenceCountEl = modal.querySelector('[data-explainable="evidence-count"]');
      const evidenceRuleEl = modal.querySelector('[data-explainable="evidence-rule"]');
      const dimEl = modal.querySelector('[data-explainable="capability-dimension"]');
      const conceptEl = modal.querySelector('[data-explainable="capability-concept"]');
      const cascadingEl = modal.querySelector('[data-explainable="cascading-effects"]');
      const confBadgeEl = modal.querySelector('[data-explainable="confidence-badge"]');
      const confTextEl = modal.querySelector('[data-explainable="confidence-text"]');
      const consequenceEl = modal.querySelector('[data-explainable="consequence"]');
      const evolutionEl = modal.querySelector('[data-explainable="evolution-recommendation"]');
      const versionEl = modal.querySelector('[data-explainable="version"]');
      const hashEl = modal.querySelector('[data-explainable="hash"]');

      if (insightId) {
        try {
          const res = await fetch(`/Insights/Explain/${encodeURIComponent(insightId)}`, {
            headers: { 'Accept': 'application/json' }
          });
          if (res.ok) {
            const data = await res.json();
            if (observationEl) observationEl.textContent = data.whatWasObserved || 'Observação consolidada.';
            if (evidenceCountEl) evidenceCountEl.textContent = `${data.confidence.evidenceCount} evidência(s)`;
            if (evidenceRuleEl) evidenceRuleEl.textContent = data.confidence.methodologicalRule;
            if (evidenceListEl) {
              if (data.evidenceConsidered && data.evidenceConsidered.length > 0) {
                evidenceListEl.innerHTML = data.evidenceConsidered.map(e => `
                  <li class="p-2 bg-light rounded d-flex justify-content-between align-items-center">
                    <span>${e.summary || e.type}</span>
                    <small class="badge bg-secondary">${e.sourceType || 'Evidência'}</small>
                  </li>
                `).join('');
              } else {
                evidenceListEl.innerHTML = '<li class="p-2 bg-light rounded text-muted">Nenhuma evidência isolada registrada; leitura agregada.</li>';
              }
            }
            if (dimEl) dimEl.textContent = data.relatedCapabilities?.[0]?.dimensionName || 'Geral';
            if (conceptEl) conceptEl.textContent = data.relatedCapabilities?.[0]?.conceptName || 'Dicionário Cognitivo';
            if (cascadingEl && data.relatedCapabilities?.[0]?.cascadingInfluences?.length) {
              cascadingEl.innerHTML = `<strong>Efeito Cascata:</strong> ${data.relatedCapabilities[0].cascadingInfluences.join(', ')}`;
            }
            if (confBadgeEl) {
              confBadgeEl.textContent = data.confidence.level;
              confBadgeEl.className = `badge ${data.confidence.level === 'Alta' ? 'bg-success' : data.confidence.level === 'Moderada' ? 'bg-info text-dark' : 'bg-warning text-dark'}`;
            }
            if (confTextEl) confTextEl.textContent = data.confidence.methodologicalRule;
            if (consequenceEl) consequenceEl.textContent = data.consequence?.systemicRisk || 'Risco de sustentabilidade da dimensão.';
            if (evolutionEl) evolutionEl.textContent = data.evolution?.recommendedAction || 'Plano de evolução recomendado no Valora Action™.';
            if (versionEl) versionEl.textContent = data.methodologyVersion || 'Valora Insight™ v2.0 - Metodologia Oficial';
            if (hashEl) hashEl.textContent = data.dataHash ? data.dataHash.substring(0, 16) + '...' : 'Hash Valora™';

            openModal();
            return;
          }
        } catch {
          // fallback to dataset below
        }
      }

      // Inline attributes fallback
      if (observationEl && trigger.dataset.observed) observationEl.textContent = trigger.dataset.observed;
      if (dimEl && trigger.dataset.dimension) dimEl.textContent = trigger.dataset.dimension;
      if (consequenceEl && trigger.dataset.risk) consequenceEl.textContent = trigger.dataset.risk;
      if (evolutionEl && trigger.dataset.recommendation) evolutionEl.textContent = trigger.dataset.recommendation;
      if (confBadgeEl && trigger.dataset.confidence) {
        confBadgeEl.textContent = trigger.dataset.confidence;
      }
      openModal();
    }

    document.querySelectorAll('[data-action="open-explainability-modal"]').forEach(trigger => {
      trigger.addEventListener('click', () => handleTrigger(trigger));
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initExplainability);
  } else {
    initExplainability();
  }
})();
