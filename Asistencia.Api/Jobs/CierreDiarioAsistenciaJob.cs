namespace Asistencia.Api.Jobs
{
    public class CierreDiarioAsistenciaJob : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);
        private readonly ICierreDiarioAsistenciaExecutor _executor;
        private readonly ILogger<CierreDiarioAsistenciaJob> _logger;

        public CierreDiarioAsistenciaJob(ICierreDiarioAsistenciaExecutor executor, ILogger<CierreDiarioAsistenciaJob> logger)
        {
            _executor = executor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cierre diario de asistencia iniciado. Intervalo: {IntervalMinutes} minutos.", Interval.TotalMinutes);

            await ExecuteStoredProcedureAsync(stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecuteStoredProcedureAsync(stoppingToken);
            }
        }

        private async Task ExecuteStoredProcedureAsync(CancellationToken cancellationToken)
        {
            try
            {
                var fechaProceso = DateTime.Today;
                await _executor.ExecuteStoredProcedureAsync(fechaProceso, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Ejecución del cierre diario cancelada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA.");
            }
        }
    }
}
