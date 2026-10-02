(function () {
  const noResults = document.documentElement.getAttribute("data-no-results") || "No se encontraron resultados";
  const searchLabel = document.documentElement.getAttribute("data-search-option") || "Buscar una opción";

  (function conservarLugarDeTrabajo() {
    const clave = "nr.scroll." + location.pathname;
    let pendiente = leer();

    function principal() {
      return document.querySelector("main");
    }

    function leer() {
      const raw = sessionStorage.getItem(clave);
      if (!raw) {
        return null;
      }
      sessionStorage.removeItem(clave);
      try {
        return JSON.parse(raw);
      } catch (error) {
        return null;
      }
    }

    function guardar() {
      const main = principal();
      const actual = {
        main: main ? main.scrollTop : 0,
        win: window.scrollY || 0
      };
      const previoRaw = sessionStorage.getItem(clave);
      if (previoRaw) {
        try {
          const previo = JSON.parse(previoRaw);
          if ((previo.main || 0) > actual.main) {
            actual.main = previo.main;
          }
          if ((previo.win || 0) > actual.win) {
            actual.win = previo.win;
          }
        } catch (error) {
          /* se reemplaza el valor corrupto */
        }
      }
      sessionStorage.setItem(clave, JSON.stringify(actual));
    }

    function aplicar() {
      if (!pendiente) {
        return;
      }
      if ("scrollRestoration" in history) {
        history.scrollRestoration = "manual";
      }
      const main = principal();
      if (main) {
        main.scrollTop = pendiente.main || 0;
      }
      if (pendiente.win) {
        window.scrollTo(0, pendiente.win);
      }
    }

    function esEnvio(boton) {
      if (!(boton instanceof HTMLElement)) {
        return false;
      }
      if (boton.closest("#confirmacionForm")) {
        return false;
      }
      const tipo = (boton.getAttribute("type") || (boton.tagName === "BUTTON" ? "submit" : "")).toLowerCase();
      return tipo === "submit";
    }

    aplicar();
    requestAnimationFrame(function () {
      aplicar();
      requestAnimationFrame(aplicar);
    });
    window.addEventListener("load", aplicar);
    window.addEventListener("pageshow", aplicar);

    document.addEventListener("click", function (event) {
      const boton = event.target.closest("button[type='submit'], button:not([type]), input[type='submit']");
      if (esEnvio(boton)) {
        const formId = boton.getAttribute("form");
        const form = formId ? document.getElementById(formId) : boton.form || boton.closest("form");
        if (form instanceof HTMLFormElement) {
          guardar();
        }
        return;
      }
      const enlace = event.target.closest("a[href]");
      if (!enlace || enlace.getAttribute("target") === "_blank") {
        return;
      }
      let url;
      try {
        url = new URL(enlace.href, location.href);
      } catch (error) {
        return;
      }
      if (url.origin !== location.origin || url.pathname !== location.pathname) {
        return;
      }
      guardar();
    }, true);

    document.addEventListener("submit", function (event) {
      const form = event.target;
      if (!(form instanceof HTMLFormElement) || form.id === "confirmacionForm") {
        return;
      }
      const accion = form.getAttribute("data-protected-action");
      if (accion && form.dataset.protegidoOk !== "1") {
        return;
      }
      guardar();
    });

    window.NewRichOlvidarScroll = function () {
      sessionStorage.removeItem(clave);
    };
  })();

  function enhanceSelect(select) {
    if (select.dataset.enhanced === "true") {
      return;
    }
    select.dataset.enhanced = "true";
    select.classList.add("hidden");
    select.setAttribute("aria-hidden", "true");

    const wrapper = document.createElement("div");
    wrapper.className = "search-select";
    select.parentNode.insertBefore(wrapper, select);
    wrapper.appendChild(select);

    const input = document.createElement("input");
    input.type = "search";
    input.className = "search-select-input";
    input.setAttribute("role", "combobox");
    input.setAttribute("aria-expanded", "false");
    input.setAttribute("aria-autocomplete", "list");
    input.setAttribute("aria-label", searchLabel);
    input.placeholder = searchLabel;
    wrapper.insertBefore(input, select);

    const menu = document.createElement("div");
    menu.className = "search-select-menu";
    menu.setAttribute("role", "listbox");
    wrapper.appendChild(menu);

    function selectedText() {
      const option = select.options[select.selectedIndex];
      return option ? option.text : "";
    }

    function render(query) {
      const q = (query || "").toLowerCase().trim();
      const matches = Array.from(select.options).filter((opt) =>
        opt.text.toLowerCase().includes(q)
      );
      menu.innerHTML = "";
      if (!matches.length) {
        const empty = document.createElement("div");
        empty.className = "search-select-empty";
        empty.textContent = noResults;
        menu.appendChild(empty);
      } else {
        matches.forEach((opt) => {
          const btn = document.createElement("button");
          btn.type = "button";
          btn.className = "search-select-option";
          btn.setAttribute("role", "option");
          btn.textContent = opt.text;
          btn.addEventListener("click", function () {
            select.value = opt.value;
            input.value = opt.text;
            menu.classList.remove("open");
            input.setAttribute("aria-expanded", "false");
            select.dispatchEvent(new Event("change", { bubbles: true }));
          });
          menu.appendChild(btn);
        });
      }
      menu.classList.add("open");
      input.setAttribute("aria-expanded", "true");
    }

    input.value = selectedText();
    input.addEventListener("focus", function () {
      input.select();
      render("");
    });
    input.addEventListener("input", function () { render(input.value); });
    input.addEventListener("keydown", function (event) {
      if (event.key === "Escape") {
        menu.classList.remove("open");
        input.setAttribute("aria-expanded", "false");
      }
      if (event.key === "Enter") {
        const first = menu.querySelector(".search-select-option");
        if (first) {
          event.preventDefault();
          first.click();
        }
      }
    });
    document.addEventListener("click", function (event) {
      if (!wrapper.contains(event.target)) {
        menu.classList.remove("open");
        input.setAttribute("aria-expanded", "false");
        input.value = selectedText();
      }
    });
  }

  document.querySelectorAll("select.searchable").forEach(enhanceSelect);

  document.querySelectorAll("table[data-kpi-sort-table]").forEach(function (table) {
    const headers = Array.from(table.querySelectorAll("thead th"));
    const body = table.querySelector("tbody");
    if (!body) {
      return;
    }

    headers.forEach(function (header, index) {
      const button = header.querySelector("[data-kpi-sort]");
      if (!button) {
        return;
      }

      button.addEventListener("click", function () {
        const ascending = button.getAttribute("aria-sort") !== "ascending";
        headers.forEach(function (item) {
          item.removeAttribute("aria-sort");
          const itemButton = item.querySelector("[data-kpi-sort]");
          if (itemButton) {
            const arrow = itemButton.querySelector("span");
            if (arrow) {
              arrow.textContent = "↕";
            }
          }
        });

        button.setAttribute("aria-sort", ascending ? "ascending" : "descending");
        const arrow = button.querySelector("span");
        if (arrow) {
          arrow.textContent = ascending ? "↑" : "↓";
        }

        const type = button.getAttribute("data-kpi-sort");
        const rows = Array.from(body.querySelectorAll("tr"));
        rows.sort(function (left, right) {
          const leftValue = left.cells[index]?.textContent.trim() || "";
          const rightValue = right.cells[index]?.textContent.trim() || "";
          const comparison = type === "number"
            ? (parseFloat(leftValue.replace(/[^\d,-]/g, "").replace(/\./g, "").replace(",", ".")) || 0)
              - (parseFloat(rightValue.replace(/[^\d,-]/g, "").replace(/\./g, "").replace(",", ".")) || 0)
            : leftValue.localeCompare(rightValue, "es", { sensitivity: "base" });
          return ascending ? comparison : -comparison;
        });
        rows.forEach(function (row) { body.appendChild(row); });
      });
    });
  });

  document.querySelectorAll("form[data-offline-generate]").forEach(function (form) {
    const usuario = form.querySelector("[data-offline-usuario]");
    const pdaField = form.querySelector("[data-offline-pda]");
    const pdaValue = form.querySelector("[data-offline-pda-value]");
    const associate = form.querySelector("[data-offline-associate]");
    const submit = form.querySelector("[data-offline-submit]");
    if (!usuario || !pdaField || !pdaValue || !associate || !submit) {
      return;
    }

    function syncPda() {
      const option = usuario.options[usuario.selectedIndex];
      const pdaId = option ? (option.getAttribute("data-pda-id") || "").trim() : "";
      const codigo = option ? (option.getAttribute("data-pda-codigo") || "").trim() : "";
      const hasUser = !!(usuario.value);
      const hasPda = hasUser && !!pdaId;
      pdaField.hidden = !hasPda;
      pdaValue.textContent = hasPda ? codigo : "";
      if (hasPda) {
        associate.hidden = true;
      } else {
        associate.hidden = !hasUser;
      }
      submit.disabled = !hasPda;
    }

    usuario.addEventListener("change", syncPda);
    syncPda();
  });

  function setControlEnabled(ctrl, enabled) {
    if (ctrl.tagName === "INPUT" && ctrl.type === "hidden") {
      return;
    }
    if (ctrl.tagName === "A") {
      ctrl.setAttribute("aria-disabled", enabled ? "false" : "true");
      if (enabled) {
        ctrl.removeAttribute("tabindex");
      } else {
        ctrl.setAttribute("tabindex", "-1");
      }
      return;
    }
    ctrl.disabled = !enabled;
    if (ctrl.tagName === "SELECT") {
      const wrap = ctrl.closest(".search-select");
      const fake = wrap && wrap.querySelector(".search-select-input");
      if (fake) {
        fake.disabled = !enabled;
      }
    }
  }

  function controlsOf(el) {
    if (el.matches("a, button")) {
      return [el];
    }
    return Array.from(el.querySelectorAll("a, button, select, input"));
  }

  function syncIdFields(panel, ids) {
    panel.querySelectorAll("form[data-post-ids]").forEach(function (form) {
      form.querySelectorAll('input[name="ids"]').forEach(function (node) {
        node.remove();
      });
      ids.forEach(function (id) {
        const input = document.createElement("input");
        input.type = "hidden";
        input.name = "ids";
        input.value = id;
        form.appendChild(input);
      });
    });
    panel.querySelectorAll("a[data-ids-query]").forEach(function (link) {
      const base = link.getAttribute("data-url-base") || "";
      if (!ids.length) {
        link.setAttribute("href", "#");
        return;
      }
      const query = ids.map(function (id) {
        return "ids=" + encodeURIComponent(id);
      }).join("&");
      link.setAttribute("href", base + "?" + query);
    });
  }

  function applyMultiSelection(panel, rows) {
    const ids = rows.map(function (row) {
      return row.getAttribute("data-row-id") || "";
    }).filter(Boolean);
    syncIdFields(panel, ids);
    panel.querySelectorAll("[data-needs-selection]").forEach(function (el) {
      const needsOne = el.hasAttribute("data-needs-one");
      const enabled = needsOne ? rows.length === 1 : rows.length > 0;
      el.hidden = false;
      controlsOf(el).forEach(function (ctrl) {
        setControlEnabled(ctrl, enabled);
      });
    });
  }

  function applyRowSelection(panel, row) {
    const id = row.getAttribute("data-row-id") || "";
    const flags = (row.getAttribute("data-flags") || "").split(/\s+/).filter(Boolean);
    panel.querySelectorAll("[data-from-attr]").forEach(function (el) {
      el.value = row.getAttribute(el.getAttribute("data-from-attr")) || "";
    });
    panel.querySelectorAll("[data-needs-selection]").forEach(function (el) {
      const when = el.getAttribute("data-show-when");
      const hide = el.getAttribute("data-hide-when");
      const visible = (!when || flags.indexOf(when) >= 0) && (!hide || flags.indexOf(hide) < 0);
      el.hidden = !visible;
      controlsOf(el).forEach(function (ctrl) {
        setControlEnabled(ctrl, visible);
      });
      const template = el.getAttribute("data-url-template");
      if (template && id) {
        const url = template.split("{id}").join(encodeURIComponent(id));
        if (el.tagName === "A") {
          el.setAttribute("href", url);
        } else if (el.tagName === "FORM") {
          el.setAttribute("action", url);
        }
      }
    });
  }

  function clearPanel(panel) {
    syncIdFields(panel, []);
    panel.querySelectorAll("[data-needs-selection]").forEach(function (el) {
      el.hidden = !!el.getAttribute("data-show-when");
      controlsOf(el).forEach(function (ctrl) {
        setControlEnabled(ctrl, false);
      });
    });
  }

  document.querySelectorAll("table[data-table-select]").forEach(function (table) {
    const name = table.getAttribute("data-table-select");
    const panel = document.querySelector('[data-table-panel="' + name + '"]');
    if (panel) {
      clearPanel(panel);
      panel.addEventListener("click", function (event) {
        const link = event.target.closest("a[aria-disabled='true']");
        if (link) {
          event.preventDefault();
        }
      });
    }
    table.querySelectorAll(".row-check").forEach(function (box) {
      box.addEventListener("change", function () {
        const multiple = table.getAttribute("data-table-select-mode") === "multiple";
        if (box.checked && !multiple) {
          table.querySelectorAll(".row-check").forEach(function (other) {
            if (other !== box) {
              other.checked = false;
            }
          });
        }
        table.querySelectorAll("tbody tr").forEach(function (tr) {
          const check = tr.querySelector(".row-check");
          tr.classList.toggle("selected", !!(check && check.checked));
        });
        const rows = Array.from(table.querySelectorAll(".row-check:checked"))
          .map(function (item) { return item.closest("tr"); })
          .filter(Boolean);
        if (!panel) {
          return;
        }
        if (rows.length === 0) {
          clearPanel(panel);
          return;
        }
        if (multiple) {
          applyMultiSelection(panel, rows);
        } else {
          applyRowSelection(panel, rows[0]);
        }
      });
    });
  });

  document.querySelectorAll("form[data-submit-on-change]").forEach(function (form) {
    form.addEventListener("change", function (event) {
      const target = event.target;
      if (!target) {
        return;
      }
      if (target.matches("input[type=date]")) {
        const periodo = form.querySelector("select[name=periodo]");
        if (periodo) {
          periodo.value = "personalizado";
        }
      }
      if (target.matches("input[type=date], select")) {
        form.submit();
      }
    });
  });

  const ticketDialog = document.getElementById("ticketDialog");
  const ticketPanel = ticketDialog && ticketDialog.querySelector("[data-ticket-panel]");
  let ticketTrigger = null;

  function closeTicket() {
    if (!ticketDialog) {
      return;
    }
    if (ticketDialog.classList.contains("hidden")) {
      if (window.history.length > 1) {
        window.history.back();
      }
      return;
    }
    ticketDialog.classList.add("hidden");
    ticketDialog.setAttribute("hidden", "hidden");
    if (ticketPanel) {
      ticketPanel.innerHTML = "";
    }
    if (ticketTrigger && typeof ticketTrigger.focus === "function") {
      ticketTrigger.focus();
    }
    ticketTrigger = null;
  }

  function fillReceiptRules(root) {
    if (!root) {
      return;
    }
    root.querySelectorAll(".receipt-rule").forEach(function (el) {
      el.textContent = "";
      const probe = document.createElement("span");
      probe.textContent = "=";
      el.appendChild(probe);
      const charWidth = probe.getBoundingClientRect().width;
      const width = el.clientWidth;
      probe.remove();
      const count = charWidth > 0 ? Math.max(1, Math.floor(width / charWidth)) : 1;
      el.textContent = "=".repeat(count);
    });
  }

  function bindTicketActions(root) {
    if (!root) {
      return;
    }
    fillReceiptRules(root);
    requestAnimationFrame(function () {
      fillReceiptRules(root);
    });
    root.querySelectorAll("[data-close-ticket]").forEach(function (el) {
      el.addEventListener("click", closeTicket);
    });
    root.querySelectorAll("[data-print-ticket]").forEach(function (el) {
      el.addEventListener("click", function () {
        fillReceiptRules(root);
        window.print();
      });
    });
  }

  async function openTicket(anchor) {
    if (!ticketDialog || !ticketPanel) {
      return;
    }
    const url = anchor.getAttribute("data-ticket-url");
    if (!url) {
      return;
    }
    ticketTrigger = anchor;
    const response = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
    if (!response.ok) {
      window.location.href = anchor.getAttribute("href") || url;
      return;
    }
    ticketPanel.innerHTML = await response.text();
    ticketDialog.classList.remove("hidden");
    ticketDialog.removeAttribute("hidden");
    bindTicketActions(ticketPanel);
    const closeBtn = ticketPanel.querySelector("[data-close-ticket]");
    if (closeBtn) {
      closeBtn.focus();
    }
  }

  document.addEventListener("click", function (event) {
    const el = event.target.closest("[data-ticket-modal]");
    if (!el) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    openTicket(el).catch(function () {
      window.location.href = el.getAttribute("href") || el.getAttribute("data-ticket-url") || "/";
    });
  }, true);
  if (ticketDialog) {
    ticketDialog.addEventListener("click", function (event) {
      if (event.target === ticketDialog) {
        closeTicket();
      }
    });
  }
  bindTicketActions(document.querySelector(".ticket-page"));

  const usuarioDialog = document.getElementById("usuarioDialog");
  const usuarioPanel = usuarioDialog && usuarioDialog.querySelector("[data-usuario-panel]");
  let usuarioTrigger = null;

  function closeUsuario() {
    if (!usuarioDialog) {
      return;
    }
    usuarioDialog.classList.add("hidden");
    usuarioDialog.setAttribute("hidden", "hidden");
    if (usuarioPanel) {
      usuarioPanel.innerHTML = "";
    }
    if (usuarioTrigger && typeof usuarioTrigger.focus === "function") {
      usuarioTrigger.focus();
    }
    usuarioTrigger = null;
  }

  async function openUsuario(anchor) {
    if (!usuarioDialog || !usuarioPanel) {
      return;
    }
    const url = anchor.getAttribute("data-usuario-url");
    if (!url) {
      return;
    }
    usuarioTrigger = anchor;
    const response = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
    if (!response.ok) {
      window.location.href = anchor.getAttribute("href") || url;
      return;
    }
    usuarioPanel.innerHTML = await response.text();
    usuarioDialog.classList.remove("hidden");
    usuarioDialog.removeAttribute("hidden");
    prepararUsuario();
  }

  function prepararUsuario() {
    if (!usuarioPanel) {
      return;
    }
    usuarioPanel.querySelectorAll("select.searchable").forEach(enhanceSelect);
    const closeBtn = usuarioPanel.querySelector("[data-close-usuario]");
    if (closeBtn) {
      closeBtn.addEventListener("click", closeUsuario);
    }
    const form = usuarioPanel.querySelector("[data-usuario-form]");
    if (!form) {
      if (closeBtn) {
        closeBtn.focus();
      }
      return;
    }
    bindUsuarioGrupo(form);
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      enviarUsuario(form).catch(function () {
        window.location.reload();
      });
    });
    const primero = form.querySelector("input:not([type=hidden])");
    if (primero) {
      primero.focus();
    }
  }

  // El grupo solo aplica al perfil vendedor; el resto de perfiles no pertenece a grupos.
  function bindUsuarioGrupo(form) {
    const rol = form.querySelector("[data-usuario-rol]");
    const grupo = form.querySelector("[data-usuario-grupo]");
    if (!rol || !grupo) {
      return;
    }
    const rolConGrupo = grupo.getAttribute("data-usuario-grupo");
    function sincronizar() {
      const conGrupo = rol.value === rolConGrupo;
      grupo.hidden = !conGrupo;
      const select = grupo.querySelector("select");
      if (!select) {
        return;
      }
      select.disabled = !conGrupo;
      if (!conGrupo) {
        select.value = "";
      }
      const input = grupo.querySelector(".search-select-input");
      if (input) {
        const option = select.options[select.selectedIndex];
        input.value = option ? option.text : "";
      }
    }
    rol.addEventListener("change", sincronizar);
    sincronizar();
  }

  document.querySelectorAll("[data-usuario-form]").forEach(bindUsuarioGrupo);

  async function enviarUsuario(form) {
    const boton = form.querySelector("button[type=submit]");
    if (boton) {
      boton.disabled = true;
    }
    const response = await fetch(form.getAttribute("action") || window.location.href, {
      method: "POST",
      headers: { "X-Requested-With": "XMLHttpRequest" },
      body: new FormData(form)
    });
    if (!response.ok) {
      window.location.reload();
      return;
    }
    const tipo = response.headers.get("content-type") || "";
    if (tipo.indexOf("application/json") >= 0) {
      const data = await response.json();
      window.location.href = data.redirect || window.location.href;
      return;
    }
    usuarioPanel.innerHTML = await response.text();
    prepararUsuario();
  }

  document.addEventListener("click", function (event) {
    const el = event.target.closest("[data-usuario-modal]");
    if (!el) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    openUsuario(el).catch(function () {
      window.location.href = el.getAttribute("href") || el.getAttribute("data-usuario-url") || "/";
    });
  }, true);
  if (usuarioDialog) {
    usuarioDialog.addEventListener("click", function (event) {
      if (event.target === usuarioDialog) {
        closeUsuario();
      }
    });
  }

  const codigoDialog = document.getElementById("codigoDialog");
  const codigoClose = codigoDialog && codigoDialog.querySelector("[data-close-codigo]");
  const codigoTirilla = codigoDialog && codigoDialog.querySelector("[data-codigo-tirilla]");
  const codigoTirillaPanel = codigoDialog && codigoDialog.querySelector("[data-codigo-tirilla-panel]");
  let codigoTrigger = null;

  function closeCodigo() {
    if (!codigoDialog) {
      return;
    }
    codigoDialog.classList.add("hidden");
    codigoDialog.setAttribute("hidden", "hidden");
    if (codigoTirilla) {
      codigoTirilla.classList.add("hidden");
      codigoTirilla.setAttribute("hidden", "hidden");
    }
    if (codigoTirillaPanel) {
      codigoTirillaPanel.innerHTML = "";
    }
    if (codigoTrigger && typeof codigoTrigger.focus === "function") {
      codigoTrigger.focus();
    }
    codigoTrigger = null;
  }

  function fillCodigoField(name, value, pill) {
    const field = codigoDialog.querySelector('[data-codigo-field="' + name + '"]');
    if (!field) {
      return;
    }
    field.textContent = value || "";
    if (pill) {
      field.className = "pill " + pill;
    }
  }

  function ocultarTirillaCodigo() {
    if (codigoTirilla) {
      codigoTirilla.classList.add("hidden");
      codigoTirilla.setAttribute("hidden", "hidden");
    }
    if (codigoTirillaPanel) {
      codigoTirillaPanel.innerHTML = "";
    }
  }

  async function cargarTirillaCodigo(url) {
    if (!codigoTirilla || !codigoTirillaPanel || !url) {
      ocultarTirillaCodigo();
      return;
    }
    codigoTirilla.classList.remove("hidden");
    codigoTirilla.removeAttribute("hidden");
    codigoTirillaPanel.textContent = codigoTirilla.getAttribute("data-cargando") || "";
    const response = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
    if (!response.ok) {
      codigoTirillaPanel.textContent = codigoTirilla.getAttribute("data-error") || "";
      return;
    }
    codigoTirillaPanel.innerHTML = await response.text();
    fillReceiptRules(codigoTirillaPanel);
    requestAnimationFrame(function () {
      fillReceiptRules(codigoTirillaPanel);
    });
    codigoTirillaPanel.querySelectorAll("[data-print-ticket]").forEach(function (el) {
      el.addEventListener("click", function () {
        fillReceiptRules(codigoTirillaPanel);
        window.print();
      });
    });
  }

  function openCodigo(button) {
    if (!codigoDialog) {
      return;
    }
    codigoTrigger = button;
    fillCodigoField("consecutivo", button.getAttribute("data-consecutivo"));
    fillCodigoField("usuario", button.getAttribute("data-usuario"));
    fillCodigoField("pda", button.getAttribute("data-pda"));
    fillCodigoField("estado", button.getAttribute("data-estado"), button.getAttribute("data-estado-pill"));
    fillCodigoField("fecha-creacion", button.getAttribute("data-fecha-creacion"));
    fillCodigoField("fecha-descarga", button.getAttribute("data-fecha-descarga"));
    fillCodigoField("fecha-venta", button.getAttribute("data-fecha-venta"));
    fillCodigoField("fecha-registro", button.getAttribute("data-fecha-registro"));
    const tirillaUrl = button.getAttribute("data-tirilla-url");
    codigoDialog.classList.remove("hidden");
    codigoDialog.removeAttribute("hidden");
    if (tirillaUrl) {
      cargarTirillaCodigo(tirillaUrl).catch(function () {
        if (codigoTirillaPanel) {
          codigoTirillaPanel.textContent = codigoTirilla.getAttribute("data-error") || "";
        }
      });
    } else {
      ocultarTirillaCodigo();
    }
    if (codigoClose) {
      codigoClose.focus();
    }
  }

  document.querySelectorAll("[data-codigo-detalle]").forEach(function (el) {
    el.addEventListener("click", function () {
      openCodigo(el);
    });
  });
  if (codigoDialog) {
    codigoDialog.addEventListener("click", function (event) {
      if (event.target === codigoDialog) {
        closeCodigo();
      }
    });
  }
  if (codigoClose) {
    codigoClose.addEventListener("click", closeCodigo);
  }

  const aviso = document.getElementById("avisoDialog");
  const avisoText = document.getElementById("avisoDialogText");
  const avisoClose = aviso && aviso.querySelector("[data-close-dialog]");
  let avisoTrigger = null;

  function closeAviso() {
    if (!aviso) {
      return;
    }
    aviso.classList.add("hidden");
    aviso.setAttribute("hidden", "hidden");
    if (avisoTrigger && typeof avisoTrigger.focus === "function") {
      avisoTrigger.focus({ preventScroll: true });
    }
    avisoTrigger = null;
  }

  function openAviso(text, trigger) {
    if (!aviso || !avisoText) {
      return;
    }
    avisoTrigger = trigger || document.activeElement;
    avisoText.textContent = text;
    aviso.classList.remove("hidden");
    aviso.removeAttribute("hidden");
    if (avisoClose) {
      avisoClose.focus({ preventScroll: true });
    }
  }
  window.openAviso = openAviso;

  document.querySelectorAll("[data-aviso]").forEach(function (el) {
    el.addEventListener("click", function () {
      openAviso(el.getAttribute("data-aviso") || "", el);
    });
  });
  if (aviso) {
    aviso.addEventListener("click", function (event) {
      if (event.target === aviso) {
        closeAviso();
      }
    });
  }
  if (avisoClose) {
    avisoClose.addEventListener("click", closeAviso);
  }
  document.addEventListener("keydown", function (event) {
    if (event.key !== "Escape") {
      return;
    }
    if (ticketDialog && !ticketDialog.classList.contains("hidden")) {
      closeTicket();
      return;
    }
    if (usuarioDialog && !usuarioDialog.classList.contains("hidden")) {
      closeUsuario();
      return;
    }
    if (codigoDialog && !codigoDialog.classList.contains("hidden")) {
      closeCodigo();
      return;
    }
    if (aviso && !aviso.classList.contains("hidden")) {
      closeAviso();
      return;
    }
    document.querySelectorAll("[data-bell-menu][open], [data-cuenta-menu][open]").forEach(function (menu) {
      menu.removeAttribute("open");
    });
  });
  if (aviso && !aviso.classList.contains("hidden") && avisoClose) {
    avisoClose.focus({ preventScroll: true });
  }

  document.querySelectorAll("[data-chat-scroll]").forEach(function (el) {
    el.scrollTop = el.scrollHeight;
  });

  document.addEventListener("click", function (event) {
    document.querySelectorAll("[data-bell-menu][open], [data-cuenta-menu][open]").forEach(function (menu) {
      if (!menu.contains(event.target)) {
        menu.removeAttribute("open");
      }
    });
  });

  iniciarNotificacionesEnVivo();
})();

