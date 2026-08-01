(function () {
  "use strict";

  const palette = document.getElementById("commandPalette");
  const input = palette?.querySelector("[data-command-input]");
  const results = palette?.querySelector("[data-command-results]");
  const notifications = document.getElementById("notificationPanel");
  const userMenu = document.getElementById("userMenu");
  let debounce;

  function togglePanel(panel, button) {
    const willOpen = panel?.hidden ?? false;
    [notifications, userMenu].forEach((item) => { if (item && item !== panel) item.hidden = true; });
    if (panel) panel.hidden = !willOpen;
    button?.setAttribute("aria-expanded", String(willOpen));
  }

  function openPalette() {
    if (!palette) return;
    palette.hidden = false;
    document.body.style.overflow = "hidden";
    window.setTimeout(() => input?.focus(), 0);
  }

  function closePalette() {
    if (!palette) return;
    palette.hidden = true;
    document.body.style.overflow = "";
  }

  async function search(query) {
    if (!results) return;
    if (query.length < 2) { results.innerHTML = "<p>Digite ao menos dois caracteres.</p>"; return; }
    results.innerHTML = "<p>Buscando…</p>";
    try {
      const response = await fetch(`/bff/search?q=${encodeURIComponent(query)}`, { credentials: "same-origin" });
      if (!response.ok) throw new Error("search_failed");
      const payload = await response.json();
      results.replaceChildren();
      const items = payload.items || payload;
      if (!items.length) { results.innerHTML = "<p>Nenhum resultado nesta organização.</p>"; return; }
      items.slice(0, 10).forEach((item) => {
        const link = document.createElement("a");
        link.href = item.url;
        link.textContent = `${item.title} — ${item.domain || "Valora"}`;
        results.append(link);
      });
    } catch { results.innerHTML = "<p>A busca está temporariamente indisponível.</p>"; }
  }

  document.addEventListener("click", (event) => {
    const action = event.target.closest("[data-action]")?.dataset.action;
    if (action === "open-command-palette") openPalette();
    if (action === "close-command-palette" || (event.target === palette)) closePalette();
    if (action === "toggle-notifications") togglePanel(notifications, event.target.closest("button"));
    if (action === "toggle-user-menu") togglePanel(userMenu, event.target.closest("button"));
    if (action === "logout") document.getElementById("logoutButton")?.click();
  });
  document.addEventListener("keydown", (event) => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") { event.preventDefault(); openPalette(); }
    if (event.key === "/" && !event.target.matches("input, textarea")) { event.preventDefault(); openPalette(); }
    if (event.key === "Escape") closePalette();
  });
  input?.addEventListener("input", () => { window.clearTimeout(debounce); debounce = window.setTimeout(() => search(input.value.trim()), 250); });
})();
