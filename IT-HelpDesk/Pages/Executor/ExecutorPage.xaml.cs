using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Pages.Manager;
using System;
using System.Collections.Generic;
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

namespace IT_HelpDesk.Pages.Executor
{
    /// <summary>
    /// Логика взаимодействия для ExecutorPage.xaml
    /// </summary>
    public partial class ExecutorPage : Page
    {
        private List<dynamic> _allRequests;
        private List<dynamic> _filteredRequests;
        private int _currentPage = 1;
        private const int PageSize = 3;
        private string _searchText = "";
        private List<int> _selectedStatusIds = new List<int>();
        public ExecutorPage()
        {
            InitializeComponent();

            Loaded += async (s, e) => await LoadRequestsAsync();
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null) loc.LanguageChanged += (sender, args) => _ = LoadRequestsAsync();

            TBSearch.TextChanged += (s, e) => ApplyFilters();
        }

        private async Task LoadRequestsAsync()
        {
            User currentUser = AuthService.CurrentUser;
            if (currentUser == null) return;

            IOrderedQueryable<Request> query = ConnectObject.GetConnect().Requests.Where(r => r.workerID == currentUser.userID && r.requestStatusID < 5).OrderBy(r => r.requestID); 

            List<Request> cases = query.ToList();

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;

            var requests = cases.Select(r => new
            {
                RequestNumber = $"#{r.requestID}",
                Category = loc?.GetCaseCategoryTranslation(r.requestCategoryID) ?? "—",
                Title = r.title,
                Description = r.description,
                StatusName = loc?.GetCaseStatusTranslation(r.requestStatusID) ?? "—",
                CreatedAt = r.createdAt?.ToString("dd.MM.yyyy, HH:mm") ?? "",
                LastResponse = GetLastResponseDate(r),
                RequestStatusID = r.requestStatusID,
                RequestID = r.requestID,
                RequestObject = r,
                SectionID = r.RequestCategory?.requestSectionID ?? 0,
                ClientName = r.User?.name ?? ""
            }).ToList();

            _allRequests = requests.ToList<dynamic>();
            UpdateCounters();
            _currentPage = 1;
            ApplyFilters();
        }

        private void UpdateCounters()
        {
            if (_allRequests == null) return;
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            int total = _allRequests.Count;
            int assigned = _allRequests.Count(r => r.RequestStatusID == 2);
            int inWork = _allRequests.Count(r => r.RequestStatusID == 3);
            int waiting = _allRequests.Count(r => r.RequestStatusID == 4);

            TotalCountText.Text = string.Format(loc?["Counters_Total"] ?? "Total: {0}", total);
            AssignedCountText.Text = string.Format(loc?["Counters_Status2"] ?? "Assigned: {0}", assigned);
            InWorkCountText.Text = string.Format(loc?["Counters_Status3"] ?? "In work: {0}", inWork);
            WaitingCountText.Text = string.Format(loc?["Counters_Status4"] ?? "Waiting: {0}", waiting);
        }

        private string GetLastResponseDate(Request requestItem)
        {
            // Последний комментарий 
            Nullable<DateTime> lastComment = ConnectObject.GetConnect().Comments.Where(c => c.requestID == requestItem.requestID).OrderByDescending(c => c.createdAt).Select(c => c.createdAt).FirstOrDefault();

            if (lastComment != null) return lastComment.Value.ToString("dd.MM.yyyy, HH:mm");

            // Если нет комментариев, но есть updatedAt
            if (requestItem.updatedAt != null) return requestItem.updatedAt.Value.ToString("dd.MM.yyyy, HH:mm");

            // Иначе дата создания
            return requestItem.createdAt?.ToString("dd.MM.yyyy, HH:mm") ?? "";
        }

        private void ApplyFilters()
        {
            if (_allRequests == null) return;
            IEnumerable<dynamic> query = _allRequests;

            // Поиск
            _searchText = TBSearch.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(_searchText))
            {
                string lowerSearch = _searchText.ToLower();
                query = query.Where(r => r.RequestNumber.ToLower().Contains(lowerSearch) || (r.Title?.ToLower().Contains(lowerSearch) ?? false) || (r.Description?.ToLower().Contains(lowerSearch) ?? false) || (r.ClientName?.ToLower().Contains(lowerSearch) ?? false));
            }

            // Фильтр по статусам
            if (_selectedStatusIds.Any())
            {
                query = query.Where(r => _selectedStatusIds.Contains(r.RequestStatusID));
            }

