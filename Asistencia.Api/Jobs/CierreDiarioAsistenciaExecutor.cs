using Asistencia.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Asistencia.Api.Jobs
{
    public class CierreDiarioAsistenciaExecutor : ICierreDiarioAsistenciaExecutor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CierreDiarioAsistenciaExecutor> _logger;

        public CierreDiarioAsistenciaExecutor(IServiceScopeFactory scopeFactory, ILogger<CierreDiarioAsistenciaExecutor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ExecuteStoredProcedureAsync(DateTime fechaProceso, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MarcacionAsistenciaDbContext>();
            var fecha = fechaProceso.Date;

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC [dbo].[SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA] {fecha}",
                cancellationToken);

            _logger.LogInformation("SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA ejecutado correctamente para fecha {FechaProceso:yyyy-MM-dd}.", fecha);
        }
    }
}
