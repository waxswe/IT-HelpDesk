using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для AddEditUser.xaml
    /// </summary>
    public partial class AddEditUser : Page
    {
        private User _tempUser = new User();
        bool isNewUser = true;
        private List<dynamic> _professions;
        const int maxNameLength = 64;
        const int maxEmailLength = 64;
        public AddEditUser(User selectedUser)
        {
            InitializeComponent();

            if (selectedUser != null)
            {
                _tempUser = selectedUser;
                isNewUser = false;
                PBPassword.Password = _tempUser.plainPassword ?? "";
            }
            else
            {
                isNewUser = true;
                _tempUser.roleID = 2;
                _tempUser.statusID = 1;
            }

            DataContext = _tempUser;
            LoadRolesAndStatuses();
            LoadProfessions();
            UpdateProfessionVisibility();

            CBRole.SelectionChanged += (s, e) => UpdateProfessionVisibility();

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null)
            {
                loc.LanguageChanged += (s, e) =>
                {
                    LoadRolesAndStatuses();
                    LoadProfessions();
                    CBRole.GetBindingExpression(ComboBox.SelectedValueProperty)?.UpdateTarget();
                    CBStatus.GetBindingExpression(ComboBox.SelectedValueProperty)?.UpdateTarget();
                    if (ProfessionPanel.Visibility == Visibility.Visible) CBProfession.GetBindingExpression(ComboBox.SelectedValueProperty)?.UpdateTarget();
                };
            }
        }

        private void LoadRolesAndStatuses()
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;

            // Роли
            List<Role> roles = ConnectObject.GetConnect().Roles.OrderBy(r => r.roleID).ToList();
            var roleItems = roles.Select(r => new
            {
                RoleID = r.roleID,
                LocalizedName = loc?.GetRoleTranslation(r.roleID) ?? r.role1
            }).ToList();
            CBRole.ItemsSource = roleItems;

            // Статусы
            List<Status> statuses = ConnectObject.GetConnect().Statuses.OrderBy(s => s.statusID).ToList();
            var statusItems = statuses.Select(s => new
            {
                StatusID = s.statusID,
                LocalizedName = loc?.GetStatusTranslation(s.statusID) ?? s.status1
            }).ToList();
            CBStatus.ItemsSource = statusItems;

            // Обновляем заголовок и кнопку
            AddUserLabel.Content = (isNewUser ? GetLoc("Add_User") : GetLoc("Edit_User"));
            SaveChangesButton.Content = (isNewUser ? GetLoc("Add_Button") : GetLoc("Save_Button"));
        }

        private void LoadProfessions()
        {
            // Сохраняем текущее выбранное значение
            int? selectedProfessionId = CBProfession.SelectedValue as int?;

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            List<Profession> professions = ConnectObject.GetConnect().Professions.OrderBy(p => p.professionID).ToList();
            _professions = professions.Select(p => new
            {
                ProfessionID = p.professionID,
                LocalizedName = loc?.GetProfessionTranslation(p.professionID) ?? p.profession1
            }).ToList<dynamic>();
            CBProfession.ItemsSource = _professions;

            // Восстанавливаем выбранное значение
            if (selectedProfessionId.HasValue)
                CBProfession.SelectedValue = selectedProfessionId.Value;
        }

        private void UpdateProfessionVisibility()
        {
            bool isExecutor = (CBRole.SelectedValue as int?) == 4;
            ProfessionPanel.Visibility = isExecutor ? Visibility.Visible : Visibility.Collapsed;
            ProfessionPanelLabel.Visibility = isExecutor ? Visibility.Visible : Visibility.Collapsed;
            if (ProfessionStar != null) ProfessionStar.Visibility = isExecutor ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            string login = TBLogin.Text?.Trim();
            if (string.IsNullOrWhiteSpace(login) || !IsValidLogin(login))
            {
                MessageBox.Show(GetLoc("Error_InvalidLogin"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                TBLogin.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TBFullname.Text))
            {
                MessageBox.Show(GetLoc("Error_EmptyFields"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                TBFullname.Focus();
                return;
            }

            string password = PBPassword.Password;
            if (isNewUser && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(GetLoc("Error_EmptyFields"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                PBPassword.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(password) || !IsValidPassword(password))
            {
                MessageBox.Show(GetLoc("Error_InvalidPassword"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                PBPassword.Focus();
                return;
            }

            string email = TBEmail.Text?.Trim();
            if (!IsValidEmail(email))
            {
                MessageBox.Show(GetLoc("Error_InvalidEmail"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                TBEmail.Focus();
                return;
            }

            if (TBFullname.Text.Length > maxNameLength)
            {
                MessageBox.Show(string.Format(GetLoc("Error_NameTooLong"), maxNameLength), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                TBFullname.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(email) && email.Length > maxEmailLength)
            {
                MessageBox.Show(string.Format(GetLoc("Error_EmailTooLong"), maxEmailLength), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                TBEmail.Focus();
                return;
            }

            if (CBRole.SelectedValue == null || CBStatus.SelectedValue == null)
            {
                MessageBox.Show(GetLoc("Error_EmptyFields"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _tempUser.mistakeCount = 0;
            _tempUser.email = email;
            

            if ((int)CBRole.SelectedValue == 4)
            {
                if (CBProfession.SelectedValue == null)
                {
                    MessageBox.Show(GetLoc("Error_Select_Profession"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _tempUser.professionID = (int)CBProfession.SelectedValue;
            }
            else
            {
                _tempUser.professionID = null;
            }

            // Добавление нового пользователя в модель данных
            if (_tempUser.userID == 0)
            {
                _tempUser.plainPassword = PBPassword.Password;
                _tempUser.password = AuthService.ComputeSha256Hash(PBPassword.Password);
                _tempUser.createdAt = DateTime.Now;
                _tempUser.isNew = true;
                _tempUser.avatarURL = "/Data/Images/avatar.jpg";

                if (ConnectObject.connect.Users.Count(x => x.login == TBLogin.Text) > 0)
                {
                    MessageBox.Show(GetLoc("Error_LoginExists"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    TBLogin.Text = string.Empty;
                    TBLogin.Focus();
                    return;
                }
                ConnectObject.GetConnect().Users.Add(_tempUser);
            }
            // сохранение изменений в модели данных
            try
            {
                ConnectObject.GetConnect().SaveChanges();

                // Уведомление пользователю об изменении данных
                if (!isNewUser && _tempUser.userID != AuthService.CurrentUser.userID)
                {
                    string adminName = AuthService.CurrentUser.name;
                    LocalizationManager loc = App.Current.Resources["LocalizationManager"] as LocalizationManager;
                    if (loc?.CurrentLanguage == "en")
                        adminName = loc.Transliterate(adminName);

                    NotificationService.Create(_tempUser.userID, "Notification_UserDataChanged", initiatorId: AuthService.CurrentUser.userID, formatArgs: AuthService.CurrentUser.name);
                }

                MessageBox.Show(isNewUser ? GetLoc("User_Added") : GetLoc("User_Updated"), GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            FrameObject.frameMain.GoBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.GoBack();
        }

        private void EyeIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            TBPasswordReveal.Text = PBPassword.Password;
            PBPassword.Visibility = Visibility.Collapsed;
            TBPasswordReveal.Visibility = Visibility.Visible;
        }

        private void EyeIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            PBPassword.Visibility = Visibility.Visible;
            TBPasswordReveal.Visibility = Visibility.Collapsed;
        }

        private bool IsValidLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) return false;
            return Regex.IsMatch(login, @"^[a-zA-Z0-9_]{3,16}$");
        }

        private bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length >= 6;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }
    }
}
