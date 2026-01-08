using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDesigner.XamlDesigner;

/// <summary>
/// Control element for displaying RDLC reports in Avalonia UI
/// Note: This is a simplified viewer as Avalonia does not support ReportViewer directly
/// </summary>
public partial class RdlcReportViewer : UserControl
{
    private string? _currentReportPath;
    private object? _currentDataSource;
    private Dictionary<string, object>? _currentDataSources;

    public RdlcReportViewer()
    {
        InitializeComponent();
    }

    /// <summary>
    /// تحميل وعرض تقرير RDLC
    /// </summary>
    /// <param name="reportPath">مسار ملف RDLC</param>
    /// <param name="dataSource">مصدر البيانات</param>
    /// <param name="dataSourceName">اسم مصدر البيانات في التقرير</param>
    public void LoadReport(string reportPath, object dataSource, string dataSourceName = "DataSet1")
    {
        try
        {
            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"ملف التقرير غير موجود: {reportPath}");
            }

            _currentReportPath = reportPath;
            _currentDataSource = dataSource;
            _currentDataSources = null;

            DisplayReport(reportPath, dataSource, dataSourceName);
        }
        catch (Exception ex)
        {
            ShowError($"خطأ في تحميل التقرير: {ex.Message}");
        }
    }

    /// <summary>
    /// تحميل تقرير مع مصادر بيانات متعددة
    /// </summary>
    public void LoadReport(string reportPath, Dictionary<string, object> dataSources)
    {
        try
        {
            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"ملف التقرير غير موجود: {reportPath}");
            }

            _currentReportPath = reportPath;
            _currentDataSources = dataSources;
            _currentDataSource = null;

            DisplayReport(reportPath, dataSources);
        }
        catch (Exception ex)
        {
            ShowError($"خطأ في تحميل التقرير: {ex.Message}");
        }
    }

    /// <summary>
    /// تحميل تقرير مع معاملات
    /// </summary>
    public void LoadReport(string reportPath, object dataSource, string dataSourceName, Dictionary<string, string> parameters)
    {
        try
        {
            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"ملف التقرير غير موجود: {reportPath}");
            }

            _currentReportPath = reportPath;
            _currentDataSource = dataSource;
            _currentDataSources = null;

            DisplayReport(reportPath, dataSource, dataSourceName, parameters);
        }
        catch (Exception ex)
        {
            ShowError($"خطأ في تحميل التقرير: {ex.Message}");
        }
    }

    private void DisplayReport(string reportPath, object dataSource, string dataSourceName = "DataSet1", Dictionary<string, string>? parameters = null)
    {
        var reportTitle = this.FindControl<TextBlock>("ReportTitle");
        var defaultContent = this.FindControl<StackPanel>("DefaultContent");
        var reportContent = this.FindControl<StackPanel>("ReportContent");
        var reportInfo = this.FindControl<TextBlock>("ReportInfo");
        var dataPreviewBorder = this.FindControl<Border>("DataPreviewBorder");
        var dataPreviewText = this.FindControl<TextBlock>("DataPreviewText");

        if (reportTitle != null)
            reportTitle.Text = Path.GetFileName(reportPath);

        if (defaultContent != null)
            defaultContent.IsVisible = false;

        if (reportContent != null)
            reportContent.IsVisible = true;

        // عرض معلومات التقرير
        var info = new StringBuilder();
        info.AppendLine($"📄 ملف التقرير: {Path.GetFileName(reportPath)}");
        info.AppendLine($"📁 المسار: {reportPath}");
        info.AppendLine($"📅 تاريخ التحميل: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        info.AppendLine();

        if (dataSource != null)
        {
            if (dataSource is DataTable dt)
            {
                info.AppendLine($"📊 نوع مصدر البيانات: DataTable");
                info.AppendLine($"📈 اسم مصدر البيانات: {dataSourceName}");
                info.AppendLine($"🔢 عدد الصفوف: {dt.Rows.Count:N0}");
                info.AppendLine($"📋 عدد الأعمدة: {dt.Columns.Count}");
                info.AppendLine();

                if (dt.Columns.Count > 0)
                {
                    info.AppendLine("📋 الأعمدة:");
                    foreach (DataColumn column in dt.Columns)
                    {
                        info.AppendLine($"   • {column.ColumnName} ({column.DataType.Name})");
                    }
                    info.AppendLine();
                }

                // عرض معاينة البيانات
                if (dataPreviewBorder != null && dataPreviewText != null)
                {
                    dataPreviewBorder.IsVisible = true;
                    dataPreviewText.Text = GenerateDataPreview(dt);
                }
            }
            else
            {
                info.AppendLine($"📊 نوع مصدر البيانات: {dataSource.GetType().Name}");
                info.AppendLine($"📈 اسم مصدر البيانات: {dataSourceName}");
            }
        }

        if (parameters != null && parameters.Count > 0)
        {
            info.AppendLine("⚙️ المعاملات:");
            foreach (var param in parameters)
            {
                info.AppendLine($"   • {param.Key}: {param.Value}");
            }
            info.AppendLine();
        }

        info.AppendLine("ℹ️ ملاحظات:");
        info.AppendLine("   • هذا عارض مبسط للتقارير في Avalonia UI");
        info.AppendLine("   • لعرض التقارير الكامل، يُنصح باستخدام:");
        info.AppendLine("     - FastReport.NET");
        info.AppendLine("     - DevExpress Reporting");
        info.AppendLine("     - Telerik Reporting");
        info.AppendLine("     - Crystal Reports");

        if (reportInfo != null)
            reportInfo.Text = info.ToString();
    }

    private void DisplayReport(string reportPath, Dictionary<string, object> dataSources)
    {
        var reportTitle = this.FindControl<TextBlock>("ReportTitle");
        var defaultContent = this.FindControl<StackPanel>("DefaultContent");
        var reportContent = this.FindControl<StackPanel>("ReportContent");
        var reportInfo = this.FindControl<TextBlock>("ReportInfo");

        if (reportTitle != null)
            reportTitle.Text = Path.GetFileName(reportPath);

        if (defaultContent != null)
            defaultContent.IsVisible = false;

        if (reportContent != null)
            reportContent.IsVisible = true;

        var info = new StringBuilder();
        info.AppendLine($"📄 ملف التقرير: {Path.GetFileName(reportPath)}");
        info.AppendLine($"📁 المسار: {reportPath}");
        info.AppendLine($"📅 تاريخ التحميل: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        info.AppendLine();

        info.AppendLine($"📊 مصادر البيانات المتعددة: {dataSources.Count}");
        foreach (var ds in dataSources)
        {
            info.AppendLine($"   • {ds.Key}: {ds.Value?.GetType().Name}");
            if (ds.Value is DataTable dt)
            {
                info.AppendLine($"     - الصفوف: {dt.Rows.Count:N0}, الأعمدة: {dt.Columns.Count}");
            }
        }

        if (reportInfo != null)
            reportInfo.Text = info.ToString();
    }

    private string GenerateDataPreview(DataTable dataTable)
    {
        var preview = new StringBuilder();

        if (dataTable.Rows.Count == 0)
        {
            return "لا توجد بيانات للعرض";
        }

        // عرض أسماء الأعمدة
        var columnNames = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
        preview.AppendLine(string.Join(" | ", columnNames));
        preview.AppendLine(new string('-', columnNames.Sum(c => c.Length) + (columnNames.Length - 1) * 3));

        // عرض أول 10 صفوف
        var rowsToShow = Math.Min(10, dataTable.Rows.Count);
        for (int i = 0; i < rowsToShow; i++)
        {
            var row = dataTable.Rows[i];
            var values = row.ItemArray.Select(v => v?.ToString() ?? "NULL").ToArray();
            preview.AppendLine(string.Join(" | ", values));
        }

        if (dataTable.Rows.Count > 10)
        {
            preview.AppendLine($"... و {dataTable.Rows.Count - 10} صف إضافي");
        }

        return preview.ToString();
    }

    private void ShowError(string message)
    {
        var reportTitle = this.FindControl<TextBlock>("ReportTitle");
        var defaultContent = this.FindControl<StackPanel>("DefaultContent");
        var reportContent = this.FindControl<StackPanel>("ReportContent");
        var reportInfo = this.FindControl<TextBlock>("ReportInfo");

        if (reportTitle != null)
            reportTitle.Text = "خطأ في التقرير";

        if (defaultContent != null)
            defaultContent.IsVisible = false;

        if (reportContent != null)
            reportContent.IsVisible = true;

        if (reportInfo != null)
        {
            reportInfo.Text = $"❌ خطأ: {message}\n\nيرجى التحقق من:\n• وجود ملف التقرير\n• صحة مصدر البيانات\n• صحة معاملات التقرير";
        }
    }

    /// <summary>
    /// تصدير التقرير إلى PDF (مبسط)
    /// </summary>
    public async Task ExportToPdf(string outputPath)
    {
        try
        {
            var content = GetReportContent();
            await File.WriteAllTextAsync(outputPath, content);
        }
        catch (Exception ex)
        {
            throw new Exception($"خطأ في تصدير التقرير: {ex.Message}");
        }
    }

    /// <summary>
    /// تصدير التقرير إلى Excel (مبسط)
    /// </summary>
    public async Task ExportToExcel(string outputPath)
    {
        try
        {
            var content = GetReportContent();
            await File.WriteAllTextAsync(outputPath, content);
        }
        catch (Exception ex)
        {
            throw new Exception($"خطأ في تصدير التقرير: {ex.Message}");
        }
    }

    /// <summary>
    /// طباعة التقرير (غير مدعوم في Avalonia)
    /// </summary>
    public void PrintReport()
    {
        throw new NotSupportedException("الطباعة غير مدعومة في Avalonia UI. يُنصح باستخدام مكتبات طباعة خارجية.");
    }

    /// <summary>
    /// تصدير التقرير إلى Word (مبسط)
    /// </summary>
    public async Task ExportToWord(string outputPath)
    {
        try
        {
            var content = GetReportContent();
            await File.WriteAllTextAsync(outputPath, content);
        }
        catch (Exception ex)
        {
            throw new Exception($"خطأ في تصدير التقرير: {ex.Message}");
        }
    }

    /// <summary>
    /// مسح التقرير الحالي
    /// </summary>
    public void ClearReport()
    {
        var reportTitle = this.FindControl<TextBlock>("ReportTitle");
        var defaultContent = this.FindControl<StackPanel>("DefaultContent");
        var reportContent = this.FindControl<StackPanel>("ReportContent");
        var dataPreviewBorder = this.FindControl<Border>("DataPreviewBorder");

        if (reportTitle != null)
            reportTitle.Text = "لا يوجد تقرير محمل";

        if (defaultContent != null)
            defaultContent.IsVisible = true;

        if (reportContent != null)
            reportContent.IsVisible = false;

        if (dataPreviewBorder != null)
            dataPreviewBorder.IsVisible = false;

        _currentReportPath = null;
        _currentDataSource = null;
        _currentDataSources = null;
    }

    /// <summary>
    /// حفظ التقرير
    /// </summary>
    public async Task SaveReport(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentReportPath))
            {
                throw new InvalidOperationException("لا يوجد تقرير محمل للحفظ");
            }

            // نسخ ملف التقرير إلى الموقع الجديد
            if (File.Exists(_currentReportPath))
            {
                File.Copy(_currentReportPath, filePath, true);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"خطأ في حفظ التقرير: {ex.Message}");
        }
    }

    /// <summary>
    /// تكبير العرض
    /// </summary>
    public void ZoomIn()
    {
        var scrollViewer = this.FindControl<ScrollViewer>("ContentScrollViewer");
        if (scrollViewer != null)
        {
            // scrollViewer.ZoomToLevel(scrollViewer.ZoomLevel * 1.1);
        }
    }

    /// <summary>
    /// تصغير العرض
    /// </summary>
    public void ZoomOut()
    {
        var scrollViewer = this.FindControl<ScrollViewer>("ContentScrollViewer");
        if (scrollViewer != null)
        {
            //  scrollViewer.ZoomToLevel(scrollViewer.ZoomLevel * 0.9);
        }
    }

    /// <summary>
    /// تعيين مستوى التكبير
    /// </summary>
    public void SetZoom(double zoomLevel)
    {
        var scrollViewer = this.FindControl<ScrollViewer>("ContentScrollViewer");
        if (scrollViewer != null && zoomLevel >= 0.1 && zoomLevel <= 4.0)
        {
            // scrollViewer.ZoomToLevel(zoomLevel);
        }
    }

    /// <summary>
    /// الحصول على عدد الصفحات (مبسط)
    /// </summary>
    public int GetPageCount()
    {
        // في التطبيق الحقيقي، هذا يعتمد على محتوى التقرير
        return 1;
    }

    private string GetReportContent()
    {
        var reportInfo = this.FindControl<TextBlock>("ReportInfo");
        return reportInfo?.Text ?? "لا يوجد محتوى تقرير";
    }
}