function iniciarNotificacionesEnVivo() {
  if (iniciarNotificacionesEnVivo.ocupado) {
    return;
  }
  iniciarNotificacionesEnVivo.ocupado = true;
  const conexionUrl = document.documentElement.getAttribute("data-notif-conexion");
  const toast = document.getElementById("notifToast");
  if (!conexionUrl || !toast || typeof signalR === "undefined") {
    iniciarNotificacionesEnVivo.ocupado = false;
    return;
  }

  const texto = toast.querySelector("[data-toast-text]");
  const enlace = toast.querySelector("[data-toast-link]");
  const duracion = parseInt(toast.getAttribute("data-toast-ms") || "3000", 10);
  let ocultarTimer = 0;

  function urlVer(id) {
    const plantilla = document.querySelector("[data-bell-menu]") && document.querySelector("[data-bell-menu]").getAttribute("data-ver-plantilla");
    if (!plantilla) {
      return "/Notificaciones/Ver/" + id;
    }
    return plantilla.replace(/00000000-0000-0000-0000-000000000000/i, id);
  }

  function actualizarCampana(aviso) {
    const menu = document.querySelector("[data-bell-menu]");
    if (!menu) {
      return;
    }
    const pendientes = (parseInt(menu.getAttribute("data-pendientes") || "0", 10) || 0) + 1;
    menu.setAttribute("data-pendientes", String(pendientes));
    const badge = menu.querySelector("[data-bell-count]");
    const resumen = menu.querySelector(".bell");
    if (badge) {
      badge.textContent = pendientes > 99 ? "99+" : String(pendientes);
      badge.classList.remove("hidden");
    }
    if (resumen) {
      const plantilla = menu.getAttribute("data-aria-plantilla") || "{0}";
      resumen.setAttribute("aria-label", plantilla.replace("{0}", String(pendientes)));
    }
    const vacio = menu.querySelector("[data-bell-empty]");
    const lista = menu.querySelector("[data-bell-list]");
    if (vacio) {
      vacio.classList.add("hidden");
    }
    if (lista) {
      const item = document.createElement("li");
      const ancla = document.createElement("a");
      ancla.href = urlVer(aviso.notificacionId || aviso.NotificacionId);
      const titulo = document.createElement("strong");
      titulo.textContent = aviso.tipo || aviso.Tipo || "";
      const mensaje = document.createElement("span");
      mensaje.textContent = aviso.mensaje || aviso.Mensaje || "";
      ancla.appendChild(titulo);
      ancla.appendChild(mensaje);
      item.appendChild(ancla);
      lista.insertBefore(item, lista.firstChild);
      while (lista.children.length > 8) {
        lista.removeChild(lista.lastChild);
      }
    }
  }

  function mostrarToast(aviso) {
    const mensaje = aviso.mensaje || aviso.Mensaje || "";
    const id = aviso.notificacionId || aviso.NotificacionId;
    if (texto) {
      texto.textContent = mensaje;
    }
    if (enlace && id) {
      enlace.setAttribute("href", urlVer(id));
    }
    toast.classList.remove("hidden");
    window.clearTimeout(ocultarTimer);
    ocultarTimer = window.setTimeout(function () {
      toast.classList.add("hidden");
    }, duracion);
  }

  fetch(conexionUrl, { credentials: "same-origin" })
    .then(function (respuesta) {
      if (!respuesta.ok) {
        throw new Error("conexion");
      }
      return respuesta.json();
    })
    .then(function (datos) {
      if (!datos || !datos.hubUrl || !datos.token) {
        throw new Error("conexion");
      }
      const conexion = new signalR.HubConnectionBuilder()
        .withUrl(datos.hubUrl, { accessTokenFactory: function () { return datos.token; } })
        .withAutomaticReconnect()
        .build();
      conexion.on("nuevaNotificacion", function (aviso) {
        const tipo = aviso.tipo || aviso.Tipo || "";
        const enSoporte = document.body.getAttribute("data-nav") === "soporte"
          || document.body.getAttribute("data-nav") === "soporte-tecnico";
        if ((tipo === "ChatSoporte" || tipo === "ChatSoporteTecnico") && enSoporte) {
          // En Soporte el toast se omite: el chat vivo va por ChatHub.
          // Si el hub de chat falló, recargar muestra el mensaje persistido.
          if (!document.documentElement.getAttribute("data-chat-vivo-ok")) {
            window.location.reload();
          }
          return;
        }
        actualizarCampana(aviso);
        mostrarToast(aviso);
      });
      function arrancar() {
        return conexion.start().catch(function () {
          window.setTimeout(arrancar, 4000);
        });
      }
      return arrancar();
    })
    .catch(function () {
      iniciarNotificacionesEnVivo.ocupado = false;
      window.setTimeout(iniciarNotificacionesEnVivo, 8000);
    });
}

