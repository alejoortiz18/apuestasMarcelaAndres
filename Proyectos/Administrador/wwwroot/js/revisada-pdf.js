(function () {
  "use strict";

  const boton = document.querySelector("[data-revisada-exportar]");
  if (!boton) {
    return;
  }

  // Cortes de página: ninguna de estas piezas queda partida entre dos hojas.
  const piezasPdf = ".stat, .toolbar .field, .revisada-parte-titulo, .revisada-previa-head, .revisada-nombre, "
    + ".revisada-loteria h2, .revisada-grupo, .revisada-totales h2, .revisada-totales-lista > div, "
    + ".revisada-historico-head, tr, .panel > p, .pager";
  const estilosSvg = ["fill", "fill-opacity", "stroke", "stroke-width", "stroke-opacity", "stroke-linejoin", "stroke-linecap",
    "stroke-dasharray", "opacity", "font-family", "font-size", "font-weight"];

  function colorRgb(texto) {
    const partes = (texto.match(/\d+(\.\d+)?/g) || ["255", "255", "255"]).slice(0, 3).map(Number);
    return partes.length === 3 ? partes : [255, 255, 255];
  }

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

  // La captura no aplica a los SVG los estilos que vienen de clases: se fijan en línea.
  function fijarEstilosSvg(raiz) {
    raiz.querySelectorAll("svg *").forEach(function (el) {
      const calculado = getComputedStyle(el);
      estilosSvg.forEach(function (p) { el.style.setProperty(p, calculado.getPropertyValue(p)); });
    });
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

  // El histórico carga de a 20 filas; el PDF lleva todas las del rango.
  async function filasHistoricas(seccion) {
    const filas = [];
    let pagina = 1;
    let hayMas = true;
    while (hayMas && pagina <= 500) {
      const datos = new URLSearchParams({
        desde: seccion.dataset.desde,
        hasta: seccion.dataset.hasta,
        orden: seccion.dataset.orden,
        descendente: seccion.dataset.descendente,
        pagina: String(pagina)
      });
      const respuesta = await fetch(seccion.dataset.url + "?" + datos.toString(), { credentials: "same-origin", headers: { "X-Requested-With": "fetch" } });
      if (!respuesta.ok) {
        throw new Error("historico");
      }
      const temporal = document.createElement("tbody");
      temporal.innerHTML = await respuesta.text();
      const fin = temporal.querySelector("[data-historico-fin]");
      if (!fin) {
        throw new Error("sesion");
      }
      hayMas = fin.getAttribute("data-hay-mas") === "true";
      fin.remove();
      filas.push.apply(filas, Array.from(temporal.children));
      pagina += 1;
    }
    return filas;
  }

  function armarCopia(original, filas) {
    const lienzo = document.createElement("div");
    lienzo.className = "revisada-pdf-lienzo";
    lienzo.style.width = original.offsetWidth + "px";
    lienzo.style.background = getComputedStyle(document.body).backgroundColor;
    const copia = original.cloneNode(true);
    copia.removeAttribute("id");
    copia.classList.add("revisada-exportando");
    copia.querySelectorAll("template").forEach(function (t) { t.remove(); });
    const cuerpo = copia.querySelector("[data-historico-filas]");
    if (cuerpo && filas) {
      filas.forEach(function (fila) { fila.querySelectorAll("template").forEach(function (t) { t.remove(); }); });
      cuerpo.replaceChildren.apply(cuerpo, filas);
    }
    lienzo.appendChild(copia);
    document.body.appendChild(lienzo);
    return { lienzo: lienzo, copia: copia };
  }

  async function exportar() {
    const original = document.getElementById("revisada-datos");
    const resumen = document.querySelector("[data-revisada-pdf-resumen]");
    const encabezado = resumen && resumen.querySelector("[data-pdf-encabezado]");
    const filtros = resumen && resumen.querySelector("[data-pdf-filtros]");
    if (!original || !encabezado || !filtros || !window.htmlToImage || !window.jspdf) {
      throw new Error("faltan piezas");
    }

    const seccionHistorico = original.querySelector("[data-historico]");
    const filas = seccionHistorico && seccionHistorico.querySelector("[data-historico-filas]")
      ? await filasHistoricas(seccionHistorico)
      : null;
    const armado = armarCopia(original, filas);
    try {
      const copia = armado.copia;
      fijarEstilosSvg(copia);
      const ancho = copia.offsetWidth;
      const alto = copia.offsetHeight;
      const origen = copia.getBoundingClientRect().top;
      const piezas = Array.from(copia.querySelectorAll(piezasPdf))
        .map(function (el) {
          const r = el.getBoundingClientRect();
          return { arriba: r.top - origen, abajo: r.bottom - origen };
        })
        .filter(function (p) { return p.abajo > p.arriba; });

      const fuentes = await fuentesIncrustadas(boton.getAttribute("data-fuentes"));
      const lienzos = await Promise.all([
        window.htmlToImage.toCanvas(encabezado, { pixelRatio: 2, fontEmbedCSS: fuentes }),
        window.htmlToImage.toCanvas(filtros, { pixelRatio: 2, fontEmbedCSS: fuentes }),
        window.htmlToImage.toCanvas(copia, { pixelRatio: Math.min(2, 30000 / alto), fontEmbedCSS: fuentes })
      ]);
      const cabeza = lienzos[0];
      const resumenFiltros = lienzos[1];
      const lienzo = lienzos[2];
      const fondo = getComputedStyle(document.body).backgroundColor;

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
    } finally {
      armado.lienzo.remove();
    }
  }

  boton.addEventListener("click", function () {
    if (boton.disabled) {
      return;
    }
    const texto = boton.textContent;
    boton.disabled = true;
    boton.setAttribute("aria-busy", "true");
    boton.textContent = boton.getAttribute("data-generando");
    exportar()
      .catch(function () {
        if (typeof window.openAviso === "function") {
          window.openAviso(boton.getAttribute("data-error"), boton);
        }
      })
      .finally(function () {
        boton.textContent = texto;
        boton.disabled = false;
        boton.removeAttribute("aria-busy");
      });
  });
})();
