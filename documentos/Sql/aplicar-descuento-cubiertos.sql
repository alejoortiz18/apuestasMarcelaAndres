/*
  Revisada manual ya no tiene el botón Registrar descuento: el descuento se registra al cubrir.
  Este script registra el descuento guardado en manual.ParametrosRevisadaManual a los grupos
  que ya estaban en Cubierto sin descuento.

  - Misma fórmula del sistema: valor del descuento = total con comisión × % ÷ 100, redondeado
    a 2 decimales; total a pagar = total con comisión − valor del descuento.
  - Si no hay descuento guardado, no cambia nada.
  - Se puede ejecutar varias veces: solo toca los grupos sin descuento.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @descuento DECIMAL(5,2) = (SELECT TOP 1 DescuentoPorcentaje FROM manual.ParametrosRevisadaManual);

IF @descuento IS NULL
BEGIN
    SELECT GruposActualizados = 0, Motivo = N'No hay descuento guardado en manual.ParametrosRevisadaManual.';
    RETURN;
END

BEGIN TRANSACTION;

UPDATE manual.RevisadaManualOperaciones
SET DescuentoPorcentaje = @descuento,
    DescuentoValor = ROUND(TotalConComision * @descuento / 100, 2),
    TotalPagar = TotalConComision - ROUND(TotalConComision * @descuento / 100, 2)
WHERE Estado = 'Cubierto'
  AND DescuentoPorcentaje IS NULL
  AND TotalConComision IS NOT NULL;

DECLARE @actualizados int = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT GruposActualizados = @actualizados, DescuentoAplicado = @descuento;
