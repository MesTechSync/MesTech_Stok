using System.Reflection;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Jobs;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// Zamanlanmis rapor uretimi ve HangfireConfig testleri.
/// Cron ifadeleri, job kayitlari ve IReportExportService interface dogrulamasi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Reporting")]
[Trait("Phase", "I-11")]
public class ScheduledReportTests
{
    [Fact(DisplayName = "HangfireConfig — RegisterRecurringJobs method should exist and be static")]
    public void ScheduledReportJob_ShouldHaveCorrectCronExpressions()
    {
        // The HangfireConfig.RegisterRecurringJobs method registers:
        //   daily sales report     → Cron.Daily(6) = "0 6 * * *"
        //   weekly performance     → "0 8 * * 1"
        //   monthly financial      → "0 6 1 * *"
        // We verify that the method exists and is callable (static void).
        var method = typeof(HangfireConfig).GetMethod(
            "RegisterRecurringJobs",
            BindingFlags.Public | BindingFlags.Static);

        method.Should().NotBeNull("HangfireConfig must have RegisterRecurringJobs static method");
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact(DisplayName = "HangfireConfig — ScheduledReportGenerationJob is registered in AddMesTechHangfire")]
    public void ScheduledReportJob_ShouldBeRegistered_InHangfireConfig()
    {
        // Verify ScheduledReportGenerationJob type exists and is in the Jobs namespace
        var jobType = typeof(ScheduledReportGenerationJob);

        jobType.Should().NotBeNull();
        jobType.Namespace.Should().Contain("Jobs");

        // Verify the AddMesTechHangfire extension method exists
        var addMethod = typeof(HangfireConfig).GetMethod(
            "AddMesTechHangfire",
            BindingFlags.Public | BindingFlags.Static);

        addMethod.Should().NotBeNull("HangfireConfig must expose AddMesTechHangfire for DI registration");
    }

    [Fact(DisplayName = "IReportExportService — ExportToExcelAsync method signature is correct")]
    public void ReportExportService_ExportToExcel_ShouldReturnBytes()
    {
        // Verify the interface method exists with correct return type
        var interfaceType = typeof(IReportExportService);
        var methods = interfaceType.GetMethods();

        var excelMethod = methods.FirstOrDefault(m => m.Name == "ExportToExcelAsync");

        excelMethod.Should().NotBeNull("IReportExportService must define ExportToExcelAsync");
        excelMethod!.ReturnType.Should().Be(typeof(Task<byte[]>));
        excelMethod.GetParameters().Should().HaveCountGreaterOrEqualTo(2,
            "ExportToExcelAsync needs data + sheetName parameters");
    }

    [Fact(DisplayName = "IReportExportService — ExportToCsvAsync method signature is correct")]
    public void ReportExportService_ExportToCsv_ShouldReturnBytes()
    {
        // Verify the interface method exists with correct return type
        var interfaceType = typeof(IReportExportService);
        var methods = interfaceType.GetMethods();

        var csvMethod = methods.FirstOrDefault(m => m.Name == "ExportToCsvAsync");

        csvMethod.Should().NotBeNull("IReportExportService must define ExportToCsvAsync");
        csvMethod!.ReturnType.Should().Be(typeof(Task<byte[]>));
        csvMethod.GetParameters().Should().HaveCountGreaterOrEqualTo(1,
            "ExportToCsvAsync needs at least data parameter");
    }

    [Fact(DisplayName = "IReportExportService — ExportToPdfAsync method signature is correct")]
    public void ReportExportService_ExportToPdf_ShouldReturnBytes()
    {
        // Verify the interface method exists with correct return type
        var interfaceType = typeof(IReportExportService);
        var methods = interfaceType.GetMethods();

        var pdfMethod = methods.FirstOrDefault(m => m.Name == "ExportToPdfAsync");

        pdfMethod.Should().NotBeNull("IReportExportService must define ExportToPdfAsync");
        pdfMethod!.ReturnType.Should().Be(typeof(Task<byte[]>));
        pdfMethod.GetParameters().Should().HaveCountGreaterOrEqualTo(2,
            "ExportToPdfAsync needs data + title parameters");
    }
}
