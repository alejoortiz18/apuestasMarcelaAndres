IF OBJECT_ID(N'dbo.LoteriasDiasSemana', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoteriasDiasSemana (
        LoteriaId           UNIQUEIDENTIFIER NOT NULL,
        DiaSemana           TINYINT          NOT NULL,
        FechaActualizacion  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_LoteriasDiasSemana PRIMARY KEY (LoteriaId, DiaSemana),
        CONSTRAINT FK_LoteriasDiasSemana_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId),
        CONSTRAINT CK_LoteriasDiasSemana_Dia CHECK (DiaSemana BETWEEN 1 AND 7)
    );
    PRINT 'Tabla LoteriasDiasSemana creada.';
END
GO

INSERT INTO dbo.LoteriasDiasSemana (LoteriaId, DiaSemana)
SELECT l.LoteriaId, d.DiaSemana
FROM dbo.Loterias l
CROSS JOIN (VALUES (1), (2), (3), (4), (5), (6), (7)) d(DiaSemana)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.LoteriasDiasSemana x
    WHERE x.LoteriaId = l.LoteriaId
);
GO
PRINT 'Loterias existentes habilitadas los 7 dias.';
