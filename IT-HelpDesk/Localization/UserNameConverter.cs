using IT_HelpDesk.Data.Classes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IT_HelpDesk.Localization
{
    public class UserNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name)
            {
                LocalizationManager loc = App.Current.Resources["LocalizationManager"] as LocalizationManager;
                if (loc != null && loc.CurrentLanguage == "en")
                    return loc.Transliterate(name);
                return name;
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
