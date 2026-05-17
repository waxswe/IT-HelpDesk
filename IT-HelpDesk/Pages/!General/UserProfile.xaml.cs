using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Pages.Administrator;
using Microsoft.Win32;
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
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Effects;

namespace IT_HelpDesk.Pages._General
{
    /// <summary>
    /// Логика взаимодействия для UserProfile.xaml
    /// </summary>
    public partial class UserProfile : Window
    {
        private User _displayUser;
        private bool _isOwnProfile;
        public UserProfile(User user = null)
        {
            InitializeComponent();

            Owner = Application.Current.MainWindow;
            _displayUser = user ?? AuthService.CurrentUser;
            _isOwnProfile = (_displayUser?.userID == AuthService.CurrentUser?.userID);
             if (_displayUser != AuthService.CurrentUser) AvatarEllipse.Cursor = Cursors.Arrow;
            Loaded += (s, e) => LoadUserData();

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null)
                loc.LanguageChanged += (s, e) => LoadUserData();

            this.Title = $"Профиль пользователя {user.login}";
        }

        private void LoadUserData()
        {
            if (_displayUser == null) return;

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            // ФИО
            if (loc != null && loc.CurrentLanguage == "en") TBFullName.Text = loc.Transliterate(_displayUser.name);
            else TBFullName.Text = _displayUser.name;

            // Роль
            if (loc != null)
            {
                if (_displayUser.roleID == 4 && _displayUser.professionID.HasValue)
                {
                    string profession = loc.GetProfessionTranslation(_displayUser.professionID.Value);
                    RoleText.Text = string.Format(loc["Executor_With_Profession"], profession);
                }
                else
                {
                    RoleText.Text = loc.GetRoleTranslation(_displayUser.roleID);
                }
            }
            else
            {
                RoleText.Text = "Неизвестная роль: roleID(" + _displayUser.roleID.ToString() + ")";
            }

            // Почта
            EmailText.Text = _displayUser.email ?? (loc?["Not_Specified"] ?? "не указан");

            // Телефон
            PhoneText.Text = _displayUser.phone ?? "-";

            // Аватар
            if (!string.IsNullOrEmpty(_displayUser.avatarURL))
            {
                string fullPath = System.IO.Path.Combine(GetAvatarDirectory(), _displayUser.avatarURL);
                if (File.Exists(fullPath)) AvatarBrush.ImageSource = LoadImage(fullPath);
                else AvatarBrush.ImageSource = AvatarHelper.GetDefaultAvatar();
            }
            else AvatarBrush.ImageSource = AvatarHelper.GetDefaultAvatar();

            // Видимость кнопок только для своего профиля
            EditProfileButton.Visibility = _isOwnProfile ? Visibility.Visible : Visibility.Collapsed;
            LogoutButton.Visibility = _isOwnProfile ? Visibility.Visible : Visibility.Collapsed;

            // администратор может удалить чужой аватар
            bool isAdmin = AuthService.CurrentUser.roleID == 1;
            DeleteAvatarButton.Visibility = (isAdmin && !_isOwnProfile && _displayUser.avatarURL != "/Data/Images/avatar.jpg") ? Visibility.Visible : Visibility.Collapsed;

            UpdateStatistics();
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewEmailPanel.Visibility = Visibility.Collapsed;
            ViewPhonePanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Visible;
            EditEmailBox.Text = _displayUser.email ?? "";
            EditPhoneBox.Text = _displayUser.phone ?? "-";
            EditProfileButton.Visibility = Visibility.Collapsed;
            DeleteAvatarButton.Visibility = _isOwnProfile ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AuthService.Logout();
            Close();
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.MainFrame.Navigate(new Authorization());
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            string newEmail = EditEmailBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newEmail))
                newEmail = null;

            string newPhone = EditPhoneBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newPhone))
                newPhone = "-";

            User user = ConnectObject.GetConnect().Users.Find(_displayUser.userID);
            if (user != null)
            {
                user.email = newEmail;
                user.phone = newPhone;
                ConnectObject.GetConnect().SaveChanges();
            }

            _displayUser.email = newEmail;
            _displayUser.phone = newPhone;
            if (_isOwnProfile)
            {
                AuthService.CurrentUser.email = newEmail;
                AuthService.CurrentUser.phone = newPhone;
            }

            EmailText.Text = newEmail ?? (GetLoc("Not_Specified") ?? "не указан");
            PhoneText.Text = newPhone;

            ExitEditMode();
        }


        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
        }

        private void ExitEditMode()
        {
            ViewEmailPanel.Visibility = Visibility.Visible;
            ViewPhonePanel.Visibility = Visibility.Visible;
            EditPanel.Visibility = Visibility.Collapsed;
            EditProfileButton.Visibility = Visibility.Visible;
            DeleteAvatarButton.Visibility = Visibility.Collapsed;
        }

        private void ChangeAvatar_Click(object sender, MouseButtonEventArgs e)
        {
            if (_displayUser != AuthService.CurrentUser) return;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";
            if (dialog.ShowDialog() != true) return;

            string sourceFile = dialog.FileName;
            string ext = System.IO.Path.GetExtension(sourceFile);
            string avatarDir = GetAvatarDirectory();
            string fileName = $"{_displayUser.login}{ext}";
            string destFile = System.IO.Path.Combine(avatarDir, fileName);

            File.Copy(sourceFile, destFile, true);

            string oldName = System.IO.Path.GetFileName(_displayUser.avatarURL);
            if (!string.IsNullOrEmpty(oldName) && oldName != fileName)
            {
                string oldPath = System.IO.Path.Combine(avatarDir, oldName);
                if (File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
           
                User user = ConnectObject.GetConnect().Users.Find(_displayUser.userID);
                if (user != null)
                {
                    user.avatarURL = fileName;
                    ConnectObject.GetConnect().SaveChanges();
                }
            
            _displayUser.avatarURL = fileName;
            if (_isOwnProfile) AuthService.CurrentUser.avatarURL = fileName;

            ReloadAvatar();
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.HeaderUserControl?.RefreshAvatar();
        }

        private string GetAvatarDirectory()
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IT-HelpDesk", "Avatars");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        private void ReloadAvatar()
        {
            string fileName = _displayUser.avatarURL;
            if (string.IsNullOrEmpty(fileName))
            {
                AvatarBrush.ImageSource = AvatarHelper.GetDefaultAvatar();
                return;
            }
            string fullPath = System.IO.Path.Combine(GetAvatarDirectory(), fileName);
            if (File.Exists(fullPath))
                AvatarBrush.ImageSource = LoadImage(fullPath);
            else
                AvatarBrush.ImageSource = AvatarHelper.GetDefaultAvatar();

            DeleteAvatarButton.Visibility = _isOwnProfile ? Visibility.Visible : Visibility.Collapsed;
        }

        private BitmapImage LoadImage(string path)
        {
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private void UpdateStatistics()
        {
            if (_displayUser == null) return;

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            string statText = "";

            switch (_displayUser.roleID)
            {
                case 2: // Клиент
                    int totalRequests = ConnectObject.GetConnect().Requests.Count(r => r.clientID == _displayUser.userID);
                    int activeRequests = ConnectObject.GetConnect().Requests.Count(r => r.clientID == _displayUser.userID && r.requestStatusID >= 1 && r.requestStatusID <= 4);
                    statText = string.Format(loc?["Statistics_Client"] ?? "Всего заявок: {0}\nАктивных заявок: {1}", totalRequests, activeRequests);
                    break;

                case 4: // Исполнитель
                    int totalAssigned = ConnectObject.GetConnect().Requests.Count(r => r.workerID == _displayUser.userID);
                    int completed = ConnectObject.GetConnect().Requests.Count(r => r.workerID == _displayUser.userID && r.requestStatusID == 5);
                    int activeForExecutor = ConnectObject.GetConnect().Requests.Count(r => r.workerID == _displayUser.userID && r.requestStatusID >= 2 && r.requestStatusID <= 4);
                    statText = string.Format(loc?["Statistics_Executor"] ?? "Всего заявок: {0}\nВыполнено: {1}\nАктивных: {2}", totalAssigned, completed, activeForExecutor);
                    break;

                default: // Администратор и другие
                    statText = loc?["Statistics_Placeholder"] ?? "Статистика не определена";
                    break;
            }

            StatInfo.Text = statText;
        }

        private void DeleteAvatar_Click(object sender, RoutedEventArgs e)
        {
            string fileName = _displayUser.avatarURL;
            if (!string.IsNullOrEmpty(fileName) && fileName != "/Data/Images/avatar.jpg" && !fileName.Contains("avatar.jpg"))
            {
                string fullPath = System.IO.Path.Combine(GetAvatarDirectory(), fileName);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            string defaultPath = "/Data/Images/avatar.jpg";

            User user = ConnectObject.GetConnect().Users.Find(_displayUser.userID);
            if (user != null)
            {
                user.avatarURL = defaultPath;
                ConnectObject.GetConnect().SaveChanges();
            }

            if (!_isOwnProfile && AuthService.CurrentUser.roleID == 1)
            {
                string adminName = AuthService.CurrentUser.name;
                LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
                if (loc?.CurrentLanguage == "en")
                    adminName = loc.Transliterate(adminName);

                NotificationService.Create(userId: _displayUser.userID, templateKey: "Notification_AvatarDeleted_ToUser", requestId: null, initiatorId: AuthService.CurrentUser.userID);
            }

            _displayUser.avatarURL = defaultPath;
            if (_isOwnProfile)
                AuthService.CurrentUser.avatarURL = defaultPath;

            ReloadAvatar();

            if (_isOwnProfile)
            {
                (Application.Current.MainWindow as MainWindow)?.HeaderUserControl?.RefreshAvatar();
                ExitEditMode();
            }
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }
    }
}
