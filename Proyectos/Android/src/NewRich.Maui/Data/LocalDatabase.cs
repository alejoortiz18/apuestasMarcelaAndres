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
    private readonly SQLiteAsyncConnection _db;

    public LocalDatabase()
    {
        SQLitePCL.Batteries_V2.Init();
        var ruta = Path.Combine(FileSystem.AppDataDirectory, "newrich-offline.db3");
        _db = new SQLiteAsyncConnection(ruta);
        _db.CreateTableAsync<CodigoOfflineLocal>().GetAwaiter().GetResult();
    }

    public Task<int> ContarDisponiblesAsync() =>
        _db.Table<CodigoOfflineLocal>().Where(c => !c.Usado).CountAsync();

    public async Task GuardarDescargaAsync(IEnumerable<(string Consecutivo, string Payload)> codigos)
    {
        foreach (var codigo in codigos)
        {
            var existente = await _db.FindAsync<CodigoOfflineLocal>(codigo.Consecutivo);
            if (existente is null)
            {
                await _db.InsertAsync(new CodigoOfflineLocal
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
        var codigo = await _db.Table<CodigoOfflineLocal>().Where(c => !c.Usado).FirstOrDefaultAsync();
        if (codigo is null)
        {
            return null;
        }

        codigo.Usado = true;
        await _db.UpdateAsync(codigo);
        return codigo;
    }
}