function iniciarDiasVenta() {
  const root = document.getElementById("dias-venta");
  if (!root) {
    return;
  }

  const lista = root.querySelector(".dias-venta-lista");
  const resumen = document.getElementById("dias-venta-resumen");
  const buscador = document.getElementById("buscar-dia-loteria");
  const panel = root.querySelector(".dias-venta-panel");
  const sufijo = (panel && panel.getAttribute("data-resumen")) || "";
  const tabs = Array.from(root.querySelectorAll(".dias-venta-tab"));

  function filasDelDia(dia) {
    return Array.from(root.querySelectorAll('.dias-venta-fila[data-dia="' + dia + '"]'));
  }

  function actualizarResumen() {
    if (!lista || !resumen) {
      return;
    }
    const dia = lista.getAttribute("data-dia-activo") || "1";
    const visibles = filasDelDia(dia).filter(function (fila) {
      return !fila.classList.contains("is-filtered");
    });
    const marcadas = visibles.filter(function (fila) {
      const caja = fila.querySelector('input[type="checkbox"]');
      return caja && caja.checked;
    }).length;
    resumen.textContent = marcadas + " de " + visibles.length + " " + sufijo;
  }

  function mostrarDia(dia) {
    if (!lista) {
      return;
    }
    lista.setAttribute("data-dia-activo", dia);
    lista.setAttribute("aria-labelledby", "dia-tab-" + dia);
    tabs.forEach(function (tab) {
      const activo = tab.getAttribute("data-dia") === dia;
      tab.classList.toggle("is-active", activo);
      tab.setAttribute("aria-selected", activo ? "true" : "false");
    });
    actualizarResumen();
  }

  tabs.forEach(function (tab) {
    tab.addEventListener("click", function () {
      mostrarDia(tab.getAttribute("data-dia") || "1");
    });
    tab.addEventListener("keydown", function (event) {
      const indice = tabs.indexOf(tab);
      if (event.key === "ArrowRight") {
        event.preventDefault();
        tabs[(indice + 1) % tabs.length].focus();
        tabs[(indice + 1) % tabs.length].click();
      }
      if (event.key === "ArrowLeft") {
        event.preventDefault();
        tabs[(indice - 1 + tabs.length) % tabs.length].focus();
        tabs[(indice - 1 + tabs.length) % tabs.length].click();
      }
    });
  });

  root.addEventListener("change", function (event) {
    if (event.target && event.target.matches('.dias-venta-fila input[type="checkbox"]')) {
      actualizarResumen();
    }
  });

  if (buscador) {
    buscador.addEventListener("input", function () {
      const q = buscador.value.toLowerCase().trim();
      root.querySelectorAll(".dias-venta-fila").forEach(function (fila) {
        const nombre = (fila.getAttribute("data-nombre") || "").toLowerCase();
        fila.classList.toggle("is-filtered", q.length > 0 && nombre.indexOf(q) === -1);
      });
      actualizarResumen();
    });
  }

  mostrarDia("1");
}

