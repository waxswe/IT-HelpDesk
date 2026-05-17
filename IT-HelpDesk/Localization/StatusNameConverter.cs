using IT_HelpDesk.Data.Classes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace IT_HelpDesk.Localization
{
    public class StatusNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int statusID)
            {
                LocalizationManager loc = App.Current.Resources["LocalizationManager"] as LocalizationManager;
                return loc?.GetStatusTranslation(statusID) ?? statusID.ToString();
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
