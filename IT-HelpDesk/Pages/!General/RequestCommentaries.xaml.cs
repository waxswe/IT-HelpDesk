using IT_HelpDesk.Data;
using IT_HelpDesk.Data.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IT_HelpDesk.Pages._General
{
    public class CommentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OwnCommentTemplate { get; set; }
        public DataTemplate OtherCommentTemplate { get; set; }
        public DataTemplate SystemCommentTemplate { get; set; }  

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            dynamic comment = item;
            if (comment.IsSystem) return SystemCommentTemplate;
            return comment.IsOwn ? OwnCommentTemplate : OtherCommentTemplate;
        }
    }
    public partial class RequestCommentaries : Page
    {
        private Request _currentRequest;
        private List<Comment> _allComments;
        private int? _editingCommentId = null; // если не null, то редактируем комментарий с этим ID
        private LocalizationManager _loc;

        public RequestCommentaries(Request request)
        {
            InitializeComponent();
            _currentRequest = request;
            _loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (_loc != null)
                _loc.LanguageChanged += (s, e) => RefreshUI();
            Loaded += async (s, e) => await LoadCommentsAsync();
            UpdateTitle();

            if (AuthService.CurrentUser?.roleID == 4) 
                CallManagerButton.Visibility = Visibility.Visible;
            else
                CallManagerButton.Visibility = Visibility.Collapsed;
        }

        private async Task LoadCommentsAsync()
        {
            _allComments = ConnectObject.GetConnect().Comments.Where(c => c.requestID == _currentRequest.requestID).OrderBy(c => c.createdAt).ToList();
            RefreshCommentsList();
            UpdateInputAvailability();
            await Task.CompletedTask;
        }

        private void RefreshCommentsList()
        {
            User currentUser = AuthService.CurrentUser;
            List<dynamic> items = new List<dynamic>();

            foreach (Comment comment in _allComments)
            {
                User author = ConnectObject.GetConnect().Users.Find(comment.userID);
                bool isCurrentUser = (author?.userID == currentUser.userID);
                bool isSystem = comment.isSystem == true;
                bool isDeleted = (comment.text == "[DELETED]");
                bool isEdited = comment.isEdited == true;

                string displayText = "";
                string authorName = "";
                string authorRole = "";
                string avatarPath = "/Data/Images/avatar.jpg";

                if (isSystem)
                {
                    string eventType = null;
                    if (comment.eventID.HasValue)
                    {
                        CommentEvent commentEvent = ConnectObject.GetConnect().CommentEvents.Find(comment.eventID);
                        eventType = commentEvent?.eventType;
                    }

                    if (!string.IsNullOrEmpty(eventType))
                    {
                        string templateKey = $"System_Comment_{eventType}";
                        string template = _loc?[templateKey] ?? templateKey;
                        string parameters = comment.text;
                        string displayParam = parameters;

                        if (!string.IsNullOrEmpty(parameters))
                        {
                            if (eventType == "StatusChanged" && parameters.StartsWith("statusId="))
                            {
                                int statusId = int.Parse(parameters.Substring(9));
                                displayParam = GetCaseStatusTranslation(statusId);
                            }
                            else if (eventType == "Assigned" && parameters.StartsWith("userId="))
                            {
                                int userId = int.Parse(parameters.Substring(7));
                                User user = ConnectObject.GetConnect().Users.Find(userId);
                                if (user != null)
                                {
                                    displayParam = (_loc?.CurrentLanguage == "en" && _loc != null) ? _loc.Transliterate(user.name) : user.name;
                                }
                            }
                        }

                        displayText = string.IsNullOrEmpty(displayParam) ? template : string.Format(template, displayParam);
                    }
                    else
                    {
                        displayText = comment.text;
                    }
                    authorName = _loc?["System_Comment_Label"] ?? "System";
                    authorRole = "";
                }
                else if (isDeleted)
                {
                    displayText = _loc?["Message_Deleted_Text"] ?? "[Message deleted]";
                }
                else
                {
                    displayText = comment.text;
                }

                if (!isSystem && author != null)
                {
                    if (_loc?.CurrentLanguage == "en")
                        authorName = _loc.Transliterate(author.name);
                    else
                        authorName = author.name;

                    authorRole = _loc?.GetRoleTranslation(author.roleID) ?? "";
                    if (!string.IsNullOrEmpty(author.avatarURL) && author.avatarURL != "/Data/Images/avatar.jpg")
                    {
                        string fullPath = AvatarHelper.GetFullAvatarPath(author.avatarURL);
                        if (System.IO.File.Exists(fullPath)) avatarPath = fullPath;
                    }
                }

                string editedIndicator = isEdited ? (_loc?["Edited_Label"] ?? "edited") : "";

                var item = new
                {
                    CommentID = comment.commentID,
                    UserID = author?.userID,
                    AuthorName = authorName,
                    AuthorRole = authorRole,
                    CreatedAt = comment.createdAt?.ToString("dd.MM HH:mm") ?? "",
                    DisplayText = displayText,
                    IsOwn = isCurrentUser,
                    IsEdited = isEdited,
                    IsSystem = isSystem,
                    EditedIndicator = editedIndicator,
                    EditedVisibility = isEdited ? Visibility.Visible : Visibility.Collapsed,
                    ActionsVisibility = (isCurrentUser && !isSystem && !isDeleted) ? Visibility.Visible : Visibility.Collapsed,
                    AvatarPath = avatarPath,
                    BackgroundColor = isCurrentUser ? "#E1F5FE" : (isSystem ? "#FFF9C4" : "#FFFFFF"),
                };
                items.Add(item);
            }

            CommentsItemsControl.ItemsSource = items;
            bool hasComments = items.Any();
            CommentsItemsControl.Visibility = hasComments ? Visibility.Visible : Visibility.Collapsed;
            NoCommentsTextBlock.Visibility = hasComments ? Visibility.Collapsed : Visibility.Visible;

            CommentsScrollViewer.ScrollToBottom();
        }

        private string GetCaseStatusTranslation(int statusId)
        {
            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            return loc?.GetCaseStatusTranslation(statusId) ?? statusId.ToString();
        }

        private void RefreshUI()
        {
            UpdateTitle();
            RefreshCommentsList();
            UpdateInputAvailability();
        }

        private void UpdateTitle()
        {
            TitleTextBlock.Text = string.Format(_loc?["Comments_Title"] ?? "Comments for request \"{0}\"", _currentRequest.title);
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendCommentAsync();
        }

        private async Task SendCommentAsync()
        {
            if (_currentRequest.requestStatusID >= 5)
            {
                MessageBox.Show(GetLoc("Comment_Disabled_Message"), GetLoc("Warning_Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string text = CommentTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (_editingCommentId.HasValue)
            {
                Comment comment = _allComments.FirstOrDefault(c => c.commentID == _editingCommentId.Value);
                if (comment != null)
                {
                    comment.text = text;
                    comment.isEdited = true;
                    comment.updatedAt = DateTime.Now;
                    await ConnectObject.GetConnect().SaveChangesAsync();
                    _editingCommentId = null;
                    SendButton.ToolTip = _loc?["Send_Button_Tooltip"] ?? "Send";
                    CommentTextBox.Text = "";
                }
            }
            else
            {
                // Новый комментарий
                Comment newComment = new Comment
                {
                    requestID = _currentRequest.requestID,
                    userID = AuthService.CurrentUser.userID,
                    isSystem = false,
                    text = text,
                    createdAt = DateTime.Now,
                    isEdited = false
                };
                ConnectObject.GetConnect().Comments.Add(newComment);
                await ConnectObject.GetConnect().SaveChangesAsync();

                CommentTextBox.Text = "";

                User currentUser = AuthService.CurrentUser;
                Request request = ConnectObject.GetConnect().Requests.Find(_currentRequest.requestID);
                if (request != null)
                {
                    bool statusChanged = false;
                    if (currentUser.roleID == 4 && (request.requestStatusID == 2 || request.requestStatusID == 3))
                    {
                        request.requestStatusID = 4;
                        statusChanged = true;
                    }
                    else if (currentUser.roleID == 2 && request.requestStatusID == 4)
                    {
                        request.requestStatusID = 3;
                        statusChanged = true;
                    }

                    if (statusChanged)
                    {
                        request.updatedAt = DateTime.Now;
                        request.updatedBy = currentUser.userID;
                        await ConnectObject.GetConnect().SaveChangesAsync();
                        _currentRequest.requestStatusID = request.requestStatusID;
                        UpdateInputAvailability();
                    }

                    if (request.clientID != currentUser.userID)
                    {
                        NotificationService.Create(request.clientID, "Notification_Comment_ToClient", requestId: request.requestID, initiatorId: currentUser.userID, formatArgs: request.requestID);
                    }
                    // Уведомление исполнителю
                    if (request.workerID.HasValue && request.workerID.Value != currentUser.userID)
                    {
                        NotificationService.Create(request.workerID.Value, "Notification_Comment_ToExecutor", requestId: request.requestID, initiatorId: currentUser.userID, formatArgs: request.requestID);
                    }
                }
            }

            await LoadCommentsAsync();
        }

        private void UpdateInputAvailability()
        {
            bool isEditable = _currentRequest.requestStatusID < 5; 
            CommentTextBox.IsEnabled = isEditable;
            SendButton.IsEnabled = isEditable;
            if (!isEditable)
                CommentTextBox.ToolTip = "Комментирование недоступно для завершённых заявок";
            else
                CommentTextBox.ToolTip = _loc?["Comment_Input_Placeholder"] ?? "Введите комментарий...";
        }

        private void CallManagerButton_Click(object sender, RoutedEventArgs e)
        {
            List<User> managers = ConnectObject.GetConnect().Users.Where(u => u.roleID == 3 && u.statusID == 1).ToList();

            foreach (User manager in managers)
            {
                NotificationService.Create(manager.userID, "Notification_NeedManager_ToManager", requestId: _currentRequest.requestID, initiatorId: AuthService.CurrentUser.userID, formatArgs: _currentRequest.requestID);
            }

            // Показываем подтверждение пользователю
            MessageBox.Show(GetLoc("CallManager_Sent") ?? "Запрос отправлен менеджеру", GetLoc("Success_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void EditComment_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int commentId = (int)button.Tag;
            Comment comment = _allComments.FirstOrDefault(c => c.commentID == commentId);
            if (comment != null)
            {
                _editingCommentId = commentId;
                CommentTextBox.Text = comment.text;
                SendButton.ToolTip = _loc?["Edit_Button_Tooltip"] ?? "Edit";
                CommentTextBox.Focus();
            }
        }

        private async void DeleteComment_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int commentId = (int)button.Tag;
            MessageBoxResult result = MessageBox.Show(_loc?["Confirm_Delete_Message_Com"] ?? "Are you sure you want to delete this message?",
                                         _loc?["Confirm_Delete_Title_Com"] ?? "Delete message",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Comment comment = _allComments.FirstOrDefault(c => c.commentID == commentId);
                if (comment != null)
                {
                    comment.text = "[DELETED]";
                    comment.isEdited = false;
                    await ConnectObject.GetConnect().SaveChangesAsync();
                    await LoadCommentsAsync();
                }
            }
        }

        private void Avatar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            int? userId = border?.Tag as int?;
            if (userId.HasValue && userId.Value != AuthService.CurrentUser.userID)
            {
                User user = ConnectObject.GetConnect().Users.Find(userId.Value);
                if (user != null)
                {
                    UserProfile profileWindow = new UserProfile(user);
                    profileWindow.ShowDialog();
                }
            }
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                SendButton_Click(null, null);
                e.Handled = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.GoBack();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            FrameObject.frameMain.GoBack();
        }

        private string GetLoc(string key)
        {
            return _loc?[key] ?? key;
        }
    }
}