iniciarDiasVenta();

function iniciarFiltroHoraFin(root) {
  const tabs = Array.from(root.querySelectorAll("[role='tab']"));
  const filas = Array.from(root.querySelectorAll("tbody tr[data-hora-fin-jornada]"));
  const vacio = root.querySelector(".topes-jornada-vacio");
  const panel = root.querySelector(".table-wrap");
  const formFiltro = root.querySelector("form[method='get']");

  function aplicar(clave, tabActivo) {
    let visibles = 0;
    filas.forEach(function (fila) {
      const coincide = !clave || fila.getAttribute("data-hora-fin-jornada") === clave;
      fila.hidden = !coincide;
      if (coincide) {
        visibles += 1;
      }
    });
    tabs.forEach(function (tab) {
      const activo = tab === tabActivo;
      tab.classList.toggle("is-active", activo);
      tab.setAttribute("aria-selected", activo ? "true" : "false");
      tab.tabIndex = activo ? 0 : -1;
    });
    if (panel) {
      panel.hidden = visibles === 0;
      if (tabActivo) {
        panel.setAttribute("aria-labelledby", tabActivo.id);
      }
    }
    if (vacio) {
      vacio.hidden = visibles > 0;
    }
  }

  function activar(tab) {
    const clave = tab.getAttribute("data-hora-fin-jornada") || "";
    if (formFiltro) {
      const hidden = formFiltro.querySelector("input[name='jornada']");
      if (hidden) {
        hidden.value = clave;
      }
      formFiltro.requestSubmit();
      return;
    }
    aplicar(clave, tab);
  }

  tabs.forEach(function (tab) {
    tab.tabIndex = tab.classList.contains("is-active") ? 0 : -1;
    tab.addEventListener("click", function () {
      activar(tab);
    });
    tab.addEventListener("keydown", function (evento) {
      if (evento.key !== "ArrowRight" && evento.key !== "ArrowLeft") {
        return;
      }
      evento.preventDefault();
      const actual = tabs.indexOf(tab);
      const direccion = evento.key === "ArrowRight" ? 1 : -1;
      const siguiente = tabs[(actual + direccion + tabs.length) % tabs.length];
      siguiente.focus();
      activar(siguiente);
    });
  });
  const inicial = tabs.find(function (tab) {
    return tab.classList.contains("is-active");
  }) || tabs[0];
  if (inicial && !formFiltro) {
    aplicar(inicial.getAttribute("data-hora-fin-jornada") || "", inicial);
  }
}

