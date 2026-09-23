using System.Text.Json;
using NewRich.Application.Contracts.Android;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.Loterias;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Auth;
using NewRich.Pda.Core.Ventas;
using SQLite;

namespace NewRich.Maui.Data;

public sealed class CodigoOfflineLocal
{
    [PrimaryKey]
    public string Consecutivo { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public bool Usado { get; set; }
}

public sealed class VentaOfflineLocal
{
    [PrimaryKey]
    public string Consecutivo { get; set; } = string.Empty;

    public string QrJson { get; set; } = string.Empty;

    public bool Sincronizada { get; set; }

    public string FechaLocal { get; set; } = string.Empty;
}

public sealed class ReporteTecnicoLocal
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string Observacion { get; set; } = string.Empty;

    public string CodigoTicket { get; set; } = string.Empty;

    public string NombreArchivo { get; set; } = string.Empty;

    public string RutaPdf { get; set; } = string.Empty;

    public bool Enviado { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string FechaLocal { get; set; } = string.Empty;
}

public sealed class DatoLocal
{
    [PrimaryKey]
    public string Clave { get; set; } = string.Empty;

    public string Json { get; set; } = string.Empty;
}

public sealed class NumeroRestringidoLocal
{
    [PrimaryKey]
    public string Numero { get; set; } = string.Empty;
}

public sealed class SesionLocal
{
    public LoginAndroidResponse Usuario { get; set; } = new();
    public ConfiguracionOperativaResponse Limites { get; set; } = new();
    public string CodigoDispositivo { get; set; } = string.Empty;
}

public sealed class LocalDatabase
{
    private readonly string _ruta;
    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _candado = new(1, 1);

    public LocalDatabase()
    {
        SQLitePCL.Batteries_V2.Init();
        _ruta = Path.Combine(FileSystem.AppDataDirectory, "newrich-offline.db3");
    }

    public async Task AsegurarAsync()
    {
        await _candado.WaitAsync();
        try
        {
            if (_db is not null)
            {
                return;
            }

            _db = new SQLiteAsyncConnection(_ruta);
            await _db.CreateTableAsync<CodigoOfflineLocal>();
            await _db.CreateTableAsync<VentaOfflineLocal>();
            await _db.CreateTableAsync<ReporteTecnicoLocal>();
            await _db.CreateTableAsync<DatoLocal>();
            await _db.CreateTableAsync<NumeroRestringidoLocal>();
        }
        finally
        {
            _candado.Release();
        }
    }

    public async Task<int> ContarDisponiblesAsync()
    {
        var db = await ConexionAsync();
        return await db.Table<CodigoOfflineLocal>().Where(c => !c.Usado).CountAsync();
    }

    public async Task<IReadOnlyList<CodigoOfflineLocal>> ListarAsync()
    {
        var db = await ConexionAsync();
        return await db.Table<CodigoOfflineLocal>()
            .OrderBy(c => c.Consecutivo)
            .ToListAsync();
    }

    public async Task GuardarDescargaAsync(IEnumerable<(string Consecutivo, string Payload)> codigos)
    {
        var db = await ConexionAsync();
        foreach (var codigo in codigos)
        {
            var existente = await db.FindAsync<CodigoOfflineLocal>(codigo.Consecutivo);
            if (existente is null)
            {
                await db.InsertAsync(new CodigoOfflineLocal
                {
                    Consecutivo = codigo.Consecutivo,
                    Payload = codigo.Payload,
                    Usado = false
                });
            }
        }
    }

    public async Task<CodigoOfflineLocal?> ConsumirAsync()
    {
        var db = await ConexionAsync();
        var codigo = await db.Table<CodigoOfflineLocal>().Where(c => !c.Usado).FirstOrDefaultAsync();
        if (codigo is null)
        {
            return null;
        }

        codigo.Usado = true;
        await db.UpdateAsync(codigo);
        await IncrementarGastadosPendientesAsync();
        return codigo;
    }

    public const string ClaveMaximosOffline = "codigosOfflineMaximos";
    public const string ClaveGastadosPendientes = "codigosOfflineGastadosPendientes";

    public async Task<int> ObtenerMaximosOfflineAsync()
    {
        var valor = await LeerJsonAsync<int?>(ClaveMaximosOffline);
        return valor is > 0 ? valor.Value : 0;
    }

    public async Task GuardarMaximosOfflineAsync(int maximos) =>
        await GuardarJsonAsync(ClaveMaximosOffline, Math.Max(0, maximos));

    public async Task<int> ObtenerGastadosPendientesAsync()
    {
        var valor = await LeerJsonAsync<int?>(ClaveGastadosPendientes);
        return valor is > 0 ? valor.Value : 0;
    }

    public async Task GuardarGastadosPendientesAsync(int gastados) =>
        await GuardarJsonAsync(ClaveGastadosPendientes, Math.Max(0, gastados));

    public async Task IncrementarGastadosPendientesAsync()
    {
        var actual = await ObtenerGastadosPendientesAsync();
        await GuardarGastadosPendientesAsync(actual + 1);
    }

    public async Task<int> ContarVentasPendientesCantidadAsync()
    {
        var db = await ConexionAsync();
        return await db.Table<VentaOfflineLocal>().Where(v => !v.Sincronizada).CountAsync();
    }

