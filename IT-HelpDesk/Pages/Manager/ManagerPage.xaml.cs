using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
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

namespace IT_HelpDesk.Pages.Manager
{
    /// <summary>
    /// Логика взаимодействия для ManagerPage.xaml
    /// </summary>
    public partial class ManagerPage : Page
    {
        private List<dynamic> _allRequests;
        private List<dynamic> _filteredRequests;
        private int _currentPage = 1;
        private const int PageSize = 4;
        private bool _isUpdatingComboBox = false;
        private string _searchText = "";
        private List<int> _selectedStatusIds = new List<int>();
        private List<int> _selectedSectionIds = new List<int>();
        private enum ViewMode { OnlyNew, Assigned, All }
        private ViewMode _currentMode = ViewMode.OnlyNew;
        public ManagerPage()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                LoadRequests();
            };
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null) loc.LanguageChanged += (sender, args) => LoadRequests();

            TBSearch.TextChanged += (s, e) => ApplyFilters();
        }

        private void LoadRequests()
        {
            IQueryable<Request> query = ConnectObject.GetConnect().Requests.Where(r => r.requestStatusID != 6);

            List<Request> cases = query.OrderByDescending(r => r.requestID).ToList();
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
            _currentPage = 1;
            ApplyFilters();
        }

        private void WorkerComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo == null) return;
            Request request = combo.Tag as Request;
            if (request == null) return;

            Grid parentGrid = combo.Parent as Grid;
            TextBlock disabledText = parentGrid?.FindName("DisabledTextBlock") as TextBlock;
            TextBlock placeholder = parentGrid?.FindName("WorkerPlaceholder") as TextBlock;

            bool canAssign = (request.requestStatusID >= 1 && request.requestStatusID <= 4);

            if (canAssign)
            {
                combo.Visibility = Visibility.Visible;
                if (disabledText != null) disabledText.Visibility = Visibility.Collapsed;

                LoadWorkersForComboBox(combo, request);
                combo.ToolTip = GetLoc("Select_Worker_Placeholder");

                if (placeholder != null)
                {
                    placeholder.Visibility = (combo.SelectedItem == null) ? Visibility.Visible : Visibility.Collapsed;
                    combo.SelectionChanged += (s, ev) =>
                    {
                        placeholder.Visibility = (combo.SelectedItem == null) ? Visibility.Visible : Visibility.Collapsed;
                    };
                }
            }
            else
            {
                combo.Visibility = Visibility.Hidden; 
                if (placeholder != null) placeholder.Visibility = Visibility.Hidden;
            }
        }

        private void ApplyFilters()
        {
            if (_allRequests == null) return;

            IEnumerable<dynamic> query = _allRequests;

            // фильтр по выбранному режиму
            if (_currentMode == ViewMode.OnlyNew)
                query = query.Where(r => r.RequestStatusID == 1);
            else if (_currentMode == ViewMode.Assigned)
                query = query.Where(r => r.RequestStatusID >= 2 && r.RequestStatusID <= 4);

            // Поиск по тексту
            _searchText = TBSearch.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(_searchText))
            {
                string lowerSearch = _searchText.ToLower();
                query = query.Where(r =>
                    r.RequestNumber.ToLower().Contains(lowerSearch) ||
                    (r.Title?.ToLower().Contains(lowerSearch) ?? false) ||
                    (r.Description?.ToLower().Contains(lowerSearch) ?? false) ||
                    (r.ClientName?.ToLower().Contains(lowerSearch) ?? false)
                );
            }

            // Фильтр по статусам
            if (_selectedStatusIds.Any())
            {
                query = query.Where(r => _selectedStatusIds.Contains(r.RequestStatusID));
            }

            // Фильтр по разделам
            if (_selectedSectionIds.Any())
            {
                query = query.Where(r => _selectedSectionIds.Contains(r.SectionID));
            }

            _filteredRequests = query.ToList();
            UpdateGlobalCounters();
            _currentPage = 1;
            UpdatePage();
        }

        private void FilterImg_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FilterMenu.IsOpen = true;
        }

        private void UpdateStatusFilter(int statusId, bool isChecked)
        {
            if (isChecked && !_selectedStatusIds.Contains(statusId))
                _selectedStatusIds.Add(statusId);
            else if (!isChecked && _selectedStatusIds.Contains(statusId))
                _selectedStatusIds.Remove(statusId);
        }

        private void UpdateSectionFilter(int sectionId, bool isChecked)
        {
            if (isChecked && !_selectedSectionIds.Contains(sectionId))
                _selectedSectionIds.Add(sectionId);
            else if (!isChecked && _selectedSectionIds.Contains(sectionId))
                _selectedSectionIds.Remove(sectionId);
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            // Сброс статусов
            _selectedStatusIds.Clear();
            FilterStatusNew.IsChecked = false;
            FilterStatusAssigned.IsChecked = false;
            FilterStatusWork.IsChecked = false;
            FilterStatusWaiting.IsChecked = false;
            FilterStatusCompleted.IsChecked = false;
            FilterStatusCancelled.IsChecked = false;
            FilterStatusClosed.IsChecked = false;
            // Сброс разделов
            _selectedSectionIds.Clear();
            FilterSectionFacility.IsChecked = false;
            FilterSectionIT.IsChecked = false;
            FilterSectionOther.IsChecked = false;
            // Сброс поиска
            TBSearch.Text = "";

            ApplyFilters();
        }


        private void SearchIcon_Click(object sender, MouseButtonEventArgs e)
        {
            TBSearch.Focus();
        }

        private void RadioMode_Checked(object sender, RoutedEventArgs e)
        {
            if (RadioOnlyNew.IsChecked == true)
                _currentMode = ViewMode.OnlyNew;
            else if (RadioAssigned.IsChecked == true)
                _currentMode = ViewMode.Assigned;
            else
                _currentMode = ViewMode.All;

            ResetFilters_Click(null, null);
            LoadRequests();
        }

        private string GetLastResponseDate(Request requestItem)
        {
            Nullable<DateTime> lastComment = ConnectObject.GetConnect().Comments.Where(c => c.requestID == requestItem.requestID).OrderByDescending(c => c.createdAt).Select(c => c.createdAt).FirstOrDefault();
            if (lastComment != null) return lastComment.Value.ToString("dd.MM.yyyy, HH:mm");
            if (requestItem.updatedAt != null) return requestItem.updatedAt.Value.ToString("dd.MM.yyyy, HH:mm");
            return requestItem.createdAt?.ToString("dd.MM.yyyy, HH:mm") ?? "";
        }

        private void UpdatePage()
        {
            if (RequestsItemsControl == null)
            {
                RequestsItemsControl = this.FindName("RequestsItemsControl") as ItemsControl;
            }
            if (NoRequestsTextBlock == null)
                NoRequestsTextBlock = this.FindName("NoRequestsTextBlock") as TextBlock;
            if (PaginationPanel == null)
                PaginationPanel = this.FindName("PaginationPanel") as StackPanel;
            if (RequestsItemsControl == null || NoRequestsTextBlock == null || PaginationPanel == null) return;

            List<dynamic> source = _filteredRequests ?? _allRequests ?? new List<dynamic>();
            bool hasRequests = source.Count > 0;

            RequestsItemsControl.Visibility = hasRequests ? Visibility.Visible : Visibility.Collapsed;
            NoRequestsTextBlock.Visibility = hasRequests ? Visibility.Collapsed : Visibility.Visible;
            PaginationPanel.Visibility = hasRequests ? Visibility.Visible : Visibility.Collapsed;

            if (!hasRequests) return;

            List<dynamic> paged = source.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            RequestsItemsControl.ItemsSource = paged;
            int totalPages = (int)Math.Ceiling(source.Count / (double)PageSize);
            PageInfo.Text = string.Format(GetLoc("Page_Info_Format"), _currentPage, totalPages);
            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < totalPages;
            TBPageNumber.Text = _currentPage.ToString();

            // Заполняем ComboBox
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (RequestsItemsControl == null) return;
                for (int i = 0; i < paged.Count; i++)
                {
                    dynamic item = paged[i];
                    FrameworkElement container = RequestsItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                    if (container != null)
                    {
                        ComboBox combo = FindVisualChild<ComboBox>(container, "WorkerComboBox");
                        if (combo != null)
                        {
                            combo.Tag = item.RequestObject;
                            LoadWorkersForComboBox(combo, item.RequestObject);
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void LoadWorkersForComboBox(ComboBox comboBox, Request request)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;

            List<User> workers = ConnectObject.GetConnect().Users.Where(u => u.roleID == 4 && u.statusID == 1).ToList();

            int sectionId = request.RequestCategory?.requestSectionID ?? 0;
            List<int> allowedProfessions = null;

            if (sectionId == 1) // Административно-хозяйственный
            {
                allowedProfessions = new List<int> { 1, 2, 3, 4 }; 
            }
            else if (sectionId == 2) // Компьютерная техника и ПО
            {
                allowedProfessions = new List<int> { 5, 6, 7, 8 }; 
            }

            if (allowedProfessions != null)
            {
                workers = workers.Where(w => w.professionID.HasValue && allowedProfessions.Contains(w.professionID.Value)).ToList();
            }

            var items = workers.Select(w => new
            {
                UserID = w.userID,
                LocalizedName = w.professionID.HasValue && w.professionID.Value != 0 ? $"{w.name} ({loc?.GetProfessionTranslation(w.professionID.Value) ?? "—"})" : w.name
            }).ToList();

            _isUpdatingComboBox = true;
            comboBox.ItemsSource = items;
            comboBox.DisplayMemberPath = "LocalizedName";
            comboBox.SelectedValuePath = "UserID";
            comboBox.SelectedValue = request.workerID;
            _isUpdatingComboBox = false;
        }

        private async void WorkerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (_isUpdatingComboBox) return;
            if (comboBox == null || comboBox.SelectedValue == null) return;

            Request request = comboBox.Tag as Request;
            if (request == null) return;

            int selectedWorkerId = (int)comboBox.SelectedValue;
            User selectedWorker = ConnectObject.GetConnect().Users.Find(selectedWorkerId);
            if (selectedWorker == null) return;

            string workerDisplay = $"{selectedWorker.name} ({GetProfessionTranslationForWorker(selectedWorker)})";
            string confirmMessage = string.Format(GetLoc("Confirm_Assign_Worker"), request.requestID, workerDisplay);

            MessageBoxResult result = MessageBox.Show(confirmMessage, GetLoc("Confirm_Assign_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                comboBox.SelectedValue = request.workerID;
                return;
            }

            Request req = ConnectObject.GetConnect().Requests.Find(request.requestID);
            if (req != null)
            {
                req.workerID = selectedWorkerId;
                req.requestStatusID = 2;
                req.updatedAt = DateTime.Now;
                req.updatedBy = AuthService.CurrentUser.userID;
                await ConnectObject.GetConnect().SaveChangesAsync();

                NotificationService.Create(selectedWorkerId, "Notification_Assigned_ToExecutor", requestId: request.requestID, initiatorId: AuthService.CurrentUser.userID, formatArgs: request.requestID);
                User client = ConnectObject.GetConnect().Users.Find(request.clientID);
                if (client != null)
                    NotificationService.Create(client.userID, "Notification_Assigned_ToClient", request.requestID, selectedWorker.userID, selectedWorker.name);

                LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
                if (loc?.CurrentLanguage == "en")
                    workerDisplay = loc.Transliterate(selectedWorker.name);
                CommentHelper.AddSystemComment(request.requestID, "Assigned", selectedWorker.name);

                LoadRequests();
            }
            else
            {
                MessageBox.Show(GetLoc("Error_RequestNotFound"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                LoadRequests();
            }
        }

        private string GetProfessionTranslationForWorker(User worker)
        {
            if (worker.professionID == null) return "";
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?.GetProfessionTranslation(worker.professionID.Value) ?? "";
        }

        private void Request_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DependencyObject source = e.OriginalSource as DependencyObject;
            TextBlock disabled = FindParent<TextBlock>(source);
            if (disabled != null && disabled.Name == "DisabledTextBlock")
            {
                e.Handled = true;
                return;
            }

            ComboBox combo = FindParent<ComboBox>(source);
            if (combo != null)
            {
                e.Handled = true;
                return; 
            }

            Border border = sender as Border;
            dynamic request = border?.DataContext;
            if (request != null)
            {
                FrameObject.frameMain.Navigate(new EditRequestStatus(request.RequestObject));
            }
        }

        private void UpdateGlobalCounters()
        {
            if (_allRequests == null) return;
            if (TotalCountText == null) return;
            List<dynamic> activeRequests = _allRequests.Where(r => r.RequestStatusID != 6).ToList();
            int total = activeRequests.Count;
            int newCount = activeRequests.Count(r => r.RequestStatusID == 1);
            int assignedCount = activeRequests.Count(r => r.RequestStatusID == 2);
            int inWorkCount = activeRequests.Count(r => r.RequestStatusID == 3);
            int waitingCount = activeRequests.Count(r => r.RequestStatusID == 4);
            int completedCount = activeRequests.Count(r => r.RequestStatusID == 5);
            int closedCount = activeRequests.Count(r => r.RequestStatusID == 7);

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            TotalCountText.Text = string.Format(loc?["Counters_Total_Manager"] ?? "Total: {0}", total);
            NewCountText.Text = string.Format(loc?["Counters_New"] ?? "New: {0}", newCount);
            AssignedCountText.Text = string.Format(loc?["Counters_Assigned"] ?? "Assigned: {0}", assignedCount);
            InWorkCountText.Text = string.Format(loc?["Counters_InWork"] ?? "In work: {0}", inWorkCount);
            WaitingCountText.Text = string.Format(loc?["Counters_Waiting"] ?? "Waiting: {0}", waitingCount);
            CompletedCountText.Text = string.Format(loc?["Counters_Completed"] ?? "Completed: {0}", completedCount);
            ClosedCountText.Text = string.Format(loc?["Counters_Closed"] ?? "Closed: {0}", closedCount);
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
            int totalPages = (int)Math.Ceiling(_allRequests.Count / (double)PageSize);
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
            if (e.Key == Key.Enter)
                GoToPageButton_Click(null, null);
        }

        private void TBPageNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private string GetCaseStatusTranslation(int statusId)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?.GetCaseStatusTranslation(statusId) ?? statusId.ToString();
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }


        private void FilterOption_Click(object sender, RoutedEventArgs e)
        {
            SwitchToAllMode();

            MenuItem mi = sender as MenuItem;
            // Статусы
            if (mi.Name == "FilterStatusNew") UpdateStatusFilter(1, mi.IsChecked);
            else if (mi.Name == "FilterStatusAssigned") UpdateStatusFilter(2, mi.IsChecked);
            else if (mi.Name == "FilterStatusWork") UpdateStatusFilter(3, mi.IsChecked);
            else if (mi.Name == "FilterStatusWaiting") UpdateStatusFilter(4, mi.IsChecked);
            else if (mi.Name == "FilterStatusCompleted") UpdateStatusFilter(5, mi.IsChecked);
            else if (mi.Name == "FilterStatusCancelled") UpdateStatusFilter(6, mi.IsChecked);
            else if (mi.Name == "FilterStatusClosed") UpdateStatusFilter(7, mi.IsChecked);
            // Разделы
            else if (mi.Name == "FilterSectionFacility") UpdateSectionFilter(1, mi.IsChecked);
            else if (mi.Name == "FilterSectionIT") UpdateSectionFilter(2, mi.IsChecked);
            else if (mi.Name == "FilterSectionOther") UpdateSectionFilter(3, mi.IsChecked);
            ApplyFilters();
        }

        private void SearchFilterPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void SearchFilterPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void SwitchToAllMode()
        {
            if (_currentMode != ViewMode.All)
            {
                _currentMode = ViewMode.All;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T && (child as FrameworkElement).Name == name) return child as T;
                T result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T t) return t;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void ExecutorStatsButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.Navigate(new ExecutorStatistics());
        }
    }
}