document.querySelectorAll("[data-topes-jornadas], [data-catalogo-jornadas]").forEach(iniciarFiltroHoraFin);

function iniciarHorarioFinJornada() {
  document.querySelectorAll("form[data-hora-fin-jornada]").forEach(function (form) {
    const select = form.querySelector("[data-jornada-horario]");
    const horaFin = form.querySelector("[data-hora-fin]");
    if (!select || !horaFin) {
      return;
    }
    const mensaje = horaFin.getAttribute("data-hora-fin-mensaje") || "";

    function validar() {
      if (horaFin.min && horaFin.max && horaFin.value && (horaFin.value < horaFin.min || horaFin.value > horaFin.max)) {
        horaFin.setCustomValidity(mensaje);
        return;
      }
      horaFin.setCustomValidity("");
    }

    function aplicar() {
      const opcion = select.options[select.selectedIndex];
      const min = opcion ? opcion.getAttribute("data-jornada-min") : "";
      const max = opcion ? opcion.getAttribute("data-jornada-max") : "";
      if (!min || !max) {
        horaFin.removeAttribute("min");
        horaFin.removeAttribute("max");
        horaFin.setCustomValidity("");
        return;
      }
      horaFin.min = min;
      horaFin.max = max;
      if (horaFin.value && (horaFin.value < min || horaFin.value > max)) {
        horaFin.value = "";
      }
      validar();
    }

    select.addEventListener("change", aplicar);
    horaFin.addEventListener("input", validar);
    horaFin.addEventListener("change", validar);
    aplicar();
  });
}

