using IT_HelpDesk.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace IT_HelpDesk.Data.Classes
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private Dictionary<string, string> _currentStrings;
        private string _currentLanguage = "ru";
        private bool _isLoaded = false;

        public event EventHandler LanguageChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public LocalizationManager()
        {
            string savedLang = Settings.Default.Language;
            if (!string.IsNullOrEmpty(savedLang))
                _currentLanguage = savedLang;
        }

        public string CurrentLanguage => _currentLanguage;

        private bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(new DependencyObject());
        }

        private void EnsureLoaded()
        {
            if (_isLoaded) return;
            if (IsInDesignMode())
            {
                _currentStrings = new Dictionary<string, string>();
                _isLoaded = true;
                return;
            }
            try
            {
                LoadLanguage(_currentLanguage);
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                _currentStrings = new Dictionary<string, string>();
                System.Diagnostics.Debug.WriteLine($"Ошибка локализации: {ex.Message}");
            }
        }

        public void ChangeLanguage(string languageCode)
        {
            if (languageCode == _currentLanguage) return;
            _currentLanguage = languageCode;
            _isLoaded = false;
            Settings.Default.Language = languageCode;
            Settings.Default.Save();

            EnsureLoaded();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void LoadLanguage(string languageCode)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string jsonPath = Path.Combine(baseDir, "Localization", $"language.{languageCode}.json");
            if (!File.Exists(jsonPath))
            {
                string projectDir = Directory.GetParent(baseDir).Parent.Parent.FullName;
                jsonPath = Path.Combine(projectDir, "Localization", $"language.{languageCode}.json");
            }
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Файл не найден: {jsonPath}");

            string jsonText = File.ReadAllText(jsonPath);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            _currentStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText, options);
        }

        // Индексатор для привязки в XAML
        public string this[string key]
        {
            get
            {
                EnsureLoaded();
                if (_currentStrings == null || _currentStrings.Count == 0)
                    System.Diagnostics.Debug.WriteLine("Словарь пуст");
                if (_currentStrings != null && _currentStrings.TryGetValue(key, out string value))
                {
                    return value;
                }
                System.Diagnostics.Debug.WriteLine($"Ключ не найден: {key}");
                return $"!{key}";
            }
        }

        public string GetRoleTranslation(int roleID)
        {
            string ruName;
            switch (roleID)
            {
                case 1: ruName = "Администратор"; break;
                case 2: ruName = "Пользователь"; break;
                case 3: ruName = "Менеджер"; break;
                case 4: ruName = "Исполнитель"; break;
                default: ruName = "Неизвестно"; break;
            }
            if (CurrentLanguage == "ru") return ruName;
            switch (roleID)
            {
                case 1: return "Administrator";
                case 2: return "User";
                case 3: return "Manager";
                case 4: return "Executor";
                default: return "Unknown";
            }
        }

        public string GetStatusTranslation(int statusID)
        {
            string ruName;
            switch (statusID)
            {
                case 1: ruName = "Активен"; break;
                case 2: ruName = "Заблокирован"; break;
                case 3: ruName = "Удалён"; break;
                default: ruName = "Неизвестно"; break;
            }
            if (CurrentLanguage == "ru") return ruName;
            switch (statusID)
            {
                case 1: return "Active";
                case 2: return "Blocked";
                case 3: return "Deleted";
                default: return "Unknown";
            }
        }

        public string GetProfessionTranslation(int professionID)
        {
            if (CurrentLanguage == "ru")
            {
                switch (professionID)
                {
                    case 1: return "Специалист АХО";
                    case 2: return "Электромонтёр / Энергетик";
                    case 3: return "Менеджер по закупкам и снабжению";
                    case 4: return "Системный администратор";
                    case 5: return "Инженер технической поддержки";
                    case 6: return "Сетевой инженер";
                    case 7: return "Руководитель IT-отдела";
                    default: return "—";
                }
            }
            else
            {
                switch (professionID)
                {
                    case 1: return "Facilities Specialist";
                    case 2: return "Electrician / Power Engineer";
                    case 3: return "Procurement & Supply Manager";
                    case 4: return "System Administrator";
                    case 5: return "Technical Support Engineer";
                    case 6: return "Network Engineer";
                    case 7: return "IT Department Head";
                    default: return "—";
                }
            }
        }

        public string GetCaseStatusTranslation(int statusID)
        {
            if (CurrentLanguage == "ru")
            {
                switch (statusID)
                {
                    case 1: return "Новая";
                    case 2: return "Назначена";
                    case 3: return "В работе";
                    case 4: return "Ожидает ответа клиента";
                    case 5: return "Выполнена";
                    case 6: return "Отменена";
                    case 7: return "Закрыта";
                    default: return "Неизвестно";
                }
            }
            else
            {
                switch (statusID)
                {
                    case 1: return "New";
                    case 2: return "Assigned";
                    case 3: return "In progress";
                    case 4: return "Awaiting client response";
                    case 5: return "Completed";
                    case 6: return "Cancelled";
                    case 7: return "Closed";
                    default: return "Unknown";
                }
            }
        }

        public string GetRequestSectionTranslation(int sectionID)
        {
            if (CurrentLanguage == "ru")
            {
                switch (sectionID)
                {
                    case 1: return "Административно-хозяйственный";
                    case 2: return "Компьютерная техника и ПО";
                    case 3: return "Другое";
                    default: return "—";
                }
            }
            else
            {
                switch (sectionID)
                {
                    case 1: return "Facility management";
                    case 2: return "Computer hardware and software";
                    case 3: return "Other";
                    default: return "—";
                }
            }
        }

        public string GetCaseCategoryTranslation(int categoryID)
        {
            if (CurrentLanguage == "ru")
            {
                switch (categoryID)
                {
                    case 1: return "Офисная мебель и крепления";
                    case 2: return "Освещение и электрика";
                    case 3: return "Климат-контроль (кондиционеры, отопление)";
                    case 4: return "Уборка помещений и вывоз мусора";
                    case 5: return "Пропуска и система контроля доступа (СКУД)";
                    case 6: return "Ремонт помещений";
                    case 7: return "Расходные материалы (бумага, канцтовары)";
                    case 8: return "Компьютеры и ноутбуки";
                    case 9: return "Мониторы и дисплеи";
                    case 10: return "Операционные системы (Windows, Linux)";
                    case 11: return "Офисные приложения (MS Office, LibreOffice)";
                    case 12: return "Корпоративные системы (1С, CRM, ERP)";
                    case 13: return "Антивирусное ПО и безопасность";
                    case 14: return "Принтеры, МФУ, сканеры";
                    case 15: return "Сетевое оборудование (роутеры, коммутаторы)";
                    case 16: return "Проводная сеть (LAN, Ethernet)";
                    case 17: return "Беспроводная сеть (Wi-Fi)";
                    case 18: return "Серверное оборудование";
                    case 19: return "Расходные материалы (картриджи, бумага)";
                    case 20: return "Другое";
                    default: return "—";
                }
            }
            else
            {
                switch (categoryID)
                {
                    case 1: return "Office furniture & fittings";
                    case 2: return "Lighting & electrical";
                    case 3: return "Climate control (AC, heating)";
                    case 4: return "Cleaning & waste disposal";
                    case 5: return "Passes & access control (ACS)";
                    case 6: return "Premises repair";
                    case 7: return "Consumables (paper, stationery)";
                    case 8: return "Computers & laptops";
                    case 9: return "Monitors & displays";
                    case 10: return "Operating systems (Windows, Linux)";
                    case 11: return "Office applications (MS Office, LibreOffice)";
                    case 12: return "Corporate systems (1C, CRM, ERP)";
                    case 13: return "Antivirus software & security";
                    case 14: return "Printers, MFPs, scanners";
                    case 15: return "Network equipment (routers, switches)";
                    case 16: return "Wired network (LAN, Ethernet)";
                    case 17: return "Wireless network (Wi-Fi)";
                    case 18: return "Server equipment";
                    case 19: return "Consumables (cartridges, paper)";
                    case 20: return "Other";
                    default: return "—";
                }
            }
        }

            public string Transliterate(string russianText)
        {
            if (string.IsNullOrEmpty(russianText)) return russianText;
            string ru = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            string[] en = new[] { "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "sch", "", "y", "", "e", "yu", "ya" };
            StringBuilder result = new StringBuilder();
            foreach (char c in russianText.ToLower())
            {
                int index = ru.IndexOf(c);
                if (index >= 0)
                    result.Append(en[index]);
                else
                    result.Append(c);
            }
            if (result.Length > 0)
                result[0] = char.ToUpper(result[0]);
            return result.ToString();
        }

    }
}