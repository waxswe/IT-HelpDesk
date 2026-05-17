using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Pages;
using IT_HelpDesk.Pages._General;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
namespace IT_HelpDesk.Controls
{
    public partial class HeaderUserControl : UserControl
    {
        private List<dynamic> _notifications = new List<dynamic>();
        private int _skip = 0;
        private const int Take = 5;
        private bool _hasMore = false;
        public HeaderUserControl()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                RefreshDisplayName();
                RefreshAvatar();
                UpdateNotificationBadge();
            };

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null) loc.LanguageChanged += (s, e) => RefreshDisplayName();
        }

        // Обработчик кнопки "Уведомления"
        private async void NotificationsButton_Click(object sender, RoutedEventArgs e)
        {
            if (NotificationsPopup.IsOpen)
                NotificationsPopup.IsOpen = false;
            else
            {
                _skip = 0;
                await LoadNotifications();
                NotificationsPopup.IsOpen = true;
            }
        }

        private async Task LoadNotifications()
        {
            if (AuthService.CurrentUser == null) return;
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            List<NotificationItem> items = NotificationService.GetNotificationsPage(AuthService.CurrentUser.userID, _skip, Take, loc);
            _notifications = items.ToList<dynamic>();

            bool hasNotifications = _notifications.Any();
            NotificationsItemsControl.Visibility = hasNotifications ? Visibility.Visible : Visibility.Collapsed;
            NoNotificationsTextBlock.Visibility = hasNotifications ? Visibility.Collapsed : Visibility.Visible;

            NotificationsItemsControl.ItemsSource = _notifications;
            int total = NotificationService.GetTotalCount(AuthService.CurrentUser.userID);
            _hasMore = (_skip + Take) < total;
            LoadMoreButton.Visibility = hasNotifications && _hasMore ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (AuthService.CurrentUser == null) return;
            _skip += Take;
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            List<NotificationItem> newItems = NotificationService.GetNotificationsPage(AuthService.CurrentUser.userID, _skip, Take, loc);
            foreach (NotificationItem item in newItems)
                _notifications.Add(item);
            NotificationsItemsControl.ItemsSource = null;
            NotificationsItemsControl.ItemsSource = _notifications;
            int total = NotificationService.GetTotalCount(AuthService.CurrentUser.userID);
            _hasMore = (_skip + Take) < total;
            LoadMoreButton.Visibility = _hasMore ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NotificationItem_Click(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            dynamic item = border?.DataContext;
            if (item == null) return;

            int notifId = item.NotificationID;
            int? requestId = item.RequestID;
            string templateKey = item.TemplateKey;

            NotificationService.MarkAsRead(notifId);
            UpdateNotificationBadge();
            NotificationsPopup.IsOpen = false;

            if (!requestId.HasValue) return;

            Request request = ConnectObject.GetConnect().Requests.Find(requestId.Value);
            if (request == null) return;

            User currentUser = AuthService.CurrentUser;
            bool canView = false;

            if (currentUser.roleID == 1 || currentUser.roleID == 3)
            {
                canView = true;
            }
            else if (currentUser.roleID == 2)
            {
                canView = (request.clientID == currentUser.userID);
            }
            else if (currentUser.roleID == 4)
            {
                canView = (request.workerID == currentUser.userID);
            }

            if (!canView)
            {
                MessageBox.Show(GetLoc("Notification_NoAccess"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Открытие нужной страницы
            if (templateKey != null && templateKey.Contains("Comment"))
            {
                FrameObject.frameMain.Navigate(new RequestCommentaries(request));
            }
            else
            {
                if (currentUser.roleID == 2)
                    FrameObject.frameMain.Navigate(new Pages.Client.AddEditCase(request));
                else
                    FrameObject.frameMain.Navigate(new Pages.Manager.EditRequestStatus(request));
            }
        }

        private void MarkAllReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (AuthService.CurrentUser == null) return;
            NotificationService.MarkAllAsRead(AuthService.CurrentUser.userID);
            UpdateNotificationBadge();
            foreach (dynamic n in _notifications)
                n.IsRead = true;
            NotificationsItemsControl.ItemsSource = null;
            NotificationsItemsControl.ItemsSource = _notifications;
        }

        public void UpdateNotificationBadge()
        {
            if (AuthService.CurrentUser == null) return;
            int count = NotificationService.GetUnreadCount(AuthService.CurrentUser.userID);
            if (count > 0)
            {
                NotificationBadge.Visibility = Visibility.Visible;
                BadgeText.Text = count > 9 ? "9+" : count.ToString();
            }
            else
                NotificationBadge.Visibility = Visibility.Collapsed;
        }

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            // Берём текущий ресурс локализации
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc == null) return;

            string newLang = (loc.CurrentLanguage == "ru") ? "en" : "ru";
            loc.ChangeLanguage(newLang);
        }

        // Обработчик кнопки "Выйти"
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AuthService.Logout();
            FrameObject.frameMain.Navigate(new Authorization());
        }

        public void UpdateNotificationCount(int unreadCount)
        {
            if (unreadCount > 0)
            {
                NotificationBadge.Visibility = Visibility.Visible;
                BadgeText.Text = unreadCount > 9 ? "9+" : unreadCount.ToString();
            }
            else
            {
                NotificationBadge.Visibility = Visibility.Collapsed;
            }
        }

        public void RefreshDisplayName()
        {
            if (AuthService.CurrentUser != null && !string.IsNullOrEmpty(AuthService.CurrentUser.name))
            {
                LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
                if (loc != null && loc.CurrentLanguage == "en")
                    FullNameText.Text = loc.Transliterate(AuthService.CurrentUser.name);
                else
                    FullNameText.Text = AuthService.CurrentUser.name;
            }
            else
            {
                FullNameText.Text = GetLoc("Default_User");
            }
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }

        private void AvatarEllipse_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AuthService.CurrentUser == null) return;
            UserProfile profileWindow = new UserProfile(AuthService.CurrentUser);
            profileWindow.Closed += (s, args) => RefreshAvatar(); 
            profileWindow.ShowDialog();
        }

        public void RefreshAvatar()
        {
            string fileName = AuthService.CurrentUser?.avatarURL;
            string fullPath = null;
            if (!string.IsNullOrEmpty(fileName))
                fullPath = Path.Combine(GetAvatarDirectory(), fileName);
            if (fullPath != null && File.Exists(fullPath))
                AvatarBrush.ImageSource = LoadImage(fullPath);
            else
                AvatarBrush.ImageSource = LoadImage(GetDefaultAvatarPath());
        }

        private string GetAvatarDirectory()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IT-HelpDesk", "Avatars");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        private string GetDefaultAvatarPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", "avatar.jpg");
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
    }
}