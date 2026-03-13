namespace Asistencia.Api.Jobs
{
    public interface ICierreDiarioAsistenciaExecutor
    {
        Task ExecuteStoredProcedureAsync(DateTime fechaProceso, CancellationToken cancellationToken);
    }
}
