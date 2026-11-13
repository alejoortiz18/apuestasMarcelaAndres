namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaCubrirVenta
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'cubrir')
            EXEC(N'CREATE SCHEMA cubrir');

        IF OBJECT_ID(N'cubrir.Operaciones', N'U') IS NULL
        BEGIN
            CREATE TABLE cubrir.Operaciones (
                OperacionId           UNIQUEIDENTIFIER NOT NULL,
                Estado                NVARCHAR(20)     NOT NULL,
                LoteriaId             UNIQUEIDENTIFIER NOT NULL,
                Numero                NVARCHAR(MAX)    NOT NULL,
                UsuarioId             UNIQUEIDENTIFIER NOT NULL,
                FechaOperacion        DATETIME2        NOT NULL,
                Subtotal              DECIMAL(18,2)    NOT NULL,
                ComisionPorcentaje    DECIMAL(18,2)    NULL,
                ComisionValor         DECIMAL(18,2)    NULL,
                TotalConComision      DECIMAL(18,2)    NULL,
                DescuentoPorcentaje   DECIMAL(18,2)    NULL,
                DescuentoValor        DECIMAL(18,2)    NULL,
                TotalPagar            DECIMAL(18,2)    NULL,
                CONSTRAINT PK_cubrir_Operaciones PRIMARY KEY (OperacionId),
                CONSTRAINT FK_cubrir_Operaciones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
                CONSTRAINT FK_cubrir_Operaciones_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId)
            );
        END

        IF OBJECT_ID(N'cubrir.Lineas', N'U') IS NULL
        BEGIN
            CREATE TABLE cubrir.Lineas (
                LineaId         UNIQUEIDENTIFIER NOT NULL,
                OperacionId     UNIQUEIDENTIFIER NOT NULL,
                JuegoId         UNIQUEIDENTIFIER NOT NULL,
                LoteriaId       UNIQUEIDENTIFIER NOT NULL,
                Valor           DECIMAL(18,2)    NOT NULL,
                FechaRegistro   DATETIME2        NOT NULL,
                CONSTRAINT PK_cubrir_Lineas PRIMARY KEY (LineaId),
                CONSTRAINT UQ_cubrir_Lineas_JuegoLoteria UNIQUE (JuegoId, LoteriaId),
                CONSTRAINT FK_cubrir_Lineas_Operaciones FOREIGN KEY (OperacionId) REFERENCES cubrir.Operaciones(OperacionId),
                CONSTRAINT FK_cubrir_Lineas_Juegos FOREIGN KEY (JuegoId) REFERENCES dbo.Juegos(JuegoId),
                CONSTRAINT FK_cubrir_Lineas_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId)
            );
        END

        IF OBJECT_ID(N'cubrir.Parametros', N'U') IS NULL
        BEGIN
            CREATE TABLE cubrir.Parametros (
                ParametroId           UNIQUEIDENTIFIER NOT NULL,
                ComisionPorcentaje    DECIMAL(5,2)     NULL,
                DescuentoPorcentaje   DECIMAL(5,2)     NULL,
                CONSTRAINT PK_cubrir_Parametros PRIMARY KEY (ParametroId)
            );
        END
        """;
}
