// Registro guiado de PDA. Muestra el avance de 0% a 100% y el resultado. El codigo unico lo genera
// la API y lo graba el servidor en el equipo; solo llega aqui al final, como nombre registrado.
(function () {
  const raiz = document.querySelector("[data-registro-pda]");
  if (!raiz) {
    return;
  }

  const pasoPreparar = raiz.querySelector('[data-paso="preparar"]');
  const pasoInstalar = raiz.querySelector('[data-paso="instalar"]');
  const botonContinuar = raiz.querySelector("[data-continuar]");
  const botonReintentar = raiz.querySelector("[data-reintentar]");
  const estadoVerificacion = raiz.querySelector("[data-verificacion-estado]");
  const accionesError = raiz.querySelector("[data-acciones-error]");
  const barra = raiz.querySelector("[data-barra]");
  const relleno = raiz.querySelector("[data-barra-relleno]");
  const porcentaje = raiz.querySelector("[data-porcentaje]");
  const bitacora = raiz.querySelector("[data-bitacora]");
  const modelo = raiz.querySelector("[data-modelo]");
  const dialogo = document.getElementById("registroPdaDialog");
  const tipo = raiz.querySelector("[data-tipo]");
  const clases = Array.prototype.slice.call(raiz.querySelectorAll("[data-clase]"));
  const token = raiz.querySelector('input[name="__RequestVerificationToken"]');
  const perfiles = tipo
    ? Array.prototype.map.call(tipo.options, function (opcion) {
        return {
          valor: opcion.value,
          texto: opcion.text,
          clases: (opcion.getAttribute("data-clases") || "").split(" ").filter(Boolean)
        };
      })
    : [];

  let conexion = null;
  let tokenRegistro = "";

  function claseElegida() {
    const marcada = clases.find(function (entrada) {
      return entrada.checked;
    });
    return marcada ? marcada.value : "";
  }

  // El PDA de venta y el celular admiten perfiles distintos, asi que la lista se arma
  // con los que acepta el equipo elegido y nunca ofrece una combinacion que el servidor rechaza.
  function sincronizarPerfiles() {
    if (!tipo || perfiles.length === 0) {
      return;
    }
    const clase = claseElegida();
    const permitidos = perfiles.filter(function (perfil) {
      return perfil.clases.indexOf(clase) >= 0;
    });
    if (permitidos.length === 0) {
      return;
    }
    const anterior = tipo.value;
    tipo.replaceChildren();
    permitidos.forEach(function (perfil) {
      const opcion = document.createElement("option");
      opcion.value = perfil.valor;
      opcion.text = perfil.texto;
      opcion.setAttribute("data-clases", perfil.clases.join(" "));
      tipo.appendChild(opcion);
    });
    const conserva = permitidos.some(function (perfil) {
      return perfil.valor === anterior;
    });
    tipo.value = conserva ? anterior : permitidos[0].valor;
    const buscador = tipo.parentNode
      ? tipo.parentNode.querySelector(".search-select-input")
      : null;
    if (buscador) {
      buscador.value = tipo.options[tipo.selectedIndex].text;
    }
  }

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

  function avisoError(texto) {
    if (typeof window.openAviso === "function" && texto) {
      window.openAviso(texto);
    }
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
    const clase = claseElegida();
    if (clase) {
      datos.append("clase", clase);
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
    mostrar(estadoVerificacion, raiz.getAttribute("data-texto-verificando"));
    botonContinuar.disabled = true;
    enviar(raiz.getAttribute("data-url-verificar"))
      .then(function (datos) {
        if (!datos.listo) {
          ocultar(estadoVerificacion);
          botonContinuar.disabled = false;
          avisoError(datos.mensaje);
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
        botonContinuar.disabled = false;
        avisoError(raiz.getAttribute("data-texto-error"));
      });
  }

  function registrar() {
    ocultar(accionesError);
    if (bitacora) {
      bitacora.replaceChildren();
    }
    pintarAvance({ porcentaje: 0, mensaje: "" });
    enviar(raiz.getAttribute("data-url-registrar"), true)
      .then(function (datos) {
        if (!datos.exitoso) {
          mostrar(accionesError);
          avisoError(datos.mensaje);
          return;
        }
        pintarAvance({ porcentaje: 100, mensaje: datos.mensaje });
        if (dialogo) {
          const nombre = dialogo.querySelector("[data-nombre-registrado]");
          const filaNombre = dialogo.querySelector("[data-nombre-registrado-fila]");
          if (nombre && datos.nombreRegistrado) {
            nombre.textContent = datos.nombreRegistrado;
            mostrar(filaNombre);
          } else {
            ocultar(filaNombre);
          }
          mostrar(dialogo);
          const aceptar = dialogo.querySelector("a.primary");
          if (aceptar) {
            aceptar.focus();
          }
        }
      })
      .catch(function () {
        mostrar(accionesError);
        avisoError(raiz.getAttribute("data-texto-error"));
      });
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

  clases.forEach(function (entrada) {
    entrada.addEventListener("change", sincronizarPerfiles);
  });
  sincronizarPerfiles();

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
