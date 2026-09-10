(function () {
  document.querySelectorAll("[data-chat-adjunto]").forEach(function (caja) {
    const input = caja.querySelector("[data-chat-archivo]");
    const chip = caja.querySelector("[data-chat-archivo-nombre]");
    const texto = caja.querySelector("[data-chat-archivo-texto]");
    const quitar = caja.querySelector("[data-chat-archivo-quitar]");
    const boton = caja.querySelector(".chat-attach-btn");
    if (!input || !chip || !texto || !quitar || !boton) {
      return;
    }

    function pintar() {
      const archivo = input.files && input.files[0];
      if (!archivo) {
        chip.classList.add("hidden");
        boton.classList.remove("is-on");
        texto.textContent = "";
        return;
      }

      texto.textContent = archivo.name;
      chip.classList.remove("hidden");
      boton.classList.add("is-on");
    }

    input.addEventListener("change", pintar);
    quitar.addEventListener("click", function () {
      input.value = "";
      pintar();
      boton.focus();
    });
  });
})();

(function () {
  const raiz = document.querySelector("[data-chat-vivo]");
  if (!raiz || typeof signalR === "undefined") {
    return;
  }

  const conexionUrl = document.documentElement.getAttribute("data-notif-conexion");
  if (!conexionUrl) {
    return;
  }

  const yo = (raiz.getAttribute("data-yo") || "").toLowerCase();
  const evento = raiz.getAttribute("data-evento") || "mensajeChat";

  function hora(fechaIso) {
    const fecha = new Date(fechaIso);
    if (Number.isNaN(fecha.getTime())) {
      return "";
    }
    const d = String(fecha.getDate()).padStart(2, "0");
    const m = String(fecha.getMonth() + 1).padStart(2, "0");
    const h = String(fecha.getHours()).padStart(2, "0");
    const min = String(fecha.getMinutes()).padStart(2, "0");
    return d + "/" + m + " " + h + ":" + min;
  }

  function conversacionActual() {
    return (raiz.getAttribute("data-conversacion") || "").toLowerCase();
  }

  function asegurarLista() {
    const caja = document.querySelector("[data-chat-scroll]");
    if (!caja) {
      return null;
    }
    const vacio = caja.querySelector(".empty");
    if (vacio) {
      vacio.remove();
    }
    return caja;
  }

  function agregarBurbuja(mensaje) {
    const caja = asegurarLista();
    if (!caja) {
      return;
    }
    const id = mensaje.mensajeId || mensaje.MensajeId;
    if (id && caja.querySelector('[data-mensaje-id="' + id + '"]')) {
      return;
    }
    const emisor = (mensaje.usuarioEmisorId || mensaje.UsuarioEmisorId || "").toLowerCase();
    const mio = emisor && emisor === yo;
    const burbuja = document.createElement("div");
    burbuja.className = "chat-message " + (mio ? "me" : "other");
    if (id) {
      burbuja.setAttribute("data-mensaje-id", id);
    }
    burbuja.appendChild(document.createTextNode(mensaje.texto || mensaje.Texto || ""));
    const adjuntoId = mensaje.adjuntoId || mensaje.AdjuntoId;
    const nombreArchivo = mensaje.nombreArchivo || mensaje.NombreArchivo || "";
    if (adjuntoId) {
      const plantilla = raiz.getAttribute("data-adjunto-plantilla") || "/Soporte/Adjunto/00000000-0000-0000-0000-000000000000";
      const href = plantilla.replace(/00000000-0000-0000-0000-000000000000/i, adjuntoId);
      const enlace = document.createElement("a");
      enlace.className = "chat-file";
      enlace.href = href;
      const ext = (nombreArchivo.split(".").pop() || "").toLowerCase();
      if (["jpg", "jpeg", "png", "webp"].indexOf(ext) >= 0) {
        const img = document.createElement("img");
        img.src = href;
        img.alt = nombreArchivo;
        enlace.appendChild(img);
        const nombre = document.createElement("span");
        nombre.textContent = nombreArchivo;
        enlace.appendChild(nombre);
        enlace.target = "_blank";
        enlace.rel = "noopener";
      } else {
        enlace.className = "chat-file chat-file-pdf";
        enlace.textContent = nombreArchivo || "Archivo adjunto";
      }
      burbuja.appendChild(enlace);
    }
    const tiempo = document.createElement("time");
    const nombre = mensaje.nombreEmisor || mensaje.NombreEmisor || "";
    tiempo.textContent = hora(mensaje.fechaEnvio || mensaje.FechaEnvio) + (nombre ? " · " + nombre : "");
    burbuja.appendChild(tiempo);
    caja.appendChild(burbuja);
    caja.scrollTop = caja.scrollHeight;
  }

  function actualizarLista(conversacionId, texto) {
    const lista = document.querySelector("[data-chat-lista]");
    const item = document.querySelector('[data-chat-item="' + conversacionId + '"]');
    if (!lista || !item) {
      window.location.reload();
      return;
    }
    const vacio = lista.querySelector(".empty");
    if (vacio) {
      vacio.remove();
    }
    const resumen = item.querySelector("small");
    if (resumen) {
      resumen.textContent = texto;
    }
    if (lista.firstElementChild !== item) {
      lista.insertBefore(item, lista.firstElementChild);
    }
    lista.scrollTop = 0;
  }

  fetch(conexionUrl, { credentials: "same-origin" })
    .then(function (respuesta) {
      if (!respuesta.ok) {
        throw new Error("conexion");
      }
      return respuesta.json();
    })
    .then(function (datos) {
      if (!datos || !datos.chatHubUrl || !datos.token) {
        throw new Error("conexion");
      }
      const conexion = new signalR.HubConnectionBuilder()
        .withUrl(datos.chatHubUrl, { accessTokenFactory: function () { return datos.token; } })
        .withAutomaticReconnect()
        .build();
      conexion.on(evento, function (aviso) {
        const conversacionId = aviso.conversacionId || aviso.ConversacionId;
        const mensaje = aviso.mensaje || aviso.Mensaje;
        if (!conversacionId || !mensaje) {
          return;
        }
        const texto = mensaje.texto || mensaje.Texto || mensaje.nombreArchivo || mensaje.NombreArchivo || "Adjunto";
        actualizarLista(conversacionId, texto);
        if (conversacionId.toLowerCase() === conversacionActual()) {
          agregarBurbuja(mensaje);
        }
      });
      function arrancar() {
        return conexion.start().catch(function () {
          window.setTimeout(arrancar, 4000);
        });
      }
      return arrancar();
    })
    .catch(function () { });
})();
