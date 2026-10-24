namespace RamTracking.Services;

using RamTracking.Models;

public interface IReportService
{
    Task ExportTxtAsync(ReportData data, string filePath);
    Task ExportCsvAsync(ReportData data, string filePath);
}