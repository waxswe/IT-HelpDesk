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
            string savedLang = Data.Settings.Default.Language;
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
            Data.Settings.Default.Language = languageCode;
            Data.Settings.Default.Save();

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
                    case 1: return "Сантехник";
                    case 2: return "Электрик";
                    case 3: return "Уборщик";
                    case 4: return "Слесарь";
                    case 5: return "Системный администратор";
                    case 6: return "Специалист по ремонту ПК";
                    case 7: return "Сетевой инженер";
                    case 8: return "Специалист по программному обеспечению";
                    case 9: return "Специалист общего профиля";
                    default: return "—";
                }
            }
            else
            {
                switch (professionID)
                {
                    case 1: return "Plumber";
                    case 2: return "Electrician";
                    case 3: return "Cleaner";
                    case 4: return "Locksmith";
                    case 5: return "System administrator";
                    case 6: return "PC repair specialist";
                    case 7: return "Network engineer";
                    case 8: return "Software specialist";
                    case 9: return "General specialist";
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
                    case 1: return "Ремонт помещения";
                    case 2: return "Техническое обслуживание";
                    case 3: return "Уборка и клининг";
                    case 4: return "Обслуживание помещений";
                    case 5: return "Доступ и пропуски";
                    case 6: return "Сбой программного обеспечения";
                    case 7: return "Ошибка в работе ПК";
                    case 8: return "Сетевое оборудование";
                    case 9: return "Техническая поддержка";
                    case 10: return "Доступ, учётные записи и информационная безопасность";
                    case 11: return "Периферийное оборудование и печать";
                    case 12: return "Прочее";
                    default: return "—";
                }
            }
            else
            {
                switch (categoryID)
                {
                    case 1: return "Premises repair";
                    case 2: return "Technical maintenance";
                    case 3: return "Cleaning";
                    case 4: return "Premises maintenance";
                    case 5: return "Access and passes";
                    case 6: return "Software failure";
                    case 7: return "PC malfunction";
                    case 8: return "Network equipment";
                    case 9: return "Technical support";
                    case 10: return "Access, accounts and information security";
                    case 11: return "Peripherals and printing";
                    case 12: return "Other";
                    default: return "—";
                }
            }
        }

        public string Transliterate(string russianText)
        {
            if (string.IsNullOrEmpty(russianText)) return russianText;
            string ru = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            string[] en = new[] { "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "sch", "", "y", "", "e", "yu", "ya" };
            StringBuilder result = new System.Text.StringBuilder();
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