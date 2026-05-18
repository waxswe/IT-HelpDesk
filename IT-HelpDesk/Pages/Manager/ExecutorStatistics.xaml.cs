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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IT_HelpDesk.Pages.Manager
{
    /// <summary>
    /// Логика взаимодействия для ExecutorStatistics.xaml
    /// </summary>
    public partial class ExecutorStatistics : Page
    {
        private enum Period { Month, Quarter, All }
        private Period _currentPeriod = Period.Month;
        private List<ExecutorStat> _allStats = new List<ExecutorStat>();
        private List<ExecutorStat> _currentStats = new List<ExecutorStat>();
        private int _currentPage = 1;
        private const int PageSize = 10;
        public ExecutorStatistics()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadStatistics();
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null) loc.LanguageChanged += (s, e) => LoadStatistics();
        }

        private void Period_Changed(object sender, RoutedEventArgs e)
        {
            if (PeriodMonth.IsChecked == true) _currentPeriod = Period.Month;
            else if (PeriodQuarter.IsChecked == true) _currentPeriod = Period.Quarter;
            else _currentPeriod = Period.All;
            LoadStatistics();
        }

        private async void LoadStatistics()
        {
            List<User> executors = ConnectObject.GetConnect().Users.Where(u => u.roleID == 4 && u.statusID == 1).ToList();
            DateTime now = DateTime.Now;
            DateTime startDate;
            switch (_currentPeriod)
            {
                case Period.Month:
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
                case Period.Quarter:
                    int quarterStartMonth = (now.Month - 1) / 3 * 3 + 1;
                    startDate = new DateTime(now.Year, quarterStartMonth, 1);
                    break;
                default:
                    startDate = DateTime.MinValue;
                    break;
            }
            if (_currentPeriod == Period.Quarter && startDate < new DateTime(2020, 1, 1))
                startDate = new DateTime(2020, 1, 1);

            var allAssignedInProgress = ConnectObject.GetConnect().Requests.Where(r => r.workerID != null && (r.requestStatusID == 2 || r.requestStatusID == 3 || r.requestStatusID == 4) && 
                            (_currentPeriod == Period.All || (r.createdAt >= startDate && r.createdAt <= now))).Select(r => r.workerID).GroupBy(w => w).Select(g => new { WorkerID = g.Key, Count = g.Count() }).ToList();
            int totalAssignedInProgressAll = allAssignedInProgress.Sum(x => x.Count);

            List<ExecutorStat> stats = new List<ExecutorStat>();
            foreach (User executor in executors)
            {
                List<Request> requests = ConnectObject.GetConnect().Requests.Where(r => r.workerID == executor.userID && (_currentPeriod == Period.All || (r.createdAt >= startDate && r.createdAt <= now))).ToList();

                int total = requests.Count(r => r.requestStatusID != 6);
                int assigned = requests.Count(r => r.requestStatusID == 2);
                int inProgress = requests.Count(r => r.requestStatusID == 3 || r.requestStatusID == 4);
                int completed = requests.Count(r => r.requestStatusID == 5 || r.requestStatusID == 7);
                double completionPercent = total == 0 ? 0 : (double)completed / total * 100;
                double loadPercent = totalAssignedInProgressAll == 0 ? 0 : (double)(assigned + inProgress) / totalAssignedInProgressAll * 100;

                LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
                string profession = executor.professionID.HasValue ?
                    (loc?.GetProfessionTranslation(executor.professionID.Value) ?? "—") : "—";
                string fullName = (loc?.CurrentLanguage == "en" && loc != null) ? loc.Transliterate(executor.name) : executor.name;

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
            _allStats = stats.OrderBy(s => s.FullName).ToList();
            _currentPage = 1;
            ApplyFiltersAndPaging();
        }

        private void ApplyFiltersAndPaging()
        {
            _currentStats = _allStats.ToList();
            UpdatePage();
        }

        private void UpdatePage()
        {
            if (StatisticsItemsControl == null)
                StatisticsItemsControl = this.FindName("StatisticsItemsControl") as ItemsControl;
            if (NoDataTextBlock == null)
                NoDataTextBlock = this.FindName("NoDataTextBlock") as TextBlock;
            if (PaginationPanel == null)
                PaginationPanel = this.FindName("PaginationPanel") as StackPanel;
            if (PrevPageButton == null)
                PrevPageButton = this.FindName("PrevPageButton") as Button;
            if (NextPageButton == null)
                NextPageButton = this.FindName("NextPageButton") as Button;
            if (TBPageNumber == null)
                TBPageNumber = this.FindName("TBPageNumber") as TextBox;

            if (StatisticsItemsControl == null || NoDataTextBlock == null || PaginationPanel == null)
                return;

            bool hasData = _currentStats != null && _currentStats.Count > 0;
            StatisticsItemsControl.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
            NoDataTextBlock.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
            PaginationPanel.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;

            if (!hasData) return;

            List<ExecutorStat> paged = _currentStats.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            StatisticsItemsControl.ItemsSource = paged;
            int totalPages = (int)Math.Ceiling(_currentStats.Count / (double)PageSize);

            if (PrevPageButton != null) PrevPageButton.IsEnabled = _currentPage > 1;
            if (NextPageButton != null) NextPageButton.IsEnabled = _currentPage < totalPages;
            if (TBPageNumber != null) TBPageNumber.Text = _currentPage.ToString();

            if (PageInfo != null)
                PageInfo.Text = string.Format(GetLoc("Page_Info_Format"), _currentPage, totalPages);
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            UpdatePage();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling(_currentStats.Count / (double)PageSize);
            if (_currentPage >= totalPages) return;
            _currentPage++;
            UpdatePage();
        }

        private void GoToPageButton_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling(_currentStats.Count / (double)PageSize);
            if (int.TryParse(TBPageNumber.Text, out int target) && target >= 1 && target <= totalPages)
            {
                _currentPage = target;
                UpdatePage();
            }
            else
            {
                MessageBox.Show(string.Format(GetLoc("InvalidPage_Message"), totalPages), GetLoc("InvalidPage_Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                TBPageNumber.Text = _currentPage.ToString();
            }
        }

        private void TBPageNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                GoToPageButton_Click(null, null);
        }

        private void TBPageNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportButton.ContextMenu.IsOpen = true;
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private async void ExportToExcel()
        {
            try
            {
                Period[] periods = new[] { Period.Month, Period.Quarter, Period.All };
                Dictionary<Period, List<ExecutorStat>> dataSets = new Dictionary<Period, List<ExecutorStat>>();
                Period originalPeriod = _currentPeriod;
                foreach (Period period in periods)
                {
                    _currentPeriod = period;
                    LoadStatistics();
                    dataSets[period] = _currentStats.ToList();
                }
                _currentPeriod = originalPeriod;
                LoadStatistics();

                ExcelPackage.License.SetNonCommercialPersonal("Роман");
                using (ExcelPackage package = new ExcelPackage())
                {
                    foreach (Period period in periods)
                    {
                        string sheetName;
                        switch (period)
                        {
                            case Period.Month:
                                sheetName = GetLoc("ExecutorStatistics_Period_Month");
                                break;
                            case Period.Quarter:
                                sheetName = GetLoc("ExecutorStatistics_Period_Quarter");
                                break;
                            default:
                                sheetName = GetLoc("ExecutorStatistics_Period_All");
                                break;
                        }
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(sheetName);
                        List<ExecutorStat> data = dataSets[period];
                        if (data.Any())
                        {
                            worksheet.Cells[1, 1].Value = GetLoc("ExecutorStatistics_Column_FullName");
                            worksheet.Cells[1, 2].Value = GetLoc("ExecutorStatistics_Column_Profession");
                            worksheet.Cells[1, 3].Value = GetLoc("ExecutorStatistics_Column_Total");
                            worksheet.Cells[1, 4].Value = GetLoc("ExecutorStatistics_Column_Assigned");
                            worksheet.Cells[1, 5].Value = GetLoc("ExecutorStatistics_Column_InProgress");
                            worksheet.Cells[1, 6].Value = GetLoc("ExecutorStatistics_Column_Completed");
                            worksheet.Cells[1, 7].Value = GetLoc("ExecutorStatistics_Column_CompletionPercent");
                            worksheet.Cells[1, 8].Value = GetLoc("ExecutorStatistics_Column_LoadPercent");

                            for (int i = 0; i < data.Count; i++)
                            {
                                ExecutorStat row = data[i];
                                worksheet.Cells[i + 2, 1].Value = row.FullName;
                                worksheet.Cells[i + 2, 2].Value = row.Profession;
                                worksheet.Cells[i + 2, 3].Value = row.TotalRequests;
                                worksheet.Cells[i + 2, 4].Value = row.AssignedRequests; 
                                worksheet.Cells[i + 2, 5].Value = row.InProgressRequests;
                                worksheet.Cells[i + 2, 6].Value = row.CompletedRequests;
                                worksheet.Cells[i + 2, 7].Value = row.CompletionPercent;
                                worksheet.Cells[i + 2, 8].Value = row.LoadPercent;
                            }
                            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                        }
                        else
                        {
                            worksheet.Cells[1, 1].Value = GetLoc("NoData");
                        }
                    }

                    SaveFileDialog saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel files (*.xlsx)|*.xlsx",
                        DefaultExt = "xlsx",
                        FileName = $"Статистика_Исполнителей_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    };
                    if (saveDialog.ShowDialog() == true)
                    {
                        FileInfo fi = new FileInfo(saveDialog.FileName);
                        await package.SaveAsAsync(fi);
                        MessageBox.Show(GetLoc("ExecutorStatistics_ExportSuccess"), GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(GetLoc("ExecutorStatistics_ExportError"), ex.Message), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            ExportToCsv();
        }

        private void ExportToCsv()
        {
            try
            {
                Period[] periods = new[] { Period.Month, Period.Quarter, Period.All };
                Dictionary<Period, List<ExecutorStat>> dataSets = new Dictionary<Period, List<ExecutorStat>>();
                Period originalPeriod = _currentPeriod;

                foreach (Period period in periods)
                {
                    _currentPeriod = period;
                    LoadStatistics();
                    dataSets[period] = _allStats.ToList();
                }

                _currentPeriod = originalPeriod;
                LoadStatistics();

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"Статистика_Исполнителей_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (StreamWriter writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                    {
                        foreach (Period period in periods)
                        {
                            string title;
                            switch (period)
                            {
                                case Period.Month:
                                    title = GetLoc("ExecutorStatistics_Period_Month");
                                    break;
                                case Period.Quarter:
                                    title = GetLoc("ExecutorStatistics_Period_Quarter");
                                    break;
                                default:
                                    title = GetLoc("ExecutorStatistics_Period_All");
                                    break;
                            }
                            writer.WriteLine($"--- {title} ---");

                            // Заголовки
                            string[] headers = {
                                GetLoc("ExecutorStatistics_Column_FullName"),
                                GetLoc("ExecutorStatistics_Column_Profession"),
                                GetLoc("ExecutorStatistics_Column_Total"),
                                GetLoc("ExecutorStatistics_Column_Assigned"),
                                GetLoc("ExecutorStatistics_Column_InProgress"),
                                GetLoc("ExecutorStatistics_Column_Completed"),
                                GetLoc("ExecutorStatistics_Column_CompletionPercent"),
                                GetLoc("ExecutorStatistics_Column_LoadPercent")
                            };
                            writer.WriteLine(string.Join(";", headers));

                            // Данные
                            foreach (var stat in dataSets[period])
                            {
                                string[] row = {
                                    stat.FullName,
                                    stat.Profession,
                                    stat.TotalRequests.ToString(),
                                    stat.AssignedRequests.ToString(),
                                    stat.InProgressRequests.ToString(),
                                    stat.CompletedRequests.ToString(),
                                    stat.CompletionPercent,
                                    stat.LoadPercent
                                };
                                writer.WriteLine(string.Join(";", row));
                            }
                            writer.WriteLine();
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.GoBack();
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }
    }
}

