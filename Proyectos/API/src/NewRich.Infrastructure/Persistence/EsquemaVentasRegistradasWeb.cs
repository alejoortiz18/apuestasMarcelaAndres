namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaVentasRegistradasWeb
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'manual')
            EXEC(N'CREATE SCHEMA manual');

        IF OBJECT_ID(N'manual.VentasRegistradasWeb', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.VentasRegistradasWeb (
                VentaRegistradaWebId UNIQUEIDENTIFIER NOT NULL,
                VendedorId           UNIQUEIDENTIFIER NOT NULL,
                FechaVenta           DATE             NOT NULL,
                Valor                DECIMAL(18,2)    NOT NULL,
                FechaRegistro        DATETIME2        NOT NULL,
                CONSTRAINT PK_manual_VentasRegistradasWeb PRIMARY KEY (VentaRegistradaWebId),
                CONSTRAINT FK_manual_VentasRegistradasWeb_Usuarios FOREIGN KEY (VendedorId) REFERENCES dbo.Usuarios(UsuarioId),
                CONSTRAINT CK_manual_VentasRegistradasWeb_Valor CHECK (Valor > 0)
            );

            CREATE INDEX IX_manual_VentasRegistradasWeb_Fecha
                ON manual.VentasRegistradasWeb (FechaVenta) INCLUDE (VendedorId, Valor);
        END

        IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_manual_VentasRegistradasWeb_VendedorFecha')
            ALTER TABLE manual.VentasRegistradasWeb DROP CONSTRAINT UQ_manual_VentasRegistradasWeb_VendedorFecha;

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_manual_VentasRegistradasWeb_VendedorFecha')
            CREATE INDEX IX_manual_VentasRegistradasWeb_VendedorFecha
                ON manual.VentasRegistradasWeb (VendedorId, FechaVenta DESC, FechaRegistro DESC);
        """;
}
