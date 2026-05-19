using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Pages._General;
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
        private enum Period { CurrentMonth, PreviousMonth, AllTime }
        private Period _currentPeriod = Period.CurrentMonth;
        private List<ExecutorStat> _allStats = new List<ExecutorStat>();
        private List<ExecutorStat> _currentStats = new List<ExecutorStat>();
        private int _currentPage = 1;
        private const int PageSize = 8;
        private string _sortColumn = "FullName";
        private bool _sortAscending = true;
        public ExecutorStatistics()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadStatistics();
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null) loc.LanguageChanged += (s, e) => LoadStatistics();
        }

        private void Period_Changed(object sender, RoutedEventArgs e)
        {
            if (PeriodCurrentMonth.IsChecked == true) _currentPeriod = Period.CurrentMonth;
            else if (PeriodPreviousMonth.IsChecked == true) _currentPeriod = Period.PreviousMonth;
            else _currentPeriod = Period.AllTime;
            LoadStatistics();
        }

        private async void LoadStatistics()
        {
            List<User> executors = ConnectObject.GetConnect().Users.Where(u => u.roleID == 4 && u.statusID == 1).ToList();
            DateTime now = DateTime.Now;
            DateTime startDate, endDate;
            switch (_currentPeriod)
            {
                case Period.CurrentMonth:
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;
                case Period.PreviousMonth:
                    startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;
                default:
                    startDate = DateTime.MinValue;
                    endDate = DateTime.MaxValue;
                    break;
            }

            List<Request> allRequests = ConnectObject.GetConnect().Requests.Where(r => r.workerID != null && (r.requestStatusID >= 2 && r.requestStatusID <= 5 || r.requestStatusID == 7) &&
                   (_currentPeriod == Period.AllTime || (r.createdAt >= startDate && r.createdAt <= endDate))).ToList();

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

                LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
                string profession = executor.professionID.HasValue ?
                    (loc?.GetProfessionTranslation(executor.professionID.Value) ?? "—") : "—";
                string fullName = (loc?.CurrentLanguage == "en" && loc != null) ? loc.Transliterate(executor.name) : executor.name;

                stats.Add(new ExecutorStat
                {
                    UserID = executor.userID,
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
            _sortColumn = "FullName";
            _sortAscending = true;
            _currentPage = 1;
            ApplyFiltersAndPaging();
        }

        private void ApplyFiltersAndPaging()
        {
            _currentStats = _allStats.ToList();
            UpdatePage();
        }

        private void SortStats()
        {
            if (_allStats == null || _allStats.Count == 0) return;

            Func<ExecutorStat, IComparable> keySelector;
            switch (_sortColumn)
            {
                case "FullName":
                    keySelector = s => s.FullName;
                    break;
                case "Profession":
                    keySelector = s => s.Profession;
                    break;
                case "TotalRequests":
                    keySelector = s => s.TotalRequests;
                    break;
                case "AssignedRequests":
                    keySelector = s => s.AssignedRequests;
                    break;
                case "InProgressRequests":
                    keySelector = s => s.InProgressRequests;
                    break;
                case "CompletedRequests":
                    keySelector = s => s.CompletedRequests;
                    break;
                case "CompletionPercent":
                    keySelector = s => double.Parse(s.CompletionPercent.TrimEnd('%'));
                    break;
                case "LoadPercent":
                    keySelector = s => double.Parse(s.LoadPercent.TrimEnd('%'));
                    break;
                default:
                    keySelector = s => s.FullName;
                    break;
            }

            if (_sortAscending)
                _allStats = _allStats.OrderBy(keySelector).ToList();
            else
                _allStats = _allStats.OrderByDescending(keySelector).ToList();
        }

        private void Header_Click(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            string column = tb.Tag.ToString();
            if (_sortColumn == column)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = column;
                _sortAscending = true;
            }
            SortStats();
            _currentPage = 1;
            ApplyFiltersAndPaging();
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
            ExportOptionsWindow exportWindow = new ExportOptionsWindow(_allStats);
            exportWindow.Owner = Window.GetWindow(this);
            exportWindow.ShowDialog();
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

        private void OpenUserProfile(object sender, MouseButtonEventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            int? userId = textBlock?.Tag as int?;
            if (!userId.HasValue) return;

            User user = ConnectObject.GetConnect().Users.Find(userId.Value);
            if (user == null) return;

            UserProfile profileWindow = new UserProfile(user);
            profileWindow.Owner = Window.GetWindow(this);
            profileWindow.ShowDialog();
        }
    }
}

