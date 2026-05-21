using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

namespace IT_HelpDesk.Pages._General
{
    /// <summary>
    /// Логика взаимодействия для SetPasswordWindow.xaml
    /// </summary>
    public partial class SetPasswordWindow : Window
    {
        private readonly User _currentUser;
        private LocalizationManager _loc;
        public bool PasswordSetSuccessfully { get; private set; } = false;
        public SetPasswordWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            _loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show(GetLoc("SetPassword_EmptyError"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show(GetLoc("SetPassword_MismatchError"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _currentUser.password = HashPassword(newPassword);
                _currentUser.isNew = false;
                _currentUser.plainPassword = newPassword;
                ConnectObject.GetConnect().SaveChanges();
                PasswordSetSuccessfully = true;
                MessageBox.Show(GetLoc("SetPassword_Success"), GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(GetLoc("SetPassword_SaveError"), ex.Message), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PasswordSetSuccessfully = false;
            this.Close();
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private string GetLoc(string key)
        {
            return _loc?[key] ?? key;
        }
    }
}
