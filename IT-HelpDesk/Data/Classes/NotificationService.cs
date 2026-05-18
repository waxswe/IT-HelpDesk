using System;
using System.Collections.Generic;
using System.Linq;
using IT_HelpDesk.Data;

namespace IT_HelpDesk.Data.Classes
{
    public static class NotificationService
    {
        public static void Create(int userId, string templateKey, int? requestId = null, int? initiatorId = null, params object[] formatArgs)
        {
            NotificationTemplate template = ConnectObject.GetConnect().NotificationTemplates.FirstOrDefault(t => t.templateKey == templateKey);
            if (template == null) return;

            LocalizationManager loc = App.Current.Resources["LocalizationManager"] as LocalizationManager;
            string templateText = loc?[templateKey] ?? template.template;

            List<object> args = new List<object>();
            if (requestId.HasValue) args.Add(requestId.Value);
            if (formatArgs != null) args.AddRange(formatArgs);

            string message = args.Count > 0 ? string.Format(templateText, args.ToArray()) : templateText;

            Notification notification = new Notification
            {
                userID = userId,
                notificationStatusID = 1,
                templateID = template.templateID,
                initiatorID = initiatorId,
                requestID = requestId,
                createdAt = DateTime.Now,
                isRead = false,
                message = message
            };
            ConnectObject.GetConnect().Notifications.Add(notification);
            ConnectObject.GetConnect().SaveChanges();

        }

        public static int GetUnreadCount(int userId)
        {
            return ConnectObject.GetConnect().Notifications.Count(n => n.userID == userId && !n.isRead);
        }

        public static List<NotificationItem> GetNotificationsPage(int userId, int skip, int take)
        {
            List<NotificationItem> notifications = ConnectObject.GetConnect().Notifications.Where(n => n.userID == userId).OrderByDescending(n => n.createdAt).Skip(skip).Take(take)
                .Select(n => new NotificationItem
                {
                    NotificationID = n.notificationID,
                    Message = n.message,
                    CreatedAt = n.createdAt.Value,
                    IsRead = n.isRead,
                    RequestID = n.requestID,
                    InitiatorID = n.initiatorID,
                    TemplateKey = n.NotificationTemplate.templateKey
                })
                .ToList();

            LocalizationManager loc = App.Current.Resources["LocalizationManager"] as LocalizationManager;
            if (loc != null)
            {
                foreach (NotificationItem item in notifications)
                {
                    if (item.TemplateKey == "Notification_UserBlocked_ToAdmin")
                    {
                        string login = "пользователь";
                        if (item.InitiatorID.HasValue)
                        {
                            User user = ConnectObject.GetConnect().Users.Find(item.InitiatorID.Value);
                            if (user != null)
                                login = user.login;

                        }
                        string template = loc["Notification_UserBlocked_ToAdmin"] ?? "Пользователь {0} был заблокирован...";
                        item.Message = string.Format(template, login);
                    }
                }
            }

            return notifications;
        }

        public static void MarkAsRead(int notificationId)
        {
            Notification n = ConnectObject.GetConnect().Notifications.Find(notificationId);
            if (n != null && !n.isRead)
            {
                n.isRead = true;
                n.notificationStatusID = 2;
                ConnectObject.GetConnect().SaveChanges();
            }

        }

        public static void MarkAllAsRead(int userId)
        {
            IQueryable<Notification> notifications = ConnectObject.GetConnect().Notifications.Where(n => n.userID == userId && !n.isRead);
            foreach (Notification n in notifications)
            {
                n.isRead = true;
                n.notificationStatusID = 2;
            }
            ConnectObject.GetConnect().SaveChanges();

        }

        public static int GetTotalCount(int userId)
        {
            return ConnectObject.GetConnect().Notifications.Count(n => n.userID == userId);
        }

        public static void NotifyAllAdmins(string templateKey, int? requestId = null, int? initiatorId = null, params object[] formatArgs)
        {
            List<User> admins = ConnectObject.GetConnect().Users.Where(u => u.roleID == 1 && u.statusID == 1).ToList();
            foreach (User admin in admins)
            {
                Create(admin.userID, templateKey, requestId, initiatorId, formatArgs);
            }
        }
    }

    public class NotificationItem
    {
        public int NotificationID { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int? RequestID { get; set; }
        public int? InitiatorID { get; set; }
        public string TemplateKey { get; set; }
    }
}