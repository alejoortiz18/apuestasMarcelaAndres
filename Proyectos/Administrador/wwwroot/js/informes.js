(function () {
  "use strict";

  function normalizar(texto) {
    return (texto || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase().trim();
  }

  function leerDatos(id) {
    const el = document.getElementById(id);
    return el ? JSON.parse(el.textContent || "{}") : null;
  }

  function iniciar(dialog) {
    const form = dialog.querySelector("[data-informe-form]");
    if (!form) {
      return;
    }
    const conteos = dialog.querySelector("[data-informe-conteos]");
    const datos = conteos ? leerDatos("informeVendedoresDatos") : null;
    const guardar = form.querySelector('button[type="submit"]:not([form])');
    const periodo = dialog.querySelector("[data-informe-periodo]");
    let origen = null;

    const campos = Array.from(dialog.querySelectorAll("[data-informe-filtro]")).map(function (fs) {
      return {
        fs: fs,
        nombre: fs.getAttribute("data-informe-filtro"),
        lista: fs.querySelector("[data-informe-lista]"),
        buscar: fs.querySelector("[data-informe-buscar]"),
        opciones: Array.from(fs.querySelectorAll("[data-informe-opcion]")),
        sinOpciones: fs.querySelector("[data-informe-sin-opciones]"),
        contador: fs.querySelector("[data-informe-contador]"),
        error: fs.querySelector("[data-informe-error]")
      };
    });

    function campo(nombre) {
      return campos.find(function (c) { return c.nombre === nombre; });
    }

    function elegidos(c) {
      return Array.from(c.fs.querySelectorAll('input[type="checkbox"]:checked')).map(function (i) { return i.value.toLowerCase(); });
    }

    function especificos(c) {
      const modo = c.fs.querySelector("[data-informe-modo]:checked");
      return !!modo && modo.value === "elegidos";
    }

    function seleccion(c) {
      return c && especificos(c) ? new Set(elegidos(c)) : null;
    }

    function filtrarOpciones(c) {
      const termino = normalizar(c.buscar ? c.buscar.value : "");
      let visibles = 0;
      c.opciones.forEach(function (li) {
        const coincide = !termino || normalizar(li.getAttribute("data-texto")).includes(termino);
        li.hidden = !coincide;
        if (coincide) {
          visibles += 1;
        }
      });
      if (c.sinOpciones) {
        c.sinOpciones.hidden = visibles > 0;
      }
    }

    function mostrarError(fs, error, mensaje) {
      fs.classList.toggle("is-invalid", !!mensaje);
      if (error) {
        error.textContent = mensaje || "";
        error.hidden = !mensaje;
      }
    }

    function marcarError(c, mostrar) {
      mostrarError(c.fs, c.error, mostrar ? c.fs.getAttribute("data-error") : "");
    }

    function llenar(lista, filas, crear) {
      lista.replaceChildren();
      filas.forEach(function (fila) { lista.appendChild(crear(fila)); });
    }

    function conteo(texto, cantidad) {
      const li = document.createElement("li");
      const nombre = document.createElement("span");
      const valor = document.createElement("strong");
      nombre.textContent = texto;
      valor.textContent = String(cantidad);
      li.append(nombre, valor);
      return li;
    }

    function recalcular() {
      if (!conteos || !datos) {
        return;
      }
      const usuarios = datos.usuarios || [];
      const nombresTipo = datos.tipos || {};
      const nombresGrupo = datos.grupos || {};
      const ordenGrupo = new Map((datos.orden || []).map(function (clave, i) { return [clave.toLowerCase(), i]; }));
      const grupos = seleccion(campo("grupos"));
      const personas = seleccion(campo("usuarios"));
      const tipos = seleccion(campo("tipos"));

      const incluidos = usuarios.filter(function (u) {
        return (!grupos || grupos.has(String(u.grupo).toLowerCase()))
          && (!personas || personas.has(String(u.id).toLowerCase()))
          && (!tipos || tipos.has(String(u.tipo)));
      });

      conteos.querySelector("[data-informe-total]").textContent = String(usuarios.length);
      conteos.querySelector("[data-informe-cumplen]").textContent = String(incluidos.length);

      const listaTipos = (tipos ? Array.from(tipos).map(Number) : Array.from(new Set(incluidos.map(function (u) { return u.tipo; }))))
        .sort(function (a, b) { return a - b; });
      llenar(conteos.querySelector("[data-informe-por-tipo]"), listaTipos, function (tipo) {
        return conteo(nombresTipo[String(tipo)] || String(tipo), incluidos.filter(function (u) { return u.tipo === tipo; }).length);
      });

      const porGrupo = new Map();
      incluidos.filter(function (u) { return u.tipo === datos.vendedor; }).forEach(function (u) {
        const clave = String(u.grupo).toLowerCase();
        porGrupo.set(clave, (porGrupo.get(clave) || 0) + 1);
      });
      const clavesGrupo = Array.from(porGrupo.keys()).sort(function (a, b) {
        return (ordenGrupo.has(a) ? ordenGrupo.get(a) : Infinity) - (ordenGrupo.has(b) ? ordenGrupo.get(b) : Infinity);
      });
      const nombreGrupo = function (clave) {
        const encontrada = Object.keys(nombresGrupo).find(function (k) { return k.toLowerCase() === clave; });
        return encontrada ? nombresGrupo[encontrada] : clave;
      };
      llenar(conteos.querySelector("[data-informe-por-grupo]"), clavesGrupo, function (clave) {
        return conteo(nombreGrupo(clave), porGrupo.get(clave));
      });
      conteos.querySelector("[data-informe-por-grupo-vacio]").hidden = clavesGrupo.length > 0;

      const ordenados = incluidos.slice().sort(function (a, b) { return (a.nombre || "").localeCompare(b.nombre || "", "es", { sensitivity: "base" }); });
      llenar(conteos.querySelector("[data-informe-personas]"), ordenados, function (u) {
        const li = document.createElement("li");
        const nombre = document.createElement("strong");
        const detalle = document.createElement("small");
        nombre.textContent = u.nombre;
        detalle.textContent = [u.usuario, nombresTipo[String(u.tipo)] || "", nombreGrupo(String(u.grupo).toLowerCase())].join(" · ");
        li.append(nombre, detalle);
        return li;
      });
      conteos.querySelector("[data-informe-personas-vacio]").hidden = ordenados.length > 0;
    }

    function esRango() {
      const marcado = periodo ? periodo.querySelector("[data-informe-periodo-opcion]:checked") : null;
      return !!marcado && marcado.value === "rango";
    }

    function errorRango() {
      if (!periodo || !esRango()) {
        return "";
      }
      const desde = periodo.querySelector("[data-informe-desde]").value;
      const hasta = periodo.querySelector("[data-informe-hasta]").value;
      if (!desde || !hasta) {
        return periodo.getAttribute("data-incompleto");
      }
      if (desde > hasta) {
        return periodo.getAttribute("data-invalido");
      }
      const dias = Math.round((Date.parse(hasta) - Date.parse(desde)) / 86400000) + 1;
      return dias > Number(periodo.getAttribute("data-dias")) ? periodo.getAttribute("data-maximo") : "";
    }

    function sincronizarPeriodo(validar) {
      if (!periodo) {
        return;
      }
      periodo.querySelector("[data-informe-rango]").hidden = !esRango();
      const error = periodo.querySelector("[data-informe-error]");
      if (validar || !errorRango()) {
        mostrarError(periodo, error, validar ? errorRango() : "");
      }
    }

    function sincronizar() {
      const plantilla = (conteos || dialog).getAttribute("data-seleccionados") || "{0}";
      campos.forEach(function (c) {
        if (c.lista) {
          c.lista.hidden = !especificos(c);
        }
        if (c.contador) {
          c.contador.textContent = plantilla.replace("{0}", String(elegidos(c).length));
        }
        if (!especificos(c) || elegidos(c).length > 0) {
          marcarError(c, false);
        }
        filtrarOpciones(c);
      });
      sincronizarPeriodo(false);
      recalcular();
    }

    function enfocables() {
      return Array.from(dialog.querySelectorAll('button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])'))
        .filter(function (el) { return el.getClientRects().length > 0; });
    }

    function abrir(boton) {
      origen = boton;
      dialog.hidden = false;
      dialog.classList.remove("hidden");
      sincronizar();
      const inicial = dialog.querySelector("[data-informe-periodo-opcion]:checked, [data-informe-modo]:checked") || enfocables()[0];
      if (inicial) {
        inicial.focus();
      }
    }

    function cerrar() {
      form.reset();
      campos.forEach(function (c) { marcarError(c, false); });
      if (periodo) {
        mostrarError(periodo, periodo.querySelector("[data-informe-error]"), "");
      }
      sincronizar();
      dialog.hidden = true;
      dialog.classList.add("hidden");
      if (origen) {
        origen.focus();
      }
    }

    document.querySelectorAll("[data-informe-abrir]").forEach(function (boton) {
      boton.addEventListener("click", function () {
        if (!boton.disabled && boton.getAttribute("data-informe-abrir") === dialog.getAttribute("data-informe-dialog")) {
          abrir(boton);
        }
      });
    });

    dialog.querySelectorAll("[data-informe-cerrar]").forEach(function (boton) {
      boton.addEventListener("click", cerrar);
    });

    form.addEventListener("change", sincronizar);

    campos.forEach(function (c) {
      if (c.buscar) {
        c.buscar.addEventListener("input", function () { filtrarOpciones(c); });
        c.buscar.addEventListener("keydown", function (event) {
          if (event.key === "Enter") {
            event.preventDefault();
          }
        });
      }
    });

    dialog.addEventListener("keydown", function (event) {
      if (event.key === "Escape") {
        event.preventDefault();
        event.stopPropagation();
        cerrar();
        return;
      }
      if (event.key !== "Tab") {
        return;
      }
      const lista = enfocables();
      if (lista.length === 0) {
        return;
      }
      const primero = lista[0];
      const ultimo = lista[lista.length - 1];
      if (event.shiftKey && document.activeElement === primero) {
        event.preventDefault();
        ultimo.focus();
      } else if (!event.shiftKey && document.activeElement === ultimo) {
        event.preventDefault();
        primero.focus();
      }
    });

    form.addEventListener("submit", function (event) {
      const invalidos = campos.filter(function (c) { return especificos(c) && elegidos(c).length === 0; });
      campos.forEach(function (c) { marcarError(c, invalidos.includes(c)); });
      const rango = errorRango();
      sincronizarPeriodo(true);
      if (rango || invalidos.length > 0) {
        event.preventDefault();
        const destino = rango
          ? periodo.querySelector("[data-informe-desde]")
          : invalidos[0].buscar || invalidos[0].fs.querySelector("input");
        if (destino) {
          destino.focus();
        }
        return;
      }
      if (guardar) {
        guardar.disabled = true;
      }
    });
  }

  document.querySelectorAll("[data-informe-dialog]").forEach(iniciar);

  const piezasPdf = ".informe-titulo-categoria, .informe-config, .informe-reporte-head, .informe-metrica, .informe-grafico, "
    + ".informe-bloque-head, .informe-tabla-head, .informe-dona-bloque, .pager, tr, li, p, h2, h3, h4, h5, dl > div";

  function colorRgb(texto) {
    const partes = (texto.match(/\d+(\.\d+)?/g) || ["255", "255", "255"]).slice(0, 3).map(Number);
    return partes.length === 3 ? partes : [255, 255, 255];
  }

  // Corta solo donde ninguna pieza (tarjeta, fila, gráfico, título) quede partida entre dos páginas.
  function paginar(alto, utilDe, piezas) {
    const libre = function (y) {
      return !piezas.some(function (p) { return p.arriba < y - 0.5 && p.abajo > y + 0.5; });
    };
    const cortes = Array.from(new Set(piezas.flatMap(function (p) { return [p.arriba, p.abajo]; })))
      .filter(function (y) { return y > 0 && y < alto && libre(y); })
      .sort(function (a, b) { return a - b; });
    const tramos = [];
    let inicio = 0;
    while (alto - inicio > 1) {
      const util = utilDe(tramos.length);
      let fin = Math.min(alto, inicio + util);
      if (fin < alto) {
        const mejor = cortes.filter(function (y) { return y > inicio + util * 0.25 && y <= fin; }).pop();
        if (mejor) {
          fin = mejor;
        }
      }
      tramos.push({ inicio: inicio, fin: fin });
      inicio = fin;
    }
    return tramos;
  }

  const estilosSvg = ["fill", "fill-opacity", "stroke", "stroke-width", "stroke-opacity", "stroke-linejoin", "stroke-linecap",
    "stroke-dasharray", "opacity", "font-family", "font-size", "font-weight"];

  // La captura no aplica a los SVG los estilos que vienen de clases: se fijan en línea mientras dura y luego se devuelven.
  function fijarEstilosSvg(seccion) {
    const previos = Array.from(seccion.querySelectorAll("svg *")).map(function (el) {
      const calculado = getComputedStyle(el);
      const previo = el.getAttribute("style");
      estilosSvg.forEach(function (p) { el.style.setProperty(p, calculado.getPropertyValue(p)); });
      return { el: el, previo: previo };
    });
    return function () {
      previos.forEach(function (p) {
        if (p.previo === null) {
          p.el.removeAttribute("style");
        } else {
          p.el.setAttribute("style", p.previo);
        }
      });
    };
  }

  function aDataUrl(blob) {
    return new Promise(function (resolver, rechazar) {
      const lector = new FileReader();
      lector.onload = function () { resolver(lector.result); };
      lector.onerror = rechazar;
      lector.readAsDataURL(blob);
    });
  }

  // La imagen de la captura no puede descargar letras externas: van incrustadas desde la copia local.
  function fuentesIncrustadas(ruta) {
    const base = new URL(ruta, window.location.href);
    return fetch(base).then(function (r) { return r.text(); }).then(function (css) {
      const urls = Array.from(new Set(Array.from(css.matchAll(/url\(([^)]+)\)/g), function (m) { return m[1]; })));
      return Promise.all(urls.map(function (url) {
        return fetch(new URL(url, base)).then(function (r) { return r.blob(); }).then(aDataUrl);
      })).then(function (datos) {
        return urls.reduce(function (texto, url, i) { return texto.split("url(" + url + ")").join("url(" + datos[i] + ")"); }, css);
      });
    });
  }

  function exportarPdf(boton) {
    const seccion = document.getElementById(boton.getAttribute("data-informe-exportar"));
    const resumen = document.getElementById(boton.getAttribute("data-resumen"));
    const encabezado = resumen ? resumen.querySelector("[data-pdf-encabezado]") : null;
    const filtros = resumen ? resumen.querySelector("[data-pdf-filtros]") : null;
    const fallo = function () {
      if (typeof window.openAviso === "function") {
        window.openAviso(boton.getAttribute("data-error"), boton);
      }
    };
    if (!seccion || !encabezado || !filtros || !window.htmlToImage || !window.jspdf) {
      fallo();
      return;
    }

    const texto = boton.textContent;
    boton.disabled = true;
    boton.setAttribute("aria-busy", "true");
    boton.textContent = boton.getAttribute("data-generando");
    seccion.classList.add("informe-exportando");
    const restaurarSvg = fijarEstilosSvg(seccion);

    const fondo = getComputedStyle(document.body).backgroundColor;
    const ancho = seccion.offsetWidth;
    const alto = seccion.offsetHeight;
    const origen = seccion.getBoundingClientRect().top;
    const piezas = Array.from(seccion.querySelectorAll(piezasPdf))
      .map(function (el) {
        const r = el.getBoundingClientRect();
        return { arriba: r.top - origen, abajo: r.bottom - origen };
      })
      .filter(function (p) { return p.abajo > p.arriba; });

    fuentesIncrustadas(boton.getAttribute("data-fuentes"))
      .then(function (fuentes) {
        return Promise.all([
          window.htmlToImage.toCanvas(encabezado, { pixelRatio: 2, fontEmbedCSS: fuentes }),
          window.htmlToImage.toCanvas(filtros, { pixelRatio: 2, fontEmbedCSS: fuentes }),
          window.htmlToImage.toCanvas(seccion, { pixelRatio: Math.min(2, 30000 / alto), fontEmbedCSS: fuentes })
        ]);
      })
      .then(function (lienzos) {
        const cabeza = lienzos[0];
        const resumenFiltros = lienzos[1];
        const lienzo = lienzos[2];
        const pdf = new window.jspdf.jsPDF({ orientation: "portrait", unit: "mm", format: "a4", compress: true });
        const pagina = { ancho: pdf.internal.pageSize.getWidth(), alto: pdf.internal.pageSize.getHeight(), margen: 10, pie: 8, espacio: 4 };
        const anchoUtil = pagina.ancho - pagina.margen * 2;
        const altoDe = function (c) { return c.height / c.width * anchoUtil; };
        const altoCabeza = altoDe(cabeza);
        const altoFiltros = altoDe(resumenFiltros);
        const mmPorPx = anchoUtil / ancho;
        const pxPorCss = lienzo.width / ancho;
        const relleno = colorRgb(fondo);
        const inicioDe = function (i) {
          const bajoCabeza = pagina.margen + altoCabeza + pagina.espacio;
          return i === 0 ? bajoCabeza + altoFiltros + pagina.espacio : bajoCabeza;
        };
        const utilDe = function (i) { return (pagina.alto - pagina.margen - pagina.pie - inicioDe(i)) / mmPorPx; };

        const tramos = paginar(alto, utilDe, piezas);
        tramos.forEach(function (tramo, i) {
          if (i > 0) {
            pdf.addPage();
          }
          pdf.setFillColor(relleno[0], relleno[1], relleno[2]);
          pdf.rect(0, 0, pagina.ancho, pagina.alto, "F");
          pdf.addImage(cabeza, "PNG", pagina.margen, pagina.margen, anchoUtil, altoCabeza, "cabeza", "FAST");
          if (i === 0) {
            pdf.addImage(resumenFiltros, "PNG", pagina.margen, pagina.margen + altoCabeza + pagina.espacio, anchoUtil, altoFiltros, undefined, "FAST");
          }
          const desde = Math.floor(tramo.inicio * pxPorCss);
          const corte = document.createElement("canvas");
          corte.width = lienzo.width;
          corte.height = Math.max(1, Math.min(lienzo.height, Math.ceil(tramo.fin * pxPorCss)) - desde);
          corte.getContext("2d").drawImage(lienzo, 0, desde, corte.width, corte.height, 0, 0, corte.width, corte.height);
          pdf.addImage(corte, "PNG", pagina.margen, inicioDe(i), anchoUtil, corte.height / pxPorCss * mmPorPx, undefined, "FAST");
          pdf.setFontSize(8);
          pdf.setTextColor(122, 116, 100);
          pdf.text(boton.getAttribute("data-titulo"), pagina.margen, pagina.alto - pagina.margen);
          pdf.text((i + 1) + " / " + tramos.length, pagina.ancho - pagina.margen, pagina.alto - pagina.margen, { align: "right" });
        });
        pdf.save(boton.getAttribute("data-archivo"));
      })
      .catch(fallo)
      .finally(function () {
        restaurarSvg();
        seccion.classList.remove("informe-exportando");
        boton.textContent = texto;
        boton.disabled = false;
        boton.removeAttribute("aria-busy");
      });
  }

  document.querySelectorAll("[data-informe-exportar]").forEach(function (boton) {
    boton.addEventListener("click", function () { exportarPdf(boton); });
  });
})();
