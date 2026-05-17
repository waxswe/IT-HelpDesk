using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Pages._General;
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

namespace IT_HelpDesk.Pages.Administrator
{
    /// <summary>
    /// Логика взаимодействия для AdministratorPage.xaml
    /// </summary>
    public partial class AdministratorPage : Page
    {
        private List<User> _allUsers;
        private List<User> _filteredUsers;
        private int _currentPage = 1;
        private const int PageSize = 5;
        private bool filterAdmin = false;
        private bool filterUser = false;
        private bool filterManager = false;
        private bool filterExecutor = false;
        private bool filterActive = false;
        private bool filterBlocked = false;
        private bool filterDeleted = false;

        public AdministratorPage()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                LoadUsers();
                LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
                if (loc != null) loc.LanguageChanged += (sender, args) => ApplyFilters();
            };
            TBSearch.TextChanged += (s, e) => ApplyFilters();
            FilterImg.MouseLeftButtonUp += (s, e) =>
            {
                TBSearch.Text = "";
                ApplyFilters();
            };
        }
        private void LoadUsers()
        {
            _allUsers = ConnectObject.GetConnect().Users.ToList();
            _filteredUsers = _allUsers.ToList();
            ApplyFilters();
        }

        private void UpdatePage()
        {
            List<User> pagedUsers = _filteredUsers.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            UsersItemsControl.ItemsSource = pagedUsers;
            int totalPages = (int)Math.Ceiling(_filteredUsers.Count / (double)PageSize);
            string pageFormat = GetLoc("Page_Info_Format");
            PageInfo.Text = string.Format(pageFormat, _currentPage, totalPages);
            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < totalPages;
            TBPageNumber.Text = Convert.ToString(_currentPage);
        }

        private void FilterImg_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FilterMenu.IsOpen = true;
        }

        private void FilterOption_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            switch (mi.Name)
            {
                case "FilterRoleAdmin": filterAdmin = mi.IsChecked; break;
                case "FilterRoleUser": filterUser = mi.IsChecked; break;
                case "FilterRoleManager": filterManager = mi.IsChecked; break;
                case "FilterRoleExecutor": filterExecutor = mi.IsChecked; break;
                case "FilterStatusActive": filterActive = mi.IsChecked; break;
                case "FilterStatusBlocked": filterBlocked = mi.IsChecked; break;
                case "FilterStatusDeleted": filterDeleted = mi.IsChecked; break;
            }
            ApplyFilters();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            filterAdmin = filterUser = filterManager = filterExecutor = false;
            filterActive = filterBlocked = filterDeleted = false;
            FilterRoleAdmin.IsChecked = false;
            FilterRoleUser.IsChecked = false;
            FilterRoleManager.IsChecked = false;
            FilterRoleExecutor.IsChecked = false;
            FilterStatusActive.IsChecked = false;
            FilterStatusBlocked.IsChecked = false;
            FilterStatusDeleted.IsChecked = false;
            TBSearch.Text = "";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<User> query = _allUsers.AsEnumerable();

            // Поиск по тексту
            string searchText = TBSearch.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(u =>
                    (u.name != null && u.name.ToLower().Contains(searchText.ToLower())) ||
                    (u.login != null && u.login.ToLower().Contains(searchText.ToLower())) ||
                    (u.email != null && u.email.ToLower().Contains(searchText.ToLower())));
            }

            // Фильтр по роли
            List<int> selectedRoles = new List<int>();
            if (filterAdmin) selectedRoles.Add(1);
            if (filterUser) selectedRoles.Add(2);
            if (filterManager) selectedRoles.Add(3);
            if (filterExecutor) selectedRoles.Add(4);
            if (selectedRoles.Any())
                query = query.Where(u => selectedRoles.Contains(u.roleID));

            // Фильтр по статусу
            List<int> selectedStatuses = new List<int>();
            if (filterActive) selectedStatuses.Add(1);
            if (filterBlocked) selectedStatuses.Add(2);
            if (filterDeleted) selectedStatuses.Add(3);
            if (selectedStatuses.Any())
                query = query.Where(u => selectedStatuses.Contains(u.statusID));

            _filteredUsers = query.ToList();
            _currentPage = 1;
            UpdatePage();
        }

        private void Search_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TBSearch.Focus();
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            UpdatePage();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling(_allUsers.Count / (double)PageSize);
            if (_currentPage >= totalPages) return;
            _currentPage++;
            UpdatePage();
        }

        private void GoToPageButton_Click(object sender, RoutedEventArgs e)
        {
            GoToPage();
        }

        private void GoToPage()
        {
            int totalPages = (int)Math.Ceiling(_allUsers.Count / (double)PageSize);
            if (int.TryParse(TBPageNumber.Text, out int targetPage) && targetPage >= 1 && targetPage <= totalPages)
            {
                _currentPage = targetPage;
                UpdatePage();
                TBPageNumber.Text = Convert.ToString(_currentPage);
            }
            else
            {
                MessageBox.Show(string.Format(GetLoc("InvalidPage_Message"), totalPages), GetLoc("InvalidPage_Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                TBPageNumber.Text = Convert.ToString(_currentPage);
            }
        }

        private void EyeIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock eye = sender as TextBlock;
            if (eye == null) return;
            StackPanel parentPanel = VisualTreeHelper.GetParent(eye) as StackPanel;
            if (parentPanel == null) return;
            TextBlock passwordTextBlock = parentPanel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "PasswordTextBlock");
            if (passwordTextBlock == null) return;
            User user = passwordTextBlock.DataContext as User;
            if (user != null && !string.IsNullOrEmpty(user.plainPassword))
            {
                passwordTextBlock.Text = user.plainPassword;
            }
        }

        private void EyeIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBlock eye = sender as TextBlock;
            if (eye == null) return;
            StackPanel parentPanel = VisualTreeHelper.GetParent(eye) as StackPanel;
            if (parentPanel == null) return;
            TextBlock passwordTextBlock = parentPanel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "PasswordTextBlock");
            if (passwordTextBlock != null)
            {
                passwordTextBlock.Text = "********";
            }
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }

        private void TBPageNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void TBPageNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (!((e.Key >= Key.D0 && e.Key <= Key.D9) ||
                  (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                  e.Key == Key.Back || e.Key == Key.Delete ||
                  e.Key == Key.Enter || e.Key == Key.Tab ||
                  e.Key == Key.Left || e.Key == Key.Right))
            {
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                GoToPage();
                e.Handled = true;
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.Navigate(new AddEditUser((sender as Button).DataContext as User));
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            User selectedUser = (sender as Button).DataContext as User;
            if (selectedUser == null) return;

            MessageBoxResult result = MessageBox.Show(string.Format(GetLoc("Confirm_Delete_Message"), selectedUser.login), GetLoc("Confirm_Delete_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                selectedUser.statusID = 3;
                selectedUser.name = "Удалённый аккаунт";
                selectedUser.avatarURL = "/Data/Images/avatar.jpg";
                ConnectObject.GetConnect().SaveChanges();
                ApplyFilters();
                MessageBox.Show(string.Format(GetLoc("Delete_User_Success_Message"), selectedUser.login), GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.Navigate(new AddEditUser(null));
        }

        private void MassMailingButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UsersItemsControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            User user = border?.DataContext as User;
            if (user != null)
            {
                UserProfile profileWindow = new UserProfile(user);
                profileWindow.ShowDialog();
            }
        }
    }
}
