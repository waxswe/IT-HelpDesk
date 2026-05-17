using IT_HelpDesk.Data.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IT_HelpDesk.Localization
{
    public static class LocalizedTooltips
    {
        public static string RequiredFieldTooltip =>
            (Application.Current.Resources["LocalizationManager"] as LocalizationManager)?["RequiredField_Tooltip"] ?? "Это поле обязательно для заполнения";
    }
}
