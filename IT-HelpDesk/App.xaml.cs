using IT_HelpDesk.Data.Classes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IT_HelpDesk.Data;

namespace IT_HelpDesk
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string savedLang = Data.Settings.Default.Language;
            if (!string.IsNullOrEmpty(savedLang))
            {
                LocalizationManager loc = new LocalizationManager();
                loc.ChangeLanguage(savedLang);
                Application.Current.Resources["LocalizationManager"] = loc;
            }
        }
    }
}
