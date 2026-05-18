using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Data;
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
using IT_HelpDesk.Pages._General;

namespace IT_HelpDesk.Pages.Client
{
    /// <summary>
    /// Логика взаимодействия для AddEditCase.xaml
    /// </summary>
    public partial class AddEditCase : Page
    {
        private Request _currentCase = new Request();
        private bool isNewCase = true;
        private List<dynamic> _sections;
        private List<dynamic> _allCategories;
        private bool _isLoadingCategories = false;
        const int maxTitleLength = 255;
        public AddEditCase(Request selectedCase)
        {
            InitializeComponent();

            if (selectedCase != null)
            {
                _currentCase = selectedCase;
                isNewCase = false;
            }
            else
            {
                isNewCase = true;
            }


            if (isNewCase)
                CommentsButton.Visibility = Visibility.Collapsed;

            DataContext = _currentCase;
            LoadSectionsAndCategories();
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null) loc.LanguageChanged += (s, e) => LoadSectionsAndCategories();
        }

        private void LoadSectionsAndCategories()
        {
            // Сохраняем текущие выбранные значения
            int? selectedSectionId = CBSection.SelectedValue as int?;
            int? selectedCategoryId = CBCategory.SelectedValue as int?;

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;

            // Загрузка разделов
            List<RequestSection> sections = ConnectObject.GetConnect().RequestSections.OrderBy(s => s.requestSectionID).ToList();
            _sections = sections.Select(s => new
            {
                SectionID = s.requestSectionID,
                LocalizedName = loc?.GetRequestSectionTranslation(s.requestSectionID) ?? s.requestSection1
            }).ToList<dynamic>();
            CBSection.ItemsSource = _sections;

            // Загрузка всех категорий
            List<RequestCategory> categories = ConnectObject.GetConnect().RequestCategories.OrderBy(c => c.requestCategoryID).ToList();
            _allCategories = categories.Select(c => new
            {
                CategoryID = c.requestCategoryID,
                SectionID = c.requestSectionID,
                LocalizedName = loc?.GetCaseCategoryTranslation(c.requestCategoryID) ?? c.requestCategory1
            }).ToList<dynamic>();

            // Восстанавливаем выбранный раздел, если он был
            if (selectedSectionId.HasValue)
            {
                CBSection.SelectedValue = selectedSectionId.Value;
                FilterCategoriesBySection(selectedSectionId.Value);
                // Восстанавливаем выбранную категорию, если она была и принадлежит текущему разделу
                if (selectedCategoryId.HasValue)
                {
                    dynamic cat = _allCategories.FirstOrDefault(c => c.CategoryID == selectedCategoryId.Value);
                    if (cat != null && cat.SectionID == selectedSectionId.Value)
                        CBCategory.SelectedValue = selectedCategoryId.Value;
                }
            }
            else if (!isNewCase && _currentCase.requestCategoryID != 0)
            {
                // Редактирование – устанавливаем по данным заявки
                dynamic selectedCategory = _allCategories.FirstOrDefault(c => c.CategoryID == _currentCase.requestCategoryID);
                if (selectedCategory != null)
                {
                    CBSection.SelectedValue = selectedCategory.SectionID;
                    FilterCategoriesBySection(selectedCategory.SectionID);
                    CBCategory.SelectedValue = _currentCase.requestCategoryID;
                }
            }

            // Обновляем заголовки и кнопки
            AddEditCaseLabel.Content = (isNewCase ? GetLoc("Add_Request") : GetLoc("Edit_Request"));
            SendCaseButton.Content = (isNewCase ? GetLoc("Send_Case_Button") : GetLoc("Save_Button"));
        }

        private void CBSection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingCategories) return;
            if (CBSection.SelectedValue != null)
            {
                int sectionId = (int)CBSection.SelectedValue;
                FilterCategoriesBySection(sectionId);
            }
            else
            {
                CBCategory.ItemsSource = null;
            }
        }

        private void FilterCategoriesBySection(int sectionId)
        {
            List<dynamic> filtered = _allCategories.Where(c => c.SectionID == sectionId).ToList();
            CBCategory.ItemsSource = filtered;
        }

        private void SendCaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (CBCategory.SelectedValue == null)
            {
                MessageBox.Show(GetLoc("Select_Category_Error"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(TBTitle.Text))
            {
                MessageBox.Show(GetLoc("Enter_Title_Error"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                TBTitle.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(TBDescriprion.Text))
            {
                MessageBox.Show(GetLoc("Enter_Description_Error"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                TBDescriprion.Focus();
                return;
            }

            if (TBTitle.Text.Length > maxTitleLength)
            {
                MessageBox.Show(string.Format(GetLoc("Error_TitleTooLong"), maxTitleLength), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                TBTitle.Focus();
                return;
            }

            _currentCase.updatedBy = AuthService.CurrentUser.userID;
            _currentCase.requestCategoryID = (int)CBCategory.SelectedValue;

            if (_currentCase.requestID == 0)
            {
                _currentCase.clientID = AuthService.CurrentUser.userID;
                _currentCase.requestStatusID = 1;
                ConnectObject.GetConnect().Requests.Add(_currentCase);
                _currentCase.createdAt = DateTime.Now;
            }
            try
            {
                ConnectObject.GetConnect().SaveChanges();

                if (_currentCase.requestID != 0)
                {
                    // Уведомления менеджерам
                    List<User> managers = ConnectObject.GetConnect().Users.Where(u => u.roleID == 3 && u.statusID == 1).ToList();
                    foreach (User manager in managers)
                    {
                        NotificationService.Create(manager.userID, "Notification_NewRequest_ToManager", requestId: _currentCase.requestID, formatArgs: _currentCase.requestID);
                    }
                    // Уведомление пользователю об успешном создании
                    NotificationService.Create(AuthService.CurrentUser.userID, "Success_Request_Created", requestId: _currentCase.requestID, formatArgs: _currentCase.requestID);
                    // Системный комментарий
                    CommentHelper.AddSystemComment(_currentCase.requestID, "Created", _currentCase.requestID);
                }

                MessageBox.Show(isNewCase ? GetLoc("Case_Added_Success") : GetLoc("Case_Updated_Success"), GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            FrameObject.frameMain.GoBack();
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TBDescriprion.Text) || !string.IsNullOrEmpty(TBTitle.Text))
            {
                MessageBoxResult result = MessageBox.Show(GetLoc("Confirm_Logout_Message"), GetLoc("Confirm_Logout_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) FrameObject.frameMain.Navigate(new ClientPage());
            }
            else FrameObject.frameMain.GoBack();
        }

        private void CommentsButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.Navigate(new RequestCommentaries(_currentCase));
        }

        private void CBCategory_DropDownOpened(object sender, EventArgs e)
        {
            if (CBSection.SelectedValue == null)
            {
                MessageBox.Show(GetLoc("Error_Select_Section_First"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                CBCategory.IsDropDownOpen = false;
            }
        }
    }
}
