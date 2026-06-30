const { test, expect } = require('@playwright/test');
const path = require('path');

const root = path.resolve(__dirname, '../..');

test('legacy mobile menu bridge works without app.js', async ({ page }) => {
  const errors = [];
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(error.message));

  await page.setViewportSize({ width: 414, height: 896 });
  await page.setContent(`<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><link rel="stylesheet" href="file://${root}/style.css?v=8.7.2"></head><body><div class="app-shell admin-shell"><button type="button" class="admin-mobile-toggle" data-action="toggleAdminMobileMenu" aria-label="Abrir menu administrativo" aria-controls="adminSidebar" aria-expanded="false"><span aria-hidden="true">☰</span><span>Menu</span></button><aside id="adminSidebar" class="admin-sidebar" aria-label="Menu administrativo" aria-hidden="true"><nav class="side-menu admin-nav"><button>Dashboard</button><button>Clientes</button><button>Pesquisas</button><button>Usuários</button><button>Respostas</button><button>Relatórios</button><button>Logs</button><button>Configurações</button></nav></aside></div><script src="file://${root}/legacy-admin-mobile-menu-bridge.js?v=8.7.2"></script></body></html>`, { waitUntil: 'domcontentloaded' });

  await expect.poll(async () => page.evaluate(() => !!window.ValoraAdminMobileMenuBridge)).toBeTruthy();
  await page.locator('[data-action="toggleAdminMobileMenu"]').click({ force: true });
  const debugAfter = await page.evaluate(() => window.ValoraAdminMobileMenuBridge.debug());
  expect(debugAfter.sidebarClass).toContain('open');
  expect(debugAfter.bodyClass).toContain('mobile-menu-open');
  expect(debugAfter.buttonExpanded).toBe('true');
  await expect(page.locator('.admin-mobile-overlay')).toHaveClass(/active/);
  await page.locator('.admin-mobile-overlay').click({ force: true });
  await expect(page.locator('#adminSidebar')).not.toHaveClass(/open/);
  expect(errors).toEqual([]);
});
