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

namespace IT_HelpDesk
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigated += MainFrame_Navigated;

            FrameObject.frameMain = MainFrame;
            ConnectObject.connect = new ITHelpDeskEntities();
        }

        private void OpenWebsite_Click(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://it-nn.com");
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            Type pageType = e.Content.GetType();

            bool isAuthPage = pageType == typeof(Pages._General.Authorization); // не отображаем на вписанных страницах

            HeaderUserControl.Visibility = isAuthPage ? Visibility.Collapsed : Visibility.Visible;

            if (HeaderUserControl.Visibility == Visibility.Visible)
            {
                HeaderUserControl.RefreshDisplayName();
                HeaderUserControl.DataContext = AuthService.CurrentUser;
                HeaderUserControl.RefreshAvatar();
                HeaderUserControl.UpdateNotificationBadge();
            }
        }
    }
}
