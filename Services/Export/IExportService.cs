// Services/Export/IExportService.cs  
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AplicacionDespacho.Services.Export
{
    public interface IExportService<T>
    {
        Task<ExportResult> ExportAsync(IEnumerable<T> data, ExportOptions options);
        bool SupportsFormat(ExportFormat format);
        string GetDefaultFileExtension(ExportFormat format);
    }

    public enum ExportFormat
    {
        Excel,
        PDF,
        CSV,
        JSON
    }

    public class ExportOptions
    {
        public string FilePath { get; set; }
        public ExportFormat Format { get; set; }
        public string TemplateName { get; set; } = "Default";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public bool IncludeHeaders { get; set; } = true;
        public string Title { get; set; }
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
        public Exception Exception { get; set; }

        public static ExportResult CreateSuccess(string filePath, string message = "Exportación exitosa")
        {
            return new ExportResult { Success = true, FilePath = filePath, Message = message };
        }

        public static ExportResult CreateError(string message, Exception exception = null)
        {
            return new ExportResult { Success = false, Message = message, Exception = exception };
        }
    }
}