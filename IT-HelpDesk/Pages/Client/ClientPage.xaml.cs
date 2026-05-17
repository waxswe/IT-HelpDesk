using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Pages._General;
using IT_HelpDesk.Pages.Administrator;
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

namespace IT_HelpDesk.Pages.Client
{
    /// <summary>
    /// Логика взаимодействия для ClientPage.xaml
    /// </summary>
    public partial class ClientPage : Page
    {
        private List<dynamic> _allRequests;
        private int _currentPage = 1;
        private const int PageSize = 6;
        public ClientPage()
        {
            InitializeComponent();

            Loaded += (s, e) => LoadRequests();
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null)
                loc.LanguageChanged += (sender, args) => LoadRequests();
        }

        private void LoadRequests()
        {
            if (AuthService.CurrentUser == null) return;

            List<Request> cases = ConnectObject.GetConnect().Requests.Where(c => c.clientID == AuthService.CurrentUser.userID).OrderByDescending(c => c.requestID).ToList();

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;

            var requests = cases.Select(c => new
            {
                RequestNumber = $"#{c.requestID}",
                Category = loc?.GetCaseCategoryTranslation(c.requestCategoryID) ?? "—",
                Title = c.title,
                Description = c.description,
                StatusName = loc?.GetCaseStatusTranslation(c.requestStatusID) ?? "—",
                CreatedAt = c.createdAt?.ToString("dd.MM.yyyy, HH:mm") ?? "",
                LastResponse = GetLastResponseDate(c),
                ActionButtonText = GetActionButtonText(c.requestStatusID),
                RequestStatusID = c.requestStatusID,
                RequestID = c.requestID,
                RequestObject = c,
                ActionButtonVisibility = (c.requestStatusID >= 1 && c.requestStatusID <= 4) ? Visibility.Visible : Visibility.Hidden
            }).ToList();

            _allRequests = requests.ToList<dynamic>();
            _currentPage = 1;
            UpdatePage();
        }

        private void UpdatePage()
        {
            if (_allRequests == null) return;
            dynamic paged = _allRequests.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            RequestsItemsControl.ItemsSource = paged;
            int totalPages = (int)Math.Ceiling(_allRequests.Count / (double)PageSize);
            PageInfo.Text = $"Страница {_currentPage}/{totalPages}";
            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < totalPages;
        }

        private string GetLastResponseDate(Request requestItem)
        {
            Nullable<DateTime> lastComment = ConnectObject.GetConnect().Comments.Where(c => c.requestID == requestItem.requestID).OrderByDescending(c => c.createdAt).Select(c => c.createdAt).FirstOrDefault();
            if (lastComment != null) return lastComment.Value.ToString("dd.MM.yyyy, HH:mm");

            if (requestItem.updatedAt != null) return requestItem.updatedAt.Value.ToString("dd.MM.yyyy, HH:mm");
            return requestItem.createdAt?.ToString("dd.MM.yyyy, HH:mm") ?? "";
        }

        private string GetActionButtonText(int requestStatusID)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return requestStatusID == 1 ? (loc?["Cancel_Button"] ?? "Отменить") : (loc?["Close_Button"] ?? "Закрыть");
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            dynamic request = button?.Tag;
            if (request == null) return;

            int requestID = request.RequestID;
            int oldStatusID = request.RequestStatusID;

            Request caseItem = ConnectObject.GetConnect().Requests.Find(requestID);
            if (caseItem == null) return;

            if (oldStatusID == 1)
                caseItem.requestStatusID = 6;   // Отменена
            else
                caseItem.requestStatusID = 7;   // Закрыта

            caseItem.updatedAt = DateTime.Now;
            caseItem.updatedBy = AuthService.CurrentUser.userID;
            ConnectObject.GetConnect().SaveChanges();

            string templateKey = (oldStatusID == 1) ? "Success_Request_Cancelled" : "Success_Request_Closed";
            NotificationService.Create(userId: AuthService.CurrentUser.userID, templateKey: templateKey, requestId: requestID);

            LoadRequests();
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            UpdatePage();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling(_allRequests.Count / (double)PageSize);
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

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }

        private void NewCaseButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.Navigate(new AddEditCase(null));
        }

        private void Case_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            dynamic request = border?.DataContext;
            if (request != null)
            {
                Request selectedCase = request.RequestObject;
                FrameObject.frameMain.Navigate(new AddEditCase(selectedCase));
            }
        }
    }
}

