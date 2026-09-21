(function () {
  const noResults = document.documentElement.getAttribute("data-no-results") || "No se encontraron resultados";
  const searchLabel = document.documentElement.getAttribute("data-search-option") || "Buscar una opción";

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
      grupo.hidden = rol.value !== rolConGrupo;
    }
    rol.addEventListener("change", sincronizar);
    sincronizar();
  }

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
      avisoTrigger.focus();
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
      avisoClose.focus();
    }
  }

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
    avisoClose.focus();
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
      origen.focus();
    }
    origen = null;
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
})();

