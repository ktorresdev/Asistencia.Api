-- ============================================================
-- DIAGNOSTICO: ¿por que un DNI no puede iniciar sesion?
-- Reemplaza el valor de @dni y ejecuta en DB_RRHH.
-- (Equivale al modulo "Diagnostico RRHH -> Login" de la web.)
-- ============================================================
USE [DB_RRHH];
DECLARE @dni VARCHAR(15) = '12345678';   -- <== pon aqui el DNI a revisar

-- 1) Persona / Trabajador / Usuario + diagnostico resumido
SELECT
    p.id_persona,
    p.dni,
    p.apellidos_nombres,
    t.id_trabajador,
    t.id_estado,
    CASE t.id_estado WHEN 10 THEN 'ACTIVO' WHEN 11 THEN 'CESADO'
         ELSE 'OTRO(' + CAST(t.id_estado AS VARCHAR) + ')' END AS estado_trabajador,
    t.id_user,
    u.username,
    u.role,
    CASE WHEN u.password_hash IS NULL OR u.password_hash = '' THEN 'SIN PASSWORD'
         ELSE 'tiene hash' END AS password,
    LEN(u.password_hash) AS len_hash,
    CASE
        WHEN p.id_persona  IS NULL THEN 'DNI NO REGISTRADO en PERSONAS'
        WHEN t.id_trabajador IS NULL THEN 'La persona existe pero NO es trabajador'
        WHEN t.id_user IS NULL OR u.id_user IS NULL THEN 'El trabajador NO tiene usuario de acceso'
        WHEN u.password_hash IS NULL OR u.password_hash = '' THEN 'Usuario sin contrasena configurada'
        ELSE 'Usuario OK -> si falla es CONTRASENA incorrecta (restablecer)'
    END AS diagnostico
FROM dbo.PERSONAS p
LEFT JOIN dbo.TRABAJADORES t ON t.id_persona = p.id_persona
LEFT JOIN dbo.USERS u        ON u.id_user    = t.id_user
WHERE p.dni = @dni;

-- 2) Por si el username NO es el DNI: buscar usuario con ese username
SELECT id_user, username, role FROM dbo.USERS WHERE username = @dni;

-- 3) Ultimos intentos de login (OK / FAIL con motivo) de ese usuario
SELECT TOP 10 a.created_at, a.username_intentado, a.resultado, a.motivo_fallo, a.ip_address
FROM dbo.AUDIT_LOGIN a
WHERE a.username_intentado = @dni
   OR a.username_intentado = (SELECT u.username FROM dbo.USERS u
                              JOIN dbo.TRABAJADORES t ON t.id_user = u.id_user
                              JOIN dbo.PERSONAS p ON p.id_persona = t.id_persona
                              WHERE p.dni = @dni)
ORDER BY a.created_at DESC;

-- ============================================================
-- COMO LEERLO
--   Bloque 1 -> columna 'diagnostico' da el motivo directo:
--     - DNI NO REGISTRADO          -> no existe la persona
--     - no es trabajador           -> falta crear el trabajador
--     - NO tiene usuario de acceso -> falta crear el usuario
--     - sin contrasena             -> password_hash vacio
--     - Usuario OK                 -> existe; el problema es la clave (restablecer)
--   Bloque 2 -> confirma el username real (normalmente es el DNI)
--   Bloque 3 -> motivo de los ultimos fallos (ej. "Credenciales invalidas"), fecha e IP
-- ============================================================
