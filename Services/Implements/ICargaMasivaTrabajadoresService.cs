using Asistencia.Services.Dtos;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface ICargaMasivaTrabajadoresService
    {
        Task<CargaMasivaTrabajadoresResultadoDto> ProcesarCsvAsync(Stream streamCsv, string nombreArchivo, CancellationToken cancellationToken = default);
        Task<CargaMasivaTrabajadoresResultadoDto> ProcesarXlsxAsync(Stream streamXlsx, string nombreArchivo, CancellationToken cancellationToken = default);
    }
}
