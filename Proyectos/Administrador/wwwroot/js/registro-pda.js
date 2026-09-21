// Registro guiado de PDA. Muestra el avance de 0% a 100% y el resultado. El codigo unico del
// dispositivo nunca viaja hasta esta pantalla: lo genera la API y lo graba el servidor en el equipo.
(function () {
  const raiz = document.querySelector("[data-registro-pda]");
  if (!raiz) {
    return;
  }

  const pasoPreparar = raiz.querySelector('[data-paso="preparar"]');
  const pasoInstalar = raiz.querySelector('[data-paso="instalar"]');
  const botonContinuar = raiz.querySelector("[data-continuar]");
  const botonReintentar = raiz.querySelector("[data-reintentar]");
  const errorVerificacion = raiz.querySelector("[data-verificacion-error]");
  const estadoVerificacion = raiz.querySelector("[data-verificacion-estado]");
  const errorRegistro = raiz.querySelector("[data-registro-error]");
  const accionesError = raiz.querySelector("[data-acciones-error]");
  const barra = raiz.querySelector("[data-barra]");
  const relleno = raiz.querySelector("[data-barra-relleno]");
  const porcentaje = raiz.querySelector("[data-porcentaje]");
  const bitacora = raiz.querySelector("[data-bitacora]");
  const modelo = raiz.querySelector("[data-modelo]");
  const dialogo = document.getElementById("registroPdaDialog");
  const tipo = raiz.querySelector("[data-tipo]");
  const token = raiz.querySelector('input[name="__RequestVerificationToken"]');

  let conexion = null;
  let tokenRegistro = "";

  function mostrar(elemento, texto) {
    if (!elemento) {
      return;
    }
    if (texto !== undefined) {
      elemento.textContent = texto;
    }
    elemento.classList.remove("hidden");
    elemento.removeAttribute("hidden");
  }

  function ocultar(elemento) {
    if (!elemento) {
      return;
    }
    elemento.classList.add("hidden");
    elemento.setAttribute("hidden", "hidden");
  }

  function enviar(url, conConfirmacion) {
    const datos = new FormData();
    if (token) {
      datos.append("__RequestVerificationToken", token.value);
    }
    if (conexion && conexion.connectionId) {
      datos.append("conexionId", conexion.connectionId);
    }
    if (tipo) {
      datos.append("tipo", tipo.value);
    }
    const headers = { "X-Requested-With": "XMLHttpRequest" };
    if (conConfirmacion && tokenRegistro) {
      headers["X-Confirmacion-Token"] = tokenRegistro;
    }
    return fetch(url, {
      method: "POST",
      credentials: "same-origin",
      headers: headers,
      body: datos
    }).then(function (respuesta) {
      if (!respuesta.ok) {
        throw new Error("registro");
      }
      return respuesta.json();
    });
  }

  function pintarAvance(avance) {
    const valor = Math.max(0, Math.min(100, Number(avance.porcentaje) || 0));
    if (relleno) {
      relleno.style.width = valor + "%";
    }
    if (barra) {
      barra.setAttribute("aria-valuenow", String(valor));
    }
    if (porcentaje) {
      porcentaje.textContent = valor + "%";
    }
    if (bitacora && avance.mensaje) {
      const ultimo = bitacora.lastElementChild;
      if (!ultimo || ultimo.textContent !== avance.mensaje) {
        const paso = document.createElement("li");
        paso.textContent = avance.mensaje;
        bitacora.appendChild(paso);
      }
    }
  }

  function conectarHub() {
    const url = raiz.getAttribute("data-hub");
    if (!url || typeof signalR === "undefined") {
      return Promise.resolve();
    }
    conexion = new signalR.HubConnectionBuilder().withUrl(url).build();
    conexion.on("avanceRegistroPda", pintarAvance);
    return conexion.start().catch(function () {
      // Sin canal en vivo el proceso igual termina: el resultado llega en la respuesta HTTP.
      conexion = null;
    });
  }

  function verificar() {
    ocultar(errorVerificacion);
    mostrar(estadoVerificacion, raiz.getAttribute("data-texto-verificando"));
    botonContinuar.disabled = true;
    enviar(raiz.getAttribute("data-url-verificar"))
      .then(function (datos) {
        if (!datos.listo) {
          ocultar(estadoVerificacion);
          mostrar(errorVerificacion, datos.mensaje);
          botonContinuar.disabled = false;
          errorVerificacion.focus();
          return;
        }
        if (modelo && datos.modelo) {
          const plantilla = raiz.getAttribute("data-texto-modelo") || "{0}";
          modelo.textContent = plantilla.replace("{0}", datos.modelo);
        }
        ocultar(pasoPreparar);
        mostrar(pasoInstalar);
        registrar();
      })
      .catch(function () {
        ocultar(estadoVerificacion);
        mostrar(errorVerificacion, raiz.getAttribute("data-texto-error"));
        botonContinuar.disabled = false;
      });
  }

  function registrar() {
    ocultar(errorRegistro);
    ocultar(accionesError);
    if (bitacora) {
      bitacora.replaceChildren();
    }
    pintarAvance({ porcentaje: 0, mensaje: "" });
    enviar(raiz.getAttribute("data-url-registrar"), true)
      .then(function (datos) {
        if (!datos.exitoso) {
          mostrar(errorRegistro, datos.mensaje);
          mostrar(accionesError);
          errorRegistro.focus();
          return;
        }
        pintarAvance({ porcentaje: 100, mensaje: datos.mensaje });
        if (dialogo) {
          mostrar(dialogo);
          const aceptar = dialogo.querySelector("a.primary");
          if (aceptar) {
            aceptar.focus();
          }
        }
      })
      .catch(function () {
        mostrar(errorRegistro, raiz.getAttribute("data-texto-error"));
        mostrar(accionesError);
      });
  }

  if (errorVerificacion) {
    errorVerificacion.setAttribute("tabindex", "-1");
  }
  if (errorRegistro) {
    errorRegistro.setAttribute("tabindex", "-1");
  }

  function pedirRegistro(siguiente) {
    const pedir = window.NewRichPedirConfirmacion;
    if (typeof pedir !== "function") {
      return;
    }
    pedir("pda.registrar", 1).then(function (valor) {
      if (!valor) {
        return;
      }
      tokenRegistro = valor;
      siguiente();
    });
  }

  botonContinuar.addEventListener("click", function () {
    pedirRegistro(function () {
      conectarHub().then(verificar);
    });
  });

  if (botonReintentar) {
    botonReintentar.addEventListener("click", function () {
      pedirRegistro(registrar);
    });
  }
})();
