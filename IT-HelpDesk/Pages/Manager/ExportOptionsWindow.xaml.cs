using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IT_HelpDesk.Pages.Manager
{
    /// <summary>
    /// Логика взаимодействия для ExportOptionsWindow.xaml
    /// </summary>
    public partial class ExportOptionsWindow : Window
    {
        private LocalizationManager _loc;
        private List<ExecutorStat> _allStats;
        public ExportOptionsWindow(List<ExecutorStat> allStats)
        {
            InitializeComponent();
            _loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            _allStats = allStats;
            SetPresetPeriod("CurrentMonth");
            PeriodCurrentMonth.IsChecked = true;
        }

        private void PresetPeriod_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null) return;

            if (rb == PeriodCurrentMonth)
            {
                SetPresetPeriod("CurrentMonth");
                DateFrom.IsEnabled = false;
                DateTo.IsEnabled = false;
            }
            else if (rb == PeriodPreviousMonth)
            {
                SetPresetPeriod("PreviousMonth");
                DateFrom.IsEnabled = false;
                DateTo.IsEnabled = false;
            }
            else if (rb == PeriodAllTime)
            {
                SetPresetPeriod("AllTime");
                DateFrom.IsEnabled = false;
                DateTo.IsEnabled = false;
            }
            else if (rb == PeriodCustom)
            {
                DateFrom.IsEnabled = true;
                DateTo.IsEnabled = true;
                DateFrom.SelectedDate = DateTime.Now.AddMonths(-1);
                DateTo.SelectedDate = DateTime.Now;
            }
        }

        private void SetPresetPeriod(string preset)
        {
            DateTime now = DateTime.Now;
            switch (preset)
            {
                case "CurrentMonth":
                    DateFrom.SelectedDate = new DateTime(now.Year, now.Month, 1);
                    DateTo.SelectedDate = DateFrom.SelectedDate.Value.AddMonths(1).AddDays(-1);
                    break;
                case "PreviousMonth":
                    DateFrom.SelectedDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    DateTo.SelectedDate = DateFrom.SelectedDate.Value.AddMonths(1).AddDays(-1);
                    break;
                case "AllTime":
                    DateFrom.SelectedDate = null;
                    DateTo.SelectedDate = null;
                    break;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime? from = DateFrom.SelectedDate;
            DateTime? to = DateTo.SelectedDate;
            if (from == null && to == null && (PeriodCustom.IsChecked == false))
            {
            }
            else if (from == null || to == null)
            {
                MessageBox.Show("Укажите начальную и конечную дату", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<ExecutorStat> filteredStats = _allStats;
            string period = "";
            if (PeriodCurrentMonth.IsChecked == true) period = "CurrentMonth";
            else if (PeriodPreviousMonth.IsChecked == true) period = "PreviousMonth";
            else if (PeriodAllTime.IsChecked == true) period = "AllTime";
            else period = "Custom";

            // Загружаем данные за выбранный период
            List<ExecutorStat> newStats = LoadStatisticsForPeriod(from, to, period);
            if (RadioExcel.IsChecked == true)
                ExportToExcel(newStats, from, to, period);
            else
                ExportToCsv(newStats, from, to, period);

            DialogResult = true;
            Close();
        }

        private List<ExecutorStat> LoadStatisticsForPeriod(DateTime? from, DateTime? to, string period)
        {
            List<User> executors = ConnectObject.GetConnect().Users.Where(u => u.roleID == 4 && u.statusID == 1).ToList();
            DateTime startDate, endDate;
            if (period == "AllTime")
            {
                startDate = DateTime.MinValue;
                endDate = DateTime.MaxValue;
            }
            else
            {
                startDate = from.Value;
                endDate = to.Value;
            }

            List<Request> allRequests = ConnectObject.GetConnect().Requests.Where(r => r.workerID != null && (r.requestStatusID >= 2 && r.requestStatusID <= 5 || r.requestStatusID == 7) &&
                            (period == "AllTime" || (r.createdAt >= startDate && r.createdAt <= endDate))).ToList();

            List<Request> globalInProgress = allRequests.Where(r => r.requestStatusID >= 2 && r.requestStatusID <= 4).ToList();
            int globalInProgressCount = globalInProgress.Count;

            List<ExecutorStat> stats = new List<ExecutorStat>();
            foreach (User executor in executors)
            {
                List<Request> requests = allRequests.Where(r => r.workerID == executor.userID).ToList();
                int total = requests.Count;
                int assigned = requests.Count(r => r.requestStatusID == 2);
                int inProgress = requests.Count(r => r.requestStatusID == 3 || r.requestStatusID == 4);
                int completed = requests.Count(r => r.requestStatusID == 5 || r.requestStatusID == 7);
                double completionPercent = total == 0 ? 0 : (double)completed / total * 100;
                double loadPercent = globalInProgressCount == 0 ? 0 : (double)(assigned + inProgress) / globalInProgressCount * 100;

                string profession = executor.professionID.HasValue ?
                    (_loc?.GetProfessionTranslation(executor.professionID.Value) ?? "—") : "—";
                string fullName = (_loc?.CurrentLanguage == "en" && _loc != null) ? _loc.Transliterate(executor.name) : executor.name;

                stats.Add(new ExecutorStat
                {
                    FullName = fullName,
                    Profession = profession,
                    TotalRequests = total,
                    AssignedRequests = assigned,
                    InProgressRequests = inProgress,
                    CompletedRequests = completed,
                    CompletionPercent = completionPercent.ToString("F1") + "%",
                    LoadPercent = loadPercent.ToString("F1") + "%"
                });
            }
            return stats.OrderBy(s => s.FullName).ToList();
        }

        private void ExportToExcel(List<ExecutorStat> data, DateTime? from, DateTime? to, string period)
        {
            try
            {
                ExcelPackage.License.SetNonCommercialPersonal("YourName");
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Report");
                    // Заголовок с периодом
                    string header = GetPeriodHeader(from, to, period);
                    worksheet.Cells[1, 1].Value = header;
                    worksheet.Cells[1, 1, 1, 8].Merge = true;
                    worksheet.Cells[2, 1].Value = GetLoc("ExecutorStatistics_Column_FullName");
                    worksheet.Cells[2, 2].Value = GetLoc("ExecutorStatistics_Column_Profession");
                    worksheet.Cells[2, 3].Value = GetLoc("ExecutorStatistics_Column_Total");
                    worksheet.Cells[2, 4].Value = GetLoc("ExecutorStatistics_Column_Assigned");
                    worksheet.Cells[2, 5].Value = GetLoc("ExecutorStatistics_Column_InProgress");
                    worksheet.Cells[2, 6].Value = GetLoc("ExecutorStatistics_Column_Completed");
                    worksheet.Cells[2, 7].Value = GetLoc("ExecutorStatistics_Column_CompletionPercent");
                    worksheet.Cells[2, 8].Value = GetLoc("ExecutorStatistics_Column_LoadPercent");

                    for (int i = 0; i < data.Count; i++)
                    {
                        ExecutorStat row = data[i];
                        worksheet.Cells[i + 3, 1].Value = row.FullName;
                        worksheet.Cells[i + 3, 2].Value = row.Profession;
                        worksheet.Cells[i + 3, 3].Value = row.TotalRequests;
                        worksheet.Cells[i + 3, 4].Value = row.AssignedRequests;
                        worksheet.Cells[i + 3, 5].Value = row.InProgressRequests;
                        worksheet.Cells[i + 3, 6].Value = row.CompletedRequests;
                        worksheet.Cells[i + 3, 7].Value = row.CompletionPercent;
                        worksheet.Cells[i + 3, 8].Value = row.LoadPercent;
                    }
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    SaveFileDialog saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel files (*.xlsx)|*.xlsx",
                        DefaultExt = "xlsx",
                        FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    };
                    if (saveDialog.ShowDialog() == true)
                    {
                        package.SaveAs(saveDialog.FileName);
                        MessageBox.Show(GetLoc("ExecutorStatistics_ExportSuccess"), GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(GetLoc("ExecutorStatistics_ExportError"), ex.Message), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(List<ExecutorStat> data, DateTime? from, DateTime? to, string period)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };
                if (saveDialog.ShowDialog() == true)
                {
                    using (StreamWriter writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                    {
                        string header = GetPeriodHeader(from, to, period);
                        writer.WriteLine(header);
                        writer.WriteLine(string.Join(";", new[] {
                            GetLoc("ExecutorStatistics_Column_FullName"),
                            GetLoc("ExecutorStatistics_Column_Profession"),
                            GetLoc("ExecutorStatistics_Column_Total"),
                            GetLoc("ExecutorStatistics_Column_Assigned"),
                            GetLoc("ExecutorStatistics_Column_InProgress"),
                            GetLoc("ExecutorStatistics_Column_Completed"),
                            GetLoc("ExecutorStatistics_Column_CompletionPercent"),
                            GetLoc("ExecutorStatistics_Column_LoadPercent")
                        }));
                        foreach (var row in data)
                        {
                            writer.WriteLine(string.Join(";", new[] {
                                row.FullName,
                                row.Profession,
                                row.TotalRequests.ToString(),
                                row.AssignedRequests.ToString(),
                                row.InProgressRequests.ToString(),
                                row.CompletedRequests.ToString(),
                                row.CompletionPercent,
                                row.LoadPercent
                            }));
                        }
                    }
                    MessageBox.Show(GetLoc("ExecutorStatistics_ExportSuccess"), GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(GetLoc("ExecutorStatistics_ExportError"), ex.Message), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetPeriodHeader(DateTime? from, DateTime? to, string period)
        {
            if (period == "CurrentMonth" && from.HasValue && to.HasValue)
                return string.Format(GetLoc("Export_DateRange_CurrentMonth"), from.Value.ToString("dd.MM.yyyy"), to.Value.ToString("dd.MM.yyyy"));
            if (period == "PreviousMonth" && from.HasValue && to.HasValue)
                return string.Format(GetLoc("Export_DateRange_PreviousMonth"), from.Value.ToString("dd.MM.yyyy"), to.Value.ToString("dd.MM.yyyy"));
            if (period == "AllTime")
                return GetLoc("Export_DateRange_AllTime");
            if (from.HasValue && to.HasValue)
                return $"{GetLoc("Export_DateRange_Custom")} ({from.Value:dd.MM.yyyy} - {to.Value:dd.MM.yyyy})";
            return "";
        }

        private string GetLoc(string key)
        {
            return _loc?[key] ?? key;
        }
    }
}
