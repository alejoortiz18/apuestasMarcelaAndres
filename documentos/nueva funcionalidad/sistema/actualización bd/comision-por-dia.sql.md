# Actualización de base de datos: comisión por usuario y por día

El script crea la tabla de porcentajes, agrega a las obligaciones de recaudo las columnas que guardan el porcentaje con que se creó cada movimiento y asigna 1 % a los vendedores de PDA y recaudadores que ya existen. El API ejecuta el mismo script al iniciar (`AsegurarEsquemaComisionDiaAsync`), así que no hay que correrlo a mano: basta con publicar el API.

Día de la semana: 1 es lunes y 7 es domingo.

## Qué cambia

| Objeto | Cambio |
|---|---|
| `dbo.UsuarioComisionDia` | Tabla nueva. Un porcentaje entero de 0 a 100 por usuario y día. |
| `recaudo.Obligaciones` | Columnas nuevas `PorcentajeRecaudador`, `ComisionRecaudador` y `EntregarAdministracion`. La columna `Porcentaje`, que ya existía, guarda ahora el porcentaje del vendedor. |

## Lo que no se borra

- `manual.ParametrosRevisadaManual` (comisión y descuento generales) queda en la base. Lo cubierto antes sigue mostrando los valores con que se guardó; lo que se cubre desde ahora va sin comisión ni descuento.
- `recaudo.PorcentajesGrupo` y la columna `Porcentaje` de las asignaciones de grupo y vendedor quedan en la base, pero ya no se usan para calcular.
- Las obligaciones creadas antes de esta versión tienen `PorcentajeRecaudador` vacío. Esas siguen con el cálculo del grupo que tenían. Las nuevas guardan los porcentajes del día y no se recalculan aunque cambie la configuración del usuario.

## Script

```sql
IF OBJECT_ID(N'dbo.UsuarioComisionDia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioComisionDia (
        UsuarioId   UNIQUEIDENTIFIER NOT NULL,
        DiaSemana   TINYINT          NOT NULL,
        Porcentaje  TINYINT          NOT NULL,
        CONSTRAINT PK_UsuarioComisionDia PRIMARY KEY (UsuarioId, DiaSemana),
        CONSTRAINT FK_UsuarioComisionDia_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId),
        CONSTRAINT CK_UsuarioComisionDia_Dia CHECK (DiaSemana BETWEEN 1 AND 7),
        CONSTRAINT CK_UsuarioComisionDia_Porcentaje CHECK (Porcentaje BETWEEN 0 AND 100)
    );
END;

IF OBJECT_ID(N'recaudo.Obligaciones', N'U') IS NOT NULL AND COL_LENGTH(N'recaudo.Obligaciones', N'PorcentajeRecaudador') IS NULL
    ALTER TABLE recaudo.Obligaciones ADD
        PorcentajeRecaudador   INT            NULL,
        ComisionRecaudador     DECIMAL(18,2)  NULL,
        EntregarAdministracion DECIMAL(18,2)  NULL;

INSERT INTO dbo.UsuarioComisionDia (UsuarioId, DiaSemana, Porcentaje)
SELECT u.UsuarioId, d.Dia, 1
FROM dbo.Usuarios u
CROSS JOIN (VALUES (1),(2),(3),(4),(5),(6),(7)) d(Dia)
WHERE u.Rol IN (N'Vendedor', N'Recaudador')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UsuarioComisionDia c WHERE c.UsuarioId = u.UsuarioId AND c.DiaSemana = d.Dia
  );
```