    public async Task GuardarVentaAsync(string consecutivo, string qrJson)
    {
        var db = await ConexionAsync();
        var existente = await db.FindAsync<VentaOfflineLocal>(consecutivo);
        if (existente is null)
        {
            await db.InsertAsync(new VentaOfflineLocal
            {
                Consecutivo = consecutivo,
                QrJson = qrJson,
                Sincronizada = false,
                FechaLocal = DateTime.Now.ToString("O")
            });
            return;
        }

        if (!existente.Sincronizada)
        {
            existente.QrJson = qrJson;
            await db.UpdateAsync(existente);
        }
    }

    public async Task<IReadOnlyList<string>> VentasPendientesAsync()
    {
        var db = await ConexionAsync();
        var filas = await db.Table<VentaOfflineLocal>().Where(v => !v.Sincronizada).ToListAsync();
        return filas.Select(v => v.QrJson).ToArray();
    }

    public async Task MarcarSincronizadasAsync(IEnumerable<string> consecutivos)
    {
        var db = await ConexionAsync();
        foreach (var consecutivo in consecutivos)
        {
            var fila = await db.FindAsync<VentaOfflineLocal>(consecutivo);
            if (fila is null)
            {
                continue;
            }

            fila.Sincronizada = true;
            await db.UpdateAsync(fila);
        }
    }

    public async Task GuardarReporteTecnicoAsync(ReporteTecnicoLocal reporte)
    {
        var db = await ConexionAsync();
        var existente = await db.FindAsync<ReporteTecnicoLocal>(reporte.Id);
        if (existente is null)
        {
            await db.InsertAsync(reporte);
            return;
        }

        await db.UpdateAsync(reporte);
    }

    public async Task<IReadOnlyList<ReporteTecnicoLocal>> ReportesTecnicosPendientesAsync()
    {
        var db = await ConexionAsync();
        return await db.Table<ReporteTecnicoLocal>()
            .Where(r => !r.Enviado)
            .OrderBy(r => r.FechaLocal)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ReporteTecnicoLocal>> ReportesTecnicosAsync()
    {
        var db = await ConexionAsync();
        return await db.Table<ReporteTecnicoLocal>()
            .OrderByDescending(r => r.FechaLocal)
            .ToListAsync();
    }

    public async Task MarcarReporteTecnicoEnviadoAsync(string id)
    {
        var db = await ConexionAsync();
        var fila = await db.FindAsync<ReporteTecnicoLocal>(id);
        if (fila is null)
        {
            return;
        }

        fila.Enviado = true;
        fila.Estado = EstadoReporteTecnico.Enviado;
        await db.UpdateAsync(fila);
    }

    public async Task GuardarLoteriasAsync(IReadOnlyList<LoteriaResponse> loterias) =>
        await GuardarJsonAsync("loterias", loterias);

    public async Task<IReadOnlyList<LoteriaResponse>> LoteriasAsync()
    {
        var lista = await LeerJsonAsync<List<LoteriaResponse>>("loterias");
        return lista ?? [];
    }

    public async Task GuardarNumerosRestringidosAsync(IReadOnlyCollection<string>? numeros)
    {
        var db = await ConexionAsync();
        await db.DeleteAllAsync<NumeroRestringidoLocal>();
        foreach (var numero in NumerosRestringidosPda.Vigentes(numeros, null))
        {
            await db.InsertAsync(new NumeroRestringidoLocal { Numero = numero });
        }
    }

    public async Task<IReadOnlyList<string>> NumerosRestringidosAsync()
    {
        var db = await ConexionAsync();
        var filas = await db.Table<NumeroRestringidoLocal>().OrderBy(n => n.Numero).ToListAsync();
        return filas.Select(n => n.Numero).ToArray();
    }

    public async Task GuardarSesionAsync(SesionLocal sesion) =>
        await GuardarJsonAsync("sesion", sesion);

    public async Task<SesionLocal?> SesionAsync() =>
        await LeerJsonAsync<SesionLocal>("sesion");

    public async Task GuardarCredencialAsync(CredencialLocalRegistro credencial) =>
        await GuardarJsonAsync("credencial", credencial);

    public async Task<CredencialLocalRegistro?> CredencialAsync() =>
        await LeerJsonAsync<CredencialLocalRegistro>("credencial");

    private async Task GuardarJsonAsync<T>(string clave, T valor)
    {
        var db = await ConexionAsync();
        var json = JsonSerializer.Serialize(valor);
        var fila = await db.FindAsync<DatoLocal>(clave);
        if (fila is null)
        {
            await db.InsertAsync(new DatoLocal { Clave = clave, Json = json });
            return;
        }

        fila.Json = json;
        await db.UpdateAsync(fila);
    }

    private async Task<T?> LeerJsonAsync<T>(string clave)
    {
        var db = await ConexionAsync();
        var fila = await db.FindAsync<DatoLocal>(clave);
        if (fila is null || string.IsNullOrWhiteSpace(fila.Json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(fila.Json);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private async Task<SQLiteAsyncConnection> ConexionAsync()
    {
        await AsegurarAsync();
        return _db!;
    }
}
