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

namespace IT_HelpDesk.Pages.Administrator
{
    /// <summary>
    /// Логика взаимодействия для MassMailing.xaml
    /// </summary>
    public partial class MassMailing : Page
    {
        public MassMailing()
        {
            InitializeComponent();

            if (AuthService.CurrentUser == null || AuthService.CurrentUser.roleID != 1)
            {
                MessageBox.Show(GetLoc("Access_Denied"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                FrameObject.frameMain.GoBack();
                return;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = TBMassMail.Text.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show(GetLoc("MassMailing_NoMessage"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool sendNotifications = CBNotifications.IsChecked == true;
            bool sendComments = CBComments.IsChecked == true;

            if (!sendNotifications && !sendComments)
            {
                MessageBox.Show(GetLoc("MassMailing_NoDestination"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SendButton.IsEnabled = false;
            SendButton.Content = GetLoc("MassMailing_Sending");

            try
            {
                int totalNotifications = 0;
                int totalComments = 0;

                if (sendNotifications)
                {
                    List<User> users = ConnectObject.GetConnect().Users.Where(u => u.statusID == 1).ToList();
                    foreach (User user in users)
                    {
                        NotificationService.Create(user.userID, "MassMailing_Notification", formatArgs: message);
                        totalNotifications++;
                    }
                }

                if (sendComments)
                {
                    List<Request> requests = ConnectObject.GetConnect().Requests.Where(r => r.requestStatusID >= 1 && r.requestStatusID <= 4).ToList();
                    foreach (Request req in requests)
                    {
                        Comment comment = new Comment
                        {
                            requestID = req.requestID,
                            userID = null,
                            isSystem = true,
                            text = message,
                            createdAt = DateTime.Now,
                            isEdited = false,
                            eventID = null
                        };
                        ConnectObject.GetConnect().Comments.Add(comment);
                        totalComments++;
                    }
                    await ConnectObject.GetConnect().SaveChangesAsync();
                }

                string resultMessage = GetLoc("MassMailing_Success") + "\n" +
                                       string.Format(GetLoc("MassMailing_Result_Notifications"), totalNotifications) + "\n" +
                                       string.Format(GetLoc("MassMailing_Result_Comments"), totalComments);
                MessageBox.Show(resultMessage, GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SendButton.IsEnabled = true;
                SendButton.Content = GetLoc("MassMailing_SendButton");
                FrameObject.frameMain.GoBack();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.GoBack();
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }
    }
}