iniciarHorarioFinJornada();

(function () {
  const dialog = document.getElementById("confirmacionDialog");
  const form = document.getElementById("confirmacionForm");
  const password = document.getElementById("confirmacionPassword");
  const error = document.getElementById("confirmacionError");
  const cancelar = dialog && dialog.querySelector("[data-confirmacion-cancelar]");
  const url = document.documentElement.getAttribute("data-confirmacion-url");
  const errorValidacion = document.documentElement.getAttribute("data-confirmacion-error") || "";
  const claveToken = "nr.confirmacion.token";
  const claveAccion = "nr.confirmacion.accion";
  let pendiente = null;
  let origen = null;

  function mostrarError(texto) {
    if (!error) {
      return;
    }
    error.textContent = texto || errorValidacion;
    error.classList.remove("hidden");
    error.removeAttribute("hidden");
  }

  function ocultarError() {
    if (!error) {
      return;
    }
    error.textContent = "";
    error.classList.add("hidden");
    error.setAttribute("hidden", "hidden");
  }

  function cerrar(token) {
    if (!dialog) {
      return;
    }
    dialog.classList.add("hidden");
    dialog.setAttribute("hidden", "hidden");
    if (password) {
      password.value = "";
    }
    ocultarError();
    const resolver = pendiente;
    pendiente = null;
    if (resolver) {
      resolver(token || null);
    }
    if (origen && typeof origen.focus === "function") {
      origen.focus({ preventScroll: true });
    }
    origen = null;
    if (!token && typeof window.NewRichOlvidarScroll === "function") {
      window.NewRichOlvidarScroll();
    }
  }

  function abrir() {
    if (!dialog || !password) {
      return Promise.resolve(null);
    }
    origen = document.activeElement;
    ocultarError();
    password.value = "";
    dialog.classList.remove("hidden");
    dialog.removeAttribute("hidden");
    password.focus();
    return new Promise(function (resolver) {
      pendiente = resolver;
    });
  }

  function usosDe(el) {
    const fijo = el.getAttribute("data-protected-usos");
    if (fijo) {
      return Math.max(1, parseInt(fijo, 10) || 1);
    }
    if (el.hasAttribute("data-post-ids")) {
      return Math.max(1, el.querySelectorAll('input[name="ids"]').length);
    }
    if (el.hasAttribute("data-ids-query")) {
      const coincidencias = (el.getAttribute("href") || "").match(/ids=/g);
      return coincidencias ? coincidencias.length : 1;
    }
    return 1;
  }

  function asegurarCampo(destino, token) {
    let input = destino.querySelector('input[name="confirmacionToken"]');
    if (!input) {
      input = document.createElement("input");
      input.type = "hidden";
      input.name = "confirmacionToken";
      destino.appendChild(input);
    }
    input.value = token;
  }

  function pedirConfirmacion(accion, usos) {
    if (!accion || !url || !form) {
      return Promise.resolve(null);
    }
    const solicitud = abrir();
    form.dataset.accion = accion;
    form.dataset.usos = String(usos || 1);
    return solicitud;
  }

  window.NewRichPedirConfirmacion = pedirConfirmacion;

  if (form) {
    form.addEventListener("submit", function (event) {
      event.preventDefault();
      ocultarError();
      const datos = new FormData(form);
      datos.set("accion", form.dataset.accion || "");
      datos.set("usos", form.dataset.usos || "1");
      const boton = form.querySelector('button[type="submit"]');
      if (boton) {
        boton.disabled = true;
      }
      fetch(url, {
        method: "POST",
        credentials: "same-origin",
        headers: { "X-Requested-With": "XMLHttpRequest" },
        body: datos
      }).then(function (respuesta) {
        if (respuesta.status === 401) {
          window.location.reload();
          return null;
        }
        return respuesta.json();
      }).then(function (data) {
        if (!data) {
          return;
        }
        if (data.ok && data.token) {
          cerrar(data.token);
          return;
        }
        mostrarError(data.mensaje || errorValidacion);
        if (password) {
          password.focus();
          password.select();
        }
      }).catch(function () {
        mostrarError(errorValidacion);
      }).finally(function () {
        if (boton) {
          boton.disabled = false;
        }
      });
    });
  }

  if (cancelar) {
    cancelar.addEventListener("click", function () {
      cerrar(null);
    });
  }

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape" && dialog && !dialog.classList.contains("hidden")) {
      event.preventDefault();
      cerrar(null);
    }
  });

  document.querySelectorAll("form[data-protected-reuse]").forEach(function (protegido) {
    const accion = protegido.getAttribute("data-protected-action");
    if (sessionStorage.getItem(claveAccion) === accion && sessionStorage.getItem(claveToken)) {
      asegurarCampo(protegido, sessionStorage.getItem(claveToken));
      protegido.dataset.protegidoOk = "1";
      sessionStorage.removeItem(claveToken);
      sessionStorage.removeItem(claveAccion);
    }
  });

  document.addEventListener("submit", function (event) {
    const destino = event.target;
    if (!(destino instanceof HTMLFormElement)) {
      return;
    }
    const accion = destino.getAttribute("data-protected-action");
    if (!accion || destino.dataset.protegidoOk === "1") {
      return;
    }
    if (destino.hasAttribute("data-horarios-catalogo")) {
      const botonHorarios = document.querySelector("[data-guardar-horarios]");
      if (!botonHorarios || botonHorarios.disabled) {
        event.preventDefault();
        event.stopImmediatePropagation();
        return;
      }
    }
    event.preventDefault();
    event.stopImmediatePropagation();
    pedirConfirmacion(accion, usosDe(destino)).then(function (token) {
      if (!token) {
        return;
      }
      asegurarCampo(destino, token);
      destino.dataset.protegidoOk = "1";
      if (typeof destino.requestSubmit === "function") {
        destino.requestSubmit();
      } else {
        destino.submit();
      }
    });
  }, true);

  document.addEventListener("click", function (event) {
    const enlace = event.target.closest("a[data-protected-action]");
    if (!enlace || enlace.getAttribute("aria-disabled") === "true") {
      return;
    }
    event.preventDefault();
    event.stopImmediatePropagation();
    const accion = enlace.getAttribute("data-protected-action");
    pedirConfirmacion(accion, usosDe(enlace)).then(function (token) {
      if (!token) {
        return;
      }
      sessionStorage.setItem(claveToken, token);
      sessionStorage.setItem(claveAccion, accion);
      window.location.href = enlace.getAttribute("href") || "#";
    });
  }, true);

  function soloDigitos(texto) {
    return String(texto || "").replace(/\D/g, "");
  }

  function conMiles(digitos) {
    const limpio = soloDigitos(digitos);
    if (!limpio) {
      return "";
    }
    return limpio.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
  }

  function digitosAntesDelCursor(valor, cursor) {
    return soloDigitos(valor.slice(0, Math.max(0, cursor || 0))).length;
  }

  function posicionDesdeDigitos(valorFormateado, cantidadDigitos) {
    if (cantidadDigitos <= 0) {
      return 0;
    }
    let vistos = 0;
    for (let i = 0; i < valorFormateado.length; i++) {
      if (/\d/.test(valorFormateado[i])) {
        vistos++;
        if (vistos === cantidadDigitos) {
          return i + 1;
        }
      }
    }
    return valorFormateado.length;
  }

  function sincronizarMiles(visible) {
    const grupo = visible.closest(".money-input") || visible.parentElement;
    const oculto = grupo ? grupo.querySelector('input[type="hidden"]') : null;
    const digitosAntes = digitosAntesDelCursor(visible.value, visible.selectionStart);
    const digitos = soloDigitos(visible.value);
    const formateado = conMiles(digitos);
    visible.value = formateado;
    if (oculto) {
      oculto.value = digitos === "" ? "0" : digitos;
    }
    const nuevaPos = posicionDesdeDigitos(formateado, digitosAntes);
    try {
      visible.setSelectionRange(nuevaPos, nuevaPos);
    } catch (error) {
    }
  }

  function enlazarCampoMiles(visible) {
    if (!(visible instanceof HTMLInputElement) || visible.dataset.milesListo === "1") {
      return;
    }
    visible.dataset.milesListo = "1";
    sincronizarMiles(visible);
    visible.addEventListener("input", function () {
      sincronizarMiles(visible);
    });
    visible.addEventListener("blur", function () {
      sincronizarMiles(visible);
    });
  }

  function enlazarCamposMiles(raiz) {
    (raiz || document).querySelectorAll("input[data-miles]").forEach(enlazarCampoMiles);
  }

  enlazarCamposMiles(document);
  document.addEventListener("focusin", function (event) {
    const visible = event.target;
    if (visible instanceof HTMLInputElement && visible.hasAttribute("data-miles")) {
      enlazarCampoMiles(visible);
    }
  });
})();

