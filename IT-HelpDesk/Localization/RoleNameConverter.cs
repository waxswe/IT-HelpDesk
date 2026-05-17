using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using IT_HelpDesk.Data.Classes;
using IT_HelpDesk;

namespace IT_HelpDesk.Localization
{
    public class RoleNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int roleID)
            {
                LocalizationManager loc = App.Current.Resources["LocalizationManager"] as LocalizationManager;
                return loc?.GetRoleTranslation(roleID) ?? roleID.ToString();
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}