(function () {
  const milisegundos = Number(document.body.getAttribute("data-inactividad-ms"));
  const formulario = document.querySelector("form[data-cerrar-sesion]");
  if (!milisegundos || !formulario) {
    return;
  }

  let temporizador = 0;

  function reiniciar() {
    window.clearTimeout(temporizador);
    temporizador = window.setTimeout(function () {
      const motivo = formulario.querySelector("input[name='motivo']");
      if (motivo) {
        motivo.value = "inactividad";
      }
      formulario.submit();
    }, milisegundos);
  }

  ["pointerdown", "keydown", "touchstart", "wheel"].forEach(function (evento) {
    document.addEventListener(evento, reiniciar, { passive: true });
  });
  reiniciar();
})();
