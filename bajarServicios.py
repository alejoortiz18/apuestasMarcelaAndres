#!/usr/bin/env python3
"""
Apaga la API NewRich y el Administrador levantados por ejecutarServicios.py.
También quita el puente USB (adb reverse) si hay un celular conectado.

No detiene SQL Server: suele quedar como servicio de Windows para otros usos.

Uso (desde la raíz del repositorio):
    python bajarServicios.py
"""

from __future__ import annotations

import subprocess
import sys
import time
from pathlib import Path

RAIZ = Path(__file__).resolve().parent
ADB = "adb"
DISPOSITIVO_USB = "TCDUAAVWV4KVGI8L"

# Fragmentos de la línea de comando para identificar los procesos a detener.
FRAGMENTOS = (
    "NewRich.Api",
    "NewRich.Admin",
    "Proyectos/API/src/NewRich.Api",
    "Proyectos/Administrador",
    "Proyectos\\API\\src\\NewRich.Api",
    "Proyectos\\Administrador",
)


def escribir(mensaje: str) -> None:
    print(mensaje, flush=True)


def ejecutar(comando: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        comando,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )


def pids_servicios() -> list[int]:
    filtro = " -or ".join(f"$_.CommandLine -like '*{f}*'" for f in FRAGMENTOS)
    script = (
        "Get-CimInstance Win32_Process -Filter "
        "\"Name='dotnet.exe' OR Name='NewRich.Api.exe' OR Name='NewRich.Admin.exe'\" | "
        f"Where-Object {{ {filtro} }} | "
        "Select-Object -ExpandProperty ProcessId"
    )
    resultado = ejecutar(["powershell", "-NoProfile", "-Command", script])
    pids: list[int] = []
    for linea in (resultado.stdout or "").splitlines():
        linea = linea.strip()
        if linea.isdigit():
            pids.append(int(linea))
    return sorted(set(pids))


def puerto_en_escucha(puerto: int) -> bool:
    resultado = ejecutar(
        [
            "powershell",
            "-NoProfile",
            "-Command",
            f"(Get-NetTCPConnection -State Listen -LocalPort {puerto} -ErrorAction SilentlyContinue | Measure-Object).Count",
        ]
    )
    try:
        return int((resultado.stdout or "0").strip() or "0") > 0
    except ValueError:
        return False


def detener_procesos() -> None:
    escribir("=== API y Administrador ===")
    pids = pids_servicios()
    if not pids:
        escribir("No hay procesos de API ni Administrador en ejecución.")
        return

    escribir(f"Deteniendo procesos: {', '.join(str(p) for p in pids)}")
    for pid in pids:
        resultado = ejecutar(["taskkill", "/PID", str(pid), "/T", "/F"])
        if resultado.returncode == 0:
            escribir(f"  PID {pid} detenido.")
        else:
            detalle = (resultado.stderr or resultado.stdout or "").strip()
            escribir(f"  PID {pid}: {detalle or 'no se pudo detener'}")

    # Dar tiempo a que liberen los puertos y los DLL.
    for _ in range(10):
        if not pids_servicios() and not puerto_en_escucha(5295) and not puerto_en_escucha(5274):
            break
        time.sleep(0.5)


def quitar_puente_usb() -> None:
    escribir("=== Puente USB (adb reverse) ===")
    dispositivos = ejecutar([ADB, "devices"])
    if dispositivos.returncode != 0:
        escribir("adb no está disponible; se omite el puente USB.")
        return

    lineas = [
        linea.strip()
        for linea in (dispositivos.stdout or "").splitlines()
        if "\tdevice" in linea
    ]
    if not lineas:
        escribir("No hay celular conectado; se omite el puente USB.")
        return

    serie = DISPOSITIVO_USB
    if not any(linea.startswith(DISPOSITIVO_USB + "\t") for linea in lineas):
        serie = lineas[0].split("\t", 1)[0]
        escribir(f"Dispositivo preferido no encontrado; usando {serie}.")

    resultado = ejecutar([ADB, "-s", serie, "reverse", "--remove", "tcp:5295"])
    if resultado.returncode == 0:
        escribir(f"Puente USB removido en {serie} (tcp:5295).")
    else:
        # Si no había reverse, adb suele devolver error; no es crítico.
        detalle = (resultado.stderr or resultado.stdout or "").strip()
        escribir(f"No había puente activo o no se pudo remover: {detalle or 'sin detalle'}")


def resumen() -> None:
    api = "activo" if puerto_en_escucha(5295) else "apagado"
    admin = "activo" if puerto_en_escucha(5274) else "apagado"
    escribir("")
    escribir("Estado final:")
    escribir(f"  API (5295)            {api}")
    escribir(f"  Administrador (5274)  {admin}")
    escribir("  SQL Server             no se detuvo (servicio de Windows)")


def main() -> None:
    escribir(f"Repositorio: {RAIZ}")
    detener_procesos()
    quitar_puente_usb()
    resumen()


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        escribir("\nCancelado por el usuario.")
        sys.exit(130)
