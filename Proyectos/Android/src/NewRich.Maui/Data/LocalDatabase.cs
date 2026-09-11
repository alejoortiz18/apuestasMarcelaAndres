using SQLite;

namespace NewRich.Maui.Data;

public sealed class CodigoOfflineLocal
{
    [PrimaryKey]
    public string Consecutivo { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public bool Usado { get; set; }
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
        return codigo;
    }

    private async Task<SQLiteAsyncConnection> ConexionAsync()
    {
        await AsegurarAsync();
        return _db!;
    }
}
