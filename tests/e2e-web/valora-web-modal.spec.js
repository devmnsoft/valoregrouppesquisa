const { test, expect } = require('@playwright/test');
const path = require('path');

async function fixture(page) {
  await page.setContent(`<style>
    .valora-dialog-layer{position:fixed;inset:0;display:grid}.valora-dialog-layer[hidden]{display:none!important}
  </style><button id="trigger">Excluir</button><button id="behind">Página</button>`);
  await page.addScriptTag({ path: path.resolve('backend/Valora.Web/wwwroot/js/core/modal.js') });
  await page.locator('#trigger').evaluate(button => button.addEventListener('click', () => window.ConfirmModal.ask('Esta alteração não poderá ser desfeita.', { title: 'Excluir registro' })));
}

for (const closeWith of ['cancelar', 'fechar', 'backdrop', 'escape', 'confirmar']) {
  test(`modal oficial abre por ação e fecha por ${closeWith} sem bloquear a página`, async ({ page }) => {
    await fixture(page);
    await expect(page.locator('.valora-dialog-layer')).toHaveCount(0);
    await page.click('#trigger');
    await expect(page.getByRole('dialog')).toContainText('Excluir registro');
    await expect(page.getByRole('dialog')).toContainText('Esta alteração não poderá ser desfeita.');
    if (closeWith === 'cancelar') await page.getByRole('button', { name: 'Cancelar' }).click();
    if (closeWith === 'fechar') await page.getByRole('button', { name: 'Fechar' }).click();
    if (closeWith === 'backdrop') await page.locator('.valora-dialog-backdrop').click({ position: { x: 2, y: 2 } });
    if (closeWith === 'escape') await page.keyboard.press('Escape');
    if (closeWith === 'confirmar') await page.getByRole('button', { name: 'Continuar' }).click();
    await expect(page.locator('.valora-dialog-layer')).toHaveCount(0);
    await expect(page.locator('body')).not.toHaveClass(/valora-dialog-open/);
    await page.click('#behind');
  });
}
