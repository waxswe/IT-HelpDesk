using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using IT_HelpDesk.Pages._General;
using IT_HelpDesk.Pages.Client;
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
using static System.Collections.Specialized.BitVector32;

namespace IT_HelpDesk.Pages.Manager
{
    /// <summary>
    /// Логика взаимодействия для EditRequestStatus.xaml
    /// </summary>
    public partial class EditRequestStatus : Page
    {
        private Request _currentRequest;
        private bool _isNewRequest = false;
        public EditRequestStatus(Request request)
        {
            InitializeComponent();

            if (request == null)
            {
                _currentRequest = new Request();
                _isNewRequest = true;
            }
            else
            {
                _currentRequest = request;
                _isNewRequest = false;
            }

            CBStatus.SelectedValue = _currentRequest.requestStatusID;

            DataContext = _currentRequest;
            LoadSections();
            LoadCategories();
            LoadStatuses();

            SetSectionAndCategory();

            CBSection.IsEnabled = false;
            CBCategory.IsEnabled = false;

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null) loc.LanguageChanged += OnLanguageChanged;
            
        }

        private void LoadSections()
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            
            List<RequestSection> sections = ConnectObject.GetConnect().RequestSections.ToList();
            var items = sections.Select(s => new
            {
                SectionID = s.requestSectionID,
                LocalizedName = loc?.GetRequestSectionTranslation(s.requestSectionID) ?? s.requestSection1
            }).ToList();

            CBSection.ItemsSource = items;
        }

        private void LoadCategories()
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;

            List<RequestCategory> categories = ConnectObject.GetConnect().RequestCategories.ToList();
            var items = categories.Select(c => new
            {
                CategoryID = c.requestCategoryID,
                LocalizedName = loc?.GetCaseCategoryTranslation(c.requestCategoryID) ?? c.requestCategory1
            }).ToList();

            CBCategory.ItemsSource = items;
        }

        private void LoadStatuses()
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;

            List<RequestStatus> statuses = ConnectObject.GetConnect().RequestStatuses.Where(s => s.requestStatusID != 6 && s.requestStatusID != 7).OrderBy(s => s.requestStatusID).ToList();

            var items = statuses.Select(s => new
            {
                requestStatusID = s.requestStatusID,
                LocalizedName = loc?.GetCaseStatusTranslation(s.requestStatusID) ?? s.requestStatus1
            }).ToList();

            CBStatus.ItemsSource = items;
            if (items.Any(i => i.requestStatusID == _currentRequest.requestStatusID))
                CBStatus.SelectedValue = _currentRequest.requestStatusID;
            else
                CBStatus.SelectedValue = items.FirstOrDefault()?.requestStatusID;
        }

        private void SetSectionAndCategory()
        {
            if (_currentRequest.requestCategoryID == 0) return;

            RequestCategory category = ConnectObject.GetConnect().RequestCategories.Find(_currentRequest.requestCategoryID);
            if (category != null)
            {
                CBCategory.SelectedValue = category.requestCategoryID;
                RequestSection section = ConnectObject.GetConnect().RequestSections.Find(category.requestSectionID);
                if (section != null)
                    CBSection.SelectedValue = section.requestSectionID;
            }
        }

        private async void SendCaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (CBStatus.SelectedValue == null) return;

            int newStatusId = (int)CBStatus.SelectedValue;
            string newStatusName = GetCaseStatusTranslation(newStatusId);

            if (!_isNewRequest && newStatusId == _currentRequest.requestStatusID)
            {
                FrameObject.frameMain.GoBack();
                return;
            }

            Request updatedRequest = null;
            if (_isNewRequest)
            {
                ConnectObject.GetConnect().Requests.Add(_currentRequest);
                updatedRequest = _currentRequest;
            }
            else
            {
                Request request = ConnectObject.GetConnect().Requests.Find(_currentRequest.requestID);
                if (request != null)
                {
                    request.requestStatusID = newStatusId;
                    request.updatedAt = DateTime.Now;
                    request.updatedBy = AuthService.CurrentUser.userID;
                    if (newStatusId == 1)
                    {
                        request.workerID = null;
                    }
                    updatedRequest = request;
                }
                else
                {
                    MessageBox.Show(GetLoc("Error_RequestNotFound"), GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }


            try
            {
                await ConnectObject.GetConnect().SaveChangesAsync();

                Request savedRequest = ConnectObject.GetConnect().Requests.Find(_currentRequest.requestID);
                if (savedRequest.clientID != AuthService.CurrentUser.userID)
                {
                    NotificationService.Create(savedRequest.clientID, "Notification_StatusChanged", savedRequest.requestID, AuthService.CurrentUser.userID);
                }
                if (savedRequest.workerID.HasValue && savedRequest.workerID.Value != AuthService.CurrentUser.userID)
                {
                    NotificationService.Create(savedRequest.workerID.Value, "Notification_StatusChanged", savedRequest.requestID, AuthService.CurrentUser.userID);
                }


                CommentHelper.AddSystemComment(_currentRequest.requestID, "StatusChanged", newStatusName);
                if (newStatusId == 1 && savedRequest.workerID != null)
                {
                    CommentHelper.AddSystemComment(savedRequest.requestID, "ExecutorRemoved", "");
                    NotificationService.Create(updatedRequest.workerID.Value, "Notification_ExecutorRemoved", updatedRequest.requestID, AuthService.CurrentUser.userID);
                }

                MessageBox.Show(GetLoc("Request_Updated_Success"), GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                FrameObject.frameMain.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, GetLoc("Error_EmptyTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCaseStatusTranslation(int statusId)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?.GetCaseStatusTranslation(statusId) ?? statusId.ToString();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.GoBack();
        }

        private void CommentsButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.Navigate(new RequestCommentaries(_currentRequest));
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Перезагружаем разделы, категории, статусы
            LoadSections();
            LoadCategories();
            LoadStatuses();

            // Восстанавливаем выбранные значения (важно!)
            if (_currentRequest != null)
            {
                // Восстанавливаем раздел (по категории)
                SetSectionAndCategory();
                // Восстанавливаем статус
                CBStatus.SelectedValue = _currentRequest.requestStatusID;
            }

            // Обновляем заголовок окна и текст кнопки (если они привязаны к локализации)
            AddEditCaseLabel.Content = GetLoc("Edit_Request");
            SendCaseButton.Content = GetLoc("Save_Button");
        }

        private string GetLoc(string key)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?[key] ?? key;
        }
    }
}
