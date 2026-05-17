using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IT_HelpDesk.Localization
{
    internal class UserRoleOrProfessionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is User user)
            {
                LocalizationManager loc = App.Current.Resources["LocalizationManager"] as LocalizationManager;
                if (user.roleID == 4 && user.professionID.HasValue)
                {
                    // Исполнитель → показываем профессию
                    return loc?.GetProfessionTranslation(user.professionID.Value) ?? "—";
                }
                else
                {
                    // Остальные → показываем роль
                    return loc?.GetRoleTranslation(user.roleID) ?? user.roleID.ToString();
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