            _filteredRequests = query.ToList();
            _currentPage = 1;
            UpdatePage();
        }

        private void UpdatePage()
        {
            List<dynamic> source = _filteredRequests ?? _allRequests ?? new List<dynamic>();
            bool hasRequests = source.Count > 0;

            RequestsItemsControl.Visibility = hasRequests ? Visibility.Visible : Visibility.Collapsed;
            NoRequestsTextBlock.Visibility = hasRequests ? Visibility.Collapsed : Visibility.Visible;
            PaginationPanel.Visibility = hasRequests ? Visibility.Visible : Visibility.Collapsed;

            if (!hasRequests) return;

            dynamic paged = source.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            RequestsItemsControl.ItemsSource = paged;
            int totalPages = (int)Math.Ceiling(source.Count / (double)PageSize);
            PageInfo.Text = string.Format(GetLoc("Page_Info_Format"), _currentPage, totalPages);
            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < totalPages;
            TBPageNumber.Text = _currentPage.ToString();
        }

        private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            Request request = btn?.Tag as Request;
            if (request == null) return;

            string confirmMessage = string.Format(GetLoc("CompleteConfirm_Message"), request.requestID);
            if (MessageBox.Show(confirmMessage, GetLoc("CompleteConfirm_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Request req = ConnectObject.GetConnect().Requests.Find(request.requestID);
            if (req != null && req.requestStatusID != 5)
            {
                req.requestStatusID = 5; 
                req.updatedAt = DateTime.Now;
                req.updatedBy = AuthService.CurrentUser.userID;
                await ConnectObject.GetConnect().SaveChangesAsync();

                if (request != null)
                {
                    // Менеджерам
                    List<User> managers = ConnectObject.GetConnect().Users.Where(u => u.roleID == 3 && u.statusID == 1).ToList();
                    foreach (User manager in managers)
                        NotificationService.Create(manager.userID, "Notification_Completed_ToManager", request.requestID, AuthService.CurrentUser.userID);
                    // Клиенту
                    if (request.clientID != AuthService.CurrentUser.userID)
                        NotificationService.Create(request.clientID, "Notification_Completed_ToClient", request.requestID, AuthService.CurrentUser.userID);

                    NotificationService.Create(userId: AuthService.CurrentUser.userID, templateKey: "Success_Request_Completed", requestId: request.requestID);
                    CommentHelper.AddSystemComment(request.requestID, "Completed");
                }

                await LoadRequestsAsync(); 
            }
        }

        private async void Request_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            dynamic request = border?.DataContext;
            if (request == null) return;

            Request selectedRequest = request.RequestObject;

            if (selectedRequest.requestStatusID == 2)
            {
                selectedRequest.requestStatusID = 3;
                selectedRequest.updatedAt = DateTime.Now;
                selectedRequest.updatedBy = AuthService.CurrentUser.userID;
                await ConnectObject.GetConnect().SaveChangesAsync();
            }

            FrameObject.frameMain.Navigate(new EditRequestStatus(selectedRequest));
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            UpdatePage();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((_filteredRequests?.Count ?? _allRequests?.Count ?? 0) / (double)PageSize);
            if (_currentPage >= totalPages) return;
            _currentPage++;
            UpdatePage();
        }

        private void GoToPageButton_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((_filteredRequests?.Count ?? _allRequests?.Count ?? 0) / (double)PageSize);
            if (int.TryParse(TBPageNumber.Text, out int target) && target >= 1 && target <= totalPages)
            {
                _currentPage = target;
                UpdatePage();
                TBPageNumber.Text = _currentPage.ToString();
            }
            else
            {
                MessageBox.Show(string.Format(GetLoc("InvalidPage_Message"), totalPages), GetLoc("InvalidPage_Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                TBPageNumber.Text = _currentPage.ToString();
            }
        }

        private void TBPageNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) GoToPageButton_Click(null, null);
        }

        private void TBPageNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void SearchIcon_Click(object sender, MouseButtonEventArgs e) => TBSearch.Focus();

        private void FilterImg_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => FilterMenu.IsOpen = true;

        private void FilterOption_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi.Name == "FilterStatusInWork") UpdateStatusFilter(3, mi.IsChecked);
            else if (mi.Name == "FilterStatusWaiting") UpdateStatusFilter(4, mi.IsChecked);
            ApplyFilters();
        }

        private void UpdateStatusFilter(int statusId, bool isChecked)
        {
            if (isChecked && !_selectedStatusIds.Contains(statusId))
                _selectedStatusIds.Add(statusId);
            else if (!isChecked && _selectedStatusIds.Contains(statusId))
                _selectedStatusIds.Remove(statusId);
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            _selectedStatusIds.Clear();
            FilterStatusInWork.IsChecked = false;
            FilterStatusWaiting.IsChecked = false;
            TBSearch.Text = "";
            ApplyFilters();
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }
    }
}
