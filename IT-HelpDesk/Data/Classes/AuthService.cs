using IT_HelpDesk.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IT_HelpDesk.Data.Classes
{
    internal class AuthService
    {
        public static User CurrentUser { get; set; }

        public static void Login(User user)
        {
            CurrentUser = user;
        }

        public static void Logout()
        {
            CurrentUser = null;

            // Очищаем сохранённые данные авторизации (если они есть)
            Data.Settings.Default.RememberMe = false;
            Data.Settings.Default.SavedLogin = "";
            Data.Settings.Default.SavedPasswordHash = "";
            Data.Settings.Default.Save();
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return Convert.ToBase64String(bytes);
            }
        }

        public static void setPassword(string login, string password)
        {
            SetUserPassword(login, password);
        }

        private static void SetUserPassword(string login, string password)
        {
            User user = ConnectObject.GetConnect().Users.FirstOrDefault(u => u.login == login);
            if (user != null)
            {
                user.password = ComputeSha256Hash((string)password);
                user.plainPassword = password;
                ConnectObject.GetConnect().SaveChanges();
            }
        }
    }
}
