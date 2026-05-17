using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Pages.Administrator;
using IT_HelpDesk.Pages.Client;
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

namespace IT_HelpDesk.Pages._General
{
    /// <summary>
    /// Логика взаимодействия для Authorization.xaml
    /// </summary>
    public partial class Authorization : Page
    {
        public Authorization()
        {
            InitializeComponent();

            Loaded += LoginWindow_Loaded;

            //AuthService.setPassword("1", "1");
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.RememberMe)
            {
                TBLogin.Text = Settings.Default.SavedLogin;
                RememberMeCheckBox.IsChecked = Settings.Default.RememberMe;
                TryAutoLogin();
            }
        }

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TBLogin.Text) || string.IsNullOrWhiteSpace(PBPassword.Password))
            {
                MessageBox.Show(GetLoc("Error_EmptyFields"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            User user = ConnectObject.GetConnect().Users.FirstOrDefault(u => u.login == TBLogin.Text);
            if (user == null)
            {
                MessageBox.Show(GetLoc("Error_NoLogin"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                TBLogin.Focus();
                return;
            }

            if (user.statusID == 2)
            {
                MessageBox.Show(GetLoc("Error_Blocked"), GetLoc("Error_BlockedTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (user.statusID == 3)
            {
                MessageBox.Show(GetLoc("Error_Deleted"), GetLoc("Error_DeletedTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (user.mistakeCount >= 3)
            {
                MessageBox.Show(GetLoc("Error_MistakeLimit"), GetLoc("Error_MistakeTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                user.statusID = 2;
                user.mistakeCount = 0;
                ConnectObject.GetConnect().SaveChanges();
                NotificationService.NotifyAllAdmins("Notification_UserBlocked_ToAdmin", null, user.userID, user.name);
                return;
            }

            string hashedInputPassword = AuthService.ComputeSha256Hash(PBPassword.Password);
            if (user.password == hashedInputPassword)
            {
                user.mistakeCount = 0;
                ConnectObject.GetConnect().SaveChanges();

                if (RememberMeCheckBox.IsChecked == true)
                {
                    Settings.Default.RememberMe = true;
                    Settings.Default.SavedLogin = TBLogin.Text;
                    Settings.Default.SavedPasswordHash = hashedInputPassword;
                    Settings.Default.Save();
                }
                else
                {
                    Settings.Default.RememberMe = false;
                    Settings.Default.SavedLogin = "";
                    Settings.Default.SavedPasswordHash = "";
                    Settings.Default.Save();
                }

                AuthService.CurrentUser = user;

                string welcomeMessage = "";
                switch (user.roleID)
                {
                    case 1:
                        welcomeMessage = string.Format(GetLoc("Welcome_Admin"), user.name);
                        MessageBox.Show(welcomeMessage, GetLoc("Welcome_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                        FrameObject.frameMain.Navigate(new AdministratorPage());
                        break;
                    case 2:
                        welcomeMessage = string.Format(GetLoc("Welcome_User"), user.name);
                        MessageBox.Show(welcomeMessage, GetLoc("Welcome_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                        FrameObject.frameMain.Navigate(new ClientPage());
                        break;
                    case 3:
                        welcomeMessage = string.Format(GetLoc("Welcome_Manager"), user.name);
                        MessageBox.Show(welcomeMessage, GetLoc("Welcome_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                        FrameObject.frameMain.Navigate(new Manager.ManagerPage());
                        break;
                    case 4:
                        welcomeMessage = string.Format(GetLoc("Welcome_Executor"), user.name);
                        MessageBox.Show(welcomeMessage, GetLoc("Welcome_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                        FrameObject.frameMain.Navigate(new Executor.ExecutorPage());
                        break;
                    default:
                        MessageBox.Show(GetLoc("Error_UnknownRole"), GetLoc("Error_UnknownTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                }
                return;
            }
            else
            {
                user.mistakeCount++;
                ConnectObject.GetConnect().SaveChanges();
                int? remainingAttempts = 3 - user.mistakeCount;
                if (remainingAttempts > 0)
                {
                    MessageBox.Show(string.Format(GetLoc("Error_WrongPassword"), remainingAttempts), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    PBPassword.Focus();
                }
                else
                    MessageBox.Show(GetLoc("Error_WrongPasswordFinal"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }


        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(GetLoc("Forget_Password_Messagebox"), GetLoc("Forget_Password_Messagebox_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EyeIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            DockPanel dockPanel = FindParent<DockPanel>((DependencyObject)sender);

            if (dockPanel != null)
            {
                dockPanel.Children.Remove(PBPassword);

                TextBox passwordReveal = new TextBox
                {
                    IsReadOnly = true,
                    Text = PBPassword.Password,
                    FontFamily = PBPassword.FontFamily,
                    FontSize = PBPassword.FontSize,
                    Background = PBPassword.Background,
                    BorderBrush = PBPassword.BorderBrush,
                    BorderThickness = PBPassword.BorderThickness,
                    VerticalContentAlignment = PBPassword.VerticalContentAlignment,
                    HorizontalContentAlignment = PBPassword.HorizontalContentAlignment
                };

                dockPanel.Children.Insert(0, passwordReveal);
            }
            if (dockPanel == null)
            {
                MessageBox.Show(GetLoc("Error_ParentNotFound"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EyeIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            DockPanel dockPanel = FindParent<DockPanel>((DependencyObject)sender);

            if (dockPanel != null)
            {
                TextBox textBox = dockPanel.Children.OfType<TextBox>().FirstOrDefault();

                if (textBox != null)
                {
                    dockPanel.Children.Remove(textBox);
                    dockPanel.Children.Insert(0, PBPassword);
                }
            }
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            T parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            LanguageButton.ContextMenu.IsOpen = true;
        }

        private void SetRussian_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager loc = (LocalizationManager)Application.Current.Resources["LocalizationManager"];
            loc.ChangeLanguage("ru");
        }

        private void SetEnglish_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager loc = (LocalizationManager)Application.Current.Resources["LocalizationManager"];
            loc.ChangeLanguage("en");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.RememberMe)
            {
                string savedLogin = Settings.Default.SavedLogin;
                string savedHash = Settings.Default.SavedPasswordHash;
                if (!string.IsNullOrEmpty(savedLogin) && !string.IsNullOrEmpty(savedHash))
                {
                    TBLogin.Text = savedLogin;
                }
            }
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }

        private bool TryAutoLogin()
        {
            Settings settings = Settings.Default;
            if (settings.RememberMe && !string.IsNullOrEmpty(settings.SavedLogin) && !string.IsNullOrEmpty(settings.SavedPasswordHash))
            {
                string login = settings.SavedLogin;
                string savedHash = settings.SavedPasswordHash;

                User user = ConnectObject.GetConnect().Users.FirstOrDefault(u => u.login == login && u.password == savedHash && u.statusID == 1);
                if (user != null)
                {
                    AuthService.CurrentUser = user;
                    LoadPageForRole(user.roleID);
                    return true;
                }
            }
            return false;
        }

        private void LoadPageForRole(int roleID)
        {
            switch (roleID)
            {
                case 1:
                    FrameObject.frameMain.Navigate(new AdministratorPage());
                    break;
                case 2:
                     FrameObject.frameMain.Navigate(new ClientPage());
                    break;
                case 3:
                    FrameObject.frameMain.Navigate(new Manager.ManagerPage());
                    break;
                case 4:
                    FrameObject.frameMain.Navigate(new Executor.ExecutorPage());
                    break;
                default:
                    FrameObject.frameMain.Navigate(new Authorization());
                    break;
            }
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) EnterButton_Click(null, null);
        }
    }
}
