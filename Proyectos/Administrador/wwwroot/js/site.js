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
      const matches = Array.from(select.options).filter((opt) => {
        if (!q && opt.value === "") {
          return false;
        }
        return opt.text.toLowerCase().includes(q);
      });
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

  const codigoDialog = document.getElementById("codigoDialog");
  const codigoClose = codigoDialog && codigoDialog.querySelector("[data-close-codigo]");
  let codigoTrigger = null;

  function closeCodigo() {
    if (!codigoDialog) {
      return;
    }
    codigoDialog.classList.add("hidden");
    codigoDialog.setAttribute("hidden", "hidden");
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
    codigoDialog.classList.remove("hidden");
    codigoDialog.removeAttribute("hidden");
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
    if (codigoDialog && !codigoDialog.classList.contains("hidden")) {
      closeCodigo();
      return;
    }
    if (aviso && !aviso.classList.contains("hidden")) {
      closeAviso();
    }
  });
  if (aviso && !aviso.classList.contains("hidden") && avisoClose) {
    avisoClose.focus();
  }

  document.querySelectorAll("[data-chat-scroll]").forEach(function (el) {
    el.scrollTop = el.scrollHeight;
  });
})();
