#!/usr/bin/env python3
"""
Levanta SQL Server (si está detenido), la API NewRich y el Administrador.
Opcionalmente configura el puente USB (adb reverse) si hay un celular conectado.

Uso (desde la raíz del repositorio):
    python ejecutarServicios.py
"""

from __future__ import annotations

import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

RAIZ = Path(__file__).resolve().parent
DOTNET = "dotnet"
ADB = "adb"

API_PROYECTO = RAIZ / "Proyectos" / "API" / "src" / "NewRich.Api" / "NewRich.Api.csproj"
ADMIN_PROYECTO = RAIZ / "Proyectos" / "Administrador" / "NewRich.Admin.csproj"

API_URL = "http://0.0.0.0:5295"
ADMIN_URL = "http://0.0.0.0:5274"
API_CHEQUEO = "http://localhost:5295/api/Premios"
ADMIN_CHEQUEO = "http://localhost:5274/"

LOG_DIR = RAIZ / "tmp" / "servicios"
DISPOSITIVO_USB = "TCDUAAVWV4KVGI8L"
ESPERA_ARRANQUE_SEG = 90
INTERVALO_CHEQUEO_SEG = 3


def escribir(mensaje: str) -> None:
    print(mensaje, flush=True)


def ejecutar(comando: list[str], *, captura: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        comando,
        capture_output=captura,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )


def servicio_sql_activo() -> bool:
    resultado = ejecutar(["sc", "query", "MSSQLSERVER"])
    return "RUNNING" in (resultado.stdout or "").upper()


def levantar_sql_server() -> None:
    escribir("=== SQL Server ===")
    if servicio_sql_activo():
        escribir("MSSQLSERVER ya está en ejecución.")
        return

    escribir("Iniciando MSSQLSERVER...")
    resultado = ejecutar(["net", "start", "MSSQLSERVER"])
    if resultado.returncode != 0 and not servicio_sql_activo():
        escribir("No se pudo iniciar SQL Server. Ejecute este script como administrador.")
        if resultado.stderr:
            escribir(resultado.stderr.strip())
        sys.exit(1)

    for _ in range(20):
        if servicio_sql_activo():
            escribir("MSSQLSERVER en ejecución.")
            return
        time.sleep(1)

    escribir("SQL Server no respondió a tiempo.")
    sys.exit(1)


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


def http_responde(url: str) -> tuple[bool, str]:
    try:
        with urllib.request.urlopen(url, timeout=8) as respuesta:
            return True, f"HTTP {respuesta.status}"
    except urllib.error.HTTPError as error:
        # 401 en la API indica que está viva y exige autenticación.
        return True, f"HTTP {error.code}"
    except Exception as error:  # noqa: BLE001
        return False, str(error)


def proceso_dotnet_activo(fragmento: str) -> bool:
    resultado = ejecutar(
        [
            "powershell",
            "-NoProfile",
            "-Command",
            (
                "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe' OR Name='NewRich.Api.exe' "
                "OR Name='NewRich.Admin.exe'\" | "
                f"Where-Object {{ $_.CommandLine -like '*{fragmento}*' }} | "
                "Select-Object -ExpandProperty ProcessId"
            ),
        ]
    )
    return bool((resultado.stdout or "").strip())


def lanzar_servicio(nombre: str, proyecto: Path, url: str, log_nombre: str) -> None:
    escribir(f"=== {nombre} ===")
    if not proyecto.exists():
        escribir(f"No se encontró el proyecto: {proyecto}")
        sys.exit(1)

    if proceso_dotnet_activo(proyecto.name) or proceso_dotnet_activo(str(proyecto).replace("\\", "/")):
        escribir(f"{nombre} ya parece estar en ejecución.")
        return

    LOG_DIR.mkdir(parents=True, exist_ok=True)
    stdout = open(LOG_DIR / f"{log_nombre}-out.log", "a", encoding="utf-8")
    stderr = open(LOG_DIR / f"{log_nombre}-err.log", "a", encoding="utf-8")
    stdout.write(f"\n--- arranque {time.strftime('%Y-%m-%d %H:%M:%S')} ---\n")
    stdout.flush()

    comando = [
        DOTNET,
        "run",
        "--project",
        str(proyecto),
        "--urls",
        url,
    ]
    subprocess.Popen(
        comando,
        cwd=str(RAIZ),
        stdout=stdout,
        stderr=stderr,
        creationflags=subprocess.CREATE_NEW_PROCESS_GROUP | subprocess.DETACHED_PROCESS,
    )
    escribir(f"{nombre} iniciado en segundo plano ({url}).")
    escribir(f"Logs: {LOG_DIR / f'{log_nombre}-out.log'}")


def esperar_servicio(nombre: str, chequeo: str, puerto: int) -> None:
    escribir(f"Esperando {nombre}...")
    limite = time.time() + ESPERA_ARRANQUE_SEG
    ultimo = ""
    while time.time() < limite:
        ok, detalle = http_responde(chequeo)
        ultimo = detalle
        if ok:
            escribir(f"{nombre} listo ({detalle}, puerto {puerto}).")
            return
        time.sleep(INTERVALO_CHEQUEO_SEG)

    escribir(f"{nombre} no respondió a tiempo: {ultimo}")
    escribir(f"Revise el log en {LOG_DIR}")
    sys.exit(1)


def configurar_puente_usb() -> None:
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

    resultado = ejecutar([ADB, "-s", serie, "reverse", "tcp:5295", "tcp:5295"])
    if resultado.returncode == 0:
        escribir(f"Puente USB listo: {serie} tcp:5295 -> PC:5295")
    else:
        escribir("No se pudo configurar adb reverse.")
        if resultado.stderr:
            escribir(resultado.stderr.strip())


def main() -> None:
    escribir(f"Repositorio: {RAIZ}")
    levantar_sql_server()

    # Primero la API; el Administrador espera a que esté lista para no pelear
    # por los DLL compartidos durante la compilación.
    lanzar_servicio("API", API_PROYECTO, API_URL, "api")
    esperar_servicio("API", API_CHEQUEO, 5295)

    lanzar_servicio("Administrador", ADMIN_PROYECTO, ADMIN_URL, "admin")
    esperar_servicio("Administrador", ADMIN_CHEQUEO, 5274)

    configurar_puente_usb()

    escribir("")
    escribir("Servicios operativos:")
    escribir(f"  API            http://localhost:5295  ({API_CHEQUEO} -> 401 es normal)")
    escribir(f"  Administrador  http://localhost:5274")
    escribir(f"  Logs           {LOG_DIR}")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        escribir("\nCancelado por el usuario.")
        sys.exit(130)
