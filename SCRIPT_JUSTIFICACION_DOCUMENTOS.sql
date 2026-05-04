-- ============================================================
-- SCRIPT: Justificaciones - Opción B (múltiples documentos)
-- Ejecutar una sola vez sobre DB_RRHH
-- ============================================================

-- 1. Tabla de documentos adjuntos por justificación
IF OBJECT_ID('dbo.JUSTIFICACION_DOCUMENTOS', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.JUSTIFICACION_DOCUMENTOS (
        id_documento     INT IDENTITY(1,1) NOT NULL,
        id_justificacion INT              NOT NULL,
        documento_url    NVARCHAR(500)    NOT NULL,
        nombre_archivo   NVARCHAR(255)    NOT NULL,
        created_at       DATETIME2        NOT NULL
            CONSTRAINT DF_JustDoc_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_JustDoc        PRIMARY KEY (id_documento),
        CONSTRAINT FK_JustDoc_Justif FOREIGN KEY (id_justificacion)
            REFERENCES dbo.JUSTIFICACIONES (id_justificacion) ON DELETE CASCADE
    );

    CREATE INDEX IX_JustDoc_Justificacion
        ON dbo.JUSTIFICACION_DOCUMENTOS (id_justificacion);

    PRINT 'Tabla JUSTIFICACION_DOCUMENTOS creada.';
END
ELSE
    PRINT 'Tabla JUSTIFICACION_DOCUMENTOS ya existe, se omite.';

-- 2. Estados para Justificaciones en MAESTRO_ESTADOS
--    (se insertan solo si el grupo JUSTIFICACION aún no existe)
IF NOT EXISTS (
    SELECT 1 FROM dbo.MAESTRO_ESTADOS
    WHERE grupo_estado = 'JUSTIFICACION' AND nombre_estado = 'PENDIENTE'
)
BEGIN
    DECLARE @baseId INT;
    SELECT @baseId = ISNULL(MAX(id_estado), 0) + 1 FROM dbo.MAESTRO_ESTADOS;

    INSERT INTO dbo.MAESTRO_ESTADOS (id_estado, grupo_estado, nombre_estado, descripcion, color_hex)
    VALUES
        (@baseId,     'JUSTIFICACION', 'PENDIENTE',  'Justificación pendiente de revisión', '#FFA500'),
        (@baseId + 1, 'JUSTIFICACION', 'APROBADO',   'Justificación aprobada',              '#28A745'),
        (@baseId + 2, 'JUSTIFICACION', 'RECHAZADO',  'Justificación rechazada',             '#DC3545');

    PRINT 'Estados JUSTIFICACION insertados con IDs ' +
          CAST(@baseId AS VARCHAR) + ', ' +
          CAST(@baseId+1 AS VARCHAR) + ', ' +
          CAST(@baseId+2 AS VARCHAR) + '.';
END
ELSE
    PRINT 'Estados JUSTIFICACION ya existen, se omiten.';
