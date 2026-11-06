namespace NewRich.Pda.Core;

public sealed class RecargaRecaudo
{
    private int _serie;
    private bool _ocupado;
    private bool _pendiente;

    public int Nueva() => ++_serie;

    public bool Vigente(int serie) => serie == _serie;

    public bool Intentar()
    {
        if (_ocupado)
        {
            _pendiente = true;
            return false;
        }

        _ocupado = true;
        return true;
    }

    public bool Terminar()
    {
        _ocupado = false;
        if (!_pendiente)
        {
            return false;
        }

        _pendiente = false;
        return true;
    }
}
