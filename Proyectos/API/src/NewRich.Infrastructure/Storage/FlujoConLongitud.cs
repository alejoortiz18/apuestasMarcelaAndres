namespace NewRich.Infrastructure.Storage;

/// <summary>
/// Flujo de solo lectura que informa la longitud del objeto. ASP.NET Core solo envía
/// Content-Length cuando el flujo dice que admite búsqueda, y la PDA lo usa para validar la descarga.
/// Solo acepta "buscar" la posición en la que ya está.
/// </summary>
public sealed class FlujoConLongitud : Stream
{
    private readonly Stream _origen;
    private readonly long _longitud;
    private readonly IDisposable? _propietario;
    private long _posicion;

    public FlujoConLongitud(Stream origen, long longitud, IDisposable? propietario)
    {
        _origen = origen;
        _longitud = longitud;
        _propietario = propietario;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _longitud;

    public override long Position
    {
        get => _posicion;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var leidos = _origen.Read(buffer, offset, count);
        _posicion += leidos;
        return leidos;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var leidos = await _origen.ReadAsync(buffer, cancellationToken);
        _posicion += leidos;
        return leidos;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override long Seek(long offset, SeekOrigin origin)
    {
        var destino = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _posicion + offset,
            _ => _longitud + offset
        };

        if (destino != _posicion)
        {
            throw new NotSupportedException();
        }

        return _posicion;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _origen.Dispose();
            _propietario?.Dispose();
        }

        base.Dispose(disposing);
    }
}