(function () {
  const form = document.getElementById("version-aplicacion");
  if (!form) {
    return;
  }

  const caja = document.getElementById("version-progreso");
  const texto = document.getElementById("version-progreso-texto");
  const barra = document.getElementById("version-progreso-barra");
  const boton = form.querySelector('button[type="submit"]');
  const maximo = Number(form.getAttribute("data-tamano-maximo")) || 0;

  function mensaje(clave, valor) {
    const plantilla = form.getAttribute(clave) || "";
    return plantilla.replace("{0}", valor == null ? "" : String(valor));
  }

  function mostrar(porcentaje, leyenda, estado) {
    if (!caja || !texto || !barra) {
      return;
    }
    caja.hidden = false;
    caja.classList.remove("is-guardando", "is-error", "is-ok");
    if (estado) {
      caja.classList.add(estado);
    }
    texto.textContent = leyenda;
    const ancho = Math.max(0, Math.min(100, porcentaje));
    barra.style.width = ancho + "%";
    barra.setAttribute("aria-valuenow", String(Math.round(ancho)));
  }

  function liberar() {
    form.dataset.subiendo = "";
    form.dataset.protegidoOk = "";
    form.removeAttribute("aria-busy");
    if (boton) {
      boton.disabled = false;
    }
  }

  form.addEventListener("submit", function (event) {
    if (form.dataset.protegidoOk !== "1" || form.dataset.subiendo === "1") {
      return;
    }

    const archivo = form.querySelector('input[type="file"]');
    const elegido = archivo && archivo.files && archivo.files[0];
    if (elegido && maximo > 0 && elegido.size > maximo) {
      event.preventDefault();
      event.stopPropagation();
      mostrar(0, mensaje("data-msg-tamano"), "is-error");
      liberar();
      return;
    }

    if (typeof form.reportValidity === "function" && !form.reportValidity()) {
      event.preventDefault();
      liberar();
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    form.dataset.subiendo = "1";
    form.setAttribute("aria-busy", "true");
    if (boton) {
      boton.disabled = true;
    }
    mostrar(0, mensaje("data-msg-subiendo", 0), "");

    const xhr = new XMLHttpRequest();
    xhr.open("POST", form.action);
    xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");
    xhr.upload.addEventListener("progress", function (progreso) {
      if (!progreso.lengthComputable || progreso.total <= 0) {
        return;
      }
      const porcentaje = Math.min(99, Math.round((progreso.loaded / progreso.total) * 100));
      mostrar(porcentaje, mensaje("data-msg-subiendo", porcentaje), "");
    });
    xhr.upload.addEventListener("load", function () {
      mostrar(100, mensaje("data-msg-guardando"), "is-guardando");
    });
    xhr.addEventListener("load", function () {
      const cuerpo = xhr.responseText || "";
      const esIngreso = cuerpo.indexOf('id="login-password"') >= 0 || xhr.status === 401;
      if (esIngreso) {
        mostrar(0, mensaje("data-msg-sesion"), "is-error");
        liberar();
        return;
      }
      if (xhr.status === 413) {
        mostrar(0, mensaje("data-msg-tamano"), "is-error");
        liberar();
        return;
      }
      if (xhr.status < 200 || xhr.status >= 400) {
        mostrar(0, mensaje("data-msg-error"), "is-error");
        liberar();
        return;
      }
      const doc = new DOMParser().parseFromString(cuerpo, "text/html");
      const error = doc.querySelector(".notice.error");
      const ok = doc.querySelector(".notice.ok");
      if (error && !ok) {
        mostrar(0, error.textContent.trim() || mensaje("data-msg-error"), "is-error");
        liberar();
        return;
      }
      mostrar(100, mensaje("data-msg-lista"), "is-ok");
      window.setTimeout(function () {
        document.open();
        document.write(cuerpo);
        document.close();
      }, 400);
    });
    xhr.addEventListener("error", function () {
      mostrar(0, mensaje("data-msg-error"), "is-error");
      liberar();
    });
    xhr.send(new FormData(form));
  });
})();

(function () {
  const KEY_HORARIOS = "nr-horarios-pendientes";
  function horaClave(valor) {
    const partes = String(valor || "").match(/(\d{1,2}):(\d{2})/);
    if (!partes) {
      return "";
    }
    return partes[1].padStart(2, "0") + ":" + partes[2];
  }
  function horarioCambio(inicio, fin) {
    return horaClave(inicio.value) !== horaClave(inicio.getAttribute("data-original"))
      || horaClave(fin.value) !== horaClave(fin.getAttribute("data-original"));
  }
  function leerHorarios() {
    try {
      return JSON.parse(sessionStorage.getItem(KEY_HORARIOS) || "{}");
    } catch (e) {
      return {};
    }
  }
  function guardarHorarios(mapa) {
    sessionStorage.setItem(KEY_HORARIOS, JSON.stringify(mapa));
  }
  function capturarHorariosPagina() {
    const mapa = leerHorarios();
    const porId = {};
    document.querySelectorAll("[data-loteria-id][data-hora]").forEach(function (input) {
      const id = input.getAttribute("data-loteria-id");
      const campo = input.getAttribute("data-hora");
      if (!id || !campo) {
        return;
      }
      porId[id] = porId[id] || {};
      porId[id][campo] = input;
    });
    Object.keys(porId).forEach(function (id) {
      const inicio = porId[id].inicio;
      const fin = porId[id].fin;
      if (!inicio || !fin) {
        return;
      }
      if (horarioCambio(inicio, fin)) {
        mapa[id] = { inicio: horaClave(inicio.value), fin: horaClave(fin.value) };
      } else {
        delete mapa[id];
      }
    });
    guardarHorarios(mapa);
    return mapa;
  }
  function marcarHorarios(mapa) {
    document.querySelectorAll("#catalogo-horarios-loterias tr[data-row-id]").forEach(function (fila) {
      const inicio = fila.querySelector("[data-hora='inicio']");
      const fin = fila.querySelector("[data-hora='fin']");
      const id = fila.getAttribute("data-row-id");
      const cambio = !!(inicio && fin && (horarioCambio(inicio, fin) || (id && mapa[id])));
      fila.classList.toggle("is-horario-cambio", cambio);
    });
    const boton = document.querySelector("[data-guardar-horarios]");
    if (!boton) {
      return;
    }
    const hay = Object.keys(mapa).length > 0;
    boton.disabled = !hay;
    if (hay) {
      boton.removeAttribute("title");
    } else {
      boton.setAttribute("title", boton.getAttribute("data-titulo-desactivado") || "");
    }
  }
  function restaurarHorariosPagina() {
    const mapa = leerHorarios();
    document.querySelectorAll("[data-loteria-id][data-hora]").forEach(function (input) {
      const id = input.getAttribute("data-loteria-id");
      const campo = input.getAttribute("data-hora");
      if (id && mapa[id] && mapa[id][campo]) {
        input.value = mapa[id][campo];
      }
    });
    marcarHorarios(mapa);
  }
  const formHorariosGuardados = document.querySelector("[data-horarios-catalogo]");
  if (formHorariosGuardados && formHorariosGuardados.getAttribute("data-horarios-guardados") === "1") {
    sessionStorage.removeItem(KEY_HORARIOS);
  }
  restaurarHorariosPagina();
  document.querySelectorAll("[data-loteria-id][data-hora]").forEach(function (input) {
    input.addEventListener("input", function () {
      marcarHorarios(capturarHorariosPagina());
    });
    input.addEventListener("change", function () {
      marcarHorarios(capturarHorariosPagina());
    });
  });
  document.querySelectorAll("[data-horarios-pager] a").forEach(function (enlace) {
    enlace.addEventListener("click", capturarHorariosPagina);
  });
  const formHorarios = document.querySelector("[data-horarios-catalogo]");
  const botonHorarios = document.querySelector("[data-guardar-horarios]");
  if (botonHorarios) {
    botonHorarios.setAttribute("data-titulo-desactivado", botonHorarios.getAttribute("title") || "");
  }
  if (formHorarios) {
    formHorarios.addEventListener("submit", function (event) {
      const mapa = capturarHorariosPagina();
      marcarHorarios(mapa);
      if (Object.keys(mapa).length === 0) {
        event.preventDefault();
        event.stopImmediatePropagation();
        return;
      }
      if (formHorarios.dataset.protegidoOk !== "1") {
        return;
      }
      const extra = formHorarios.querySelector("[data-horarios-extra]");
      if (!extra) {
        event.preventDefault();
        return;
      }
      extra.innerHTML = "";
      let indice = 0;
      Object.keys(mapa).forEach(function (id) {
        const horas = mapa[id];
        if (!horas.inicio || !horas.fin) {
          return;
        }
        extra.insertAdjacentHTML("beforeend",
          '<input type="hidden" name="horarios[' + indice + '].LoteriaId" value="' + id + '" />' +
          '<input type="hidden" name="horarios[' + indice + '].HoraInicio" value="' + horas.inicio + '" />' +
          '<input type="hidden" name="horarios[' + indice + '].HoraFin" value="' + horas.fin + '" />');
        indice += 1;
      });
      if (indice === 0) {
        event.preventDefault();
        return;
      }
      formHorarios.querySelectorAll("input[name^='horarios']").forEach(function (campo) {
        if (!campo.closest("[data-horarios-extra]")) {
          campo.disabled = true;
        }
      });
    });
  }
})();

