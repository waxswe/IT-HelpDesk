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
            using (var context = new ITHelpDeskEntities())
            {
                NotificationTemplate template = context.NotificationTemplates.FirstOrDefault(t => t.templateKey == templateKey);
                if (template == null) return;

                LocalizationManager loc = App.Current.Resources["LocalizationManager"] as LocalizationManager;
                string templateText = loc?[templateKey] ?? template.template;

                var args = new List<object>();
                if (requestId.HasValue) args.Add(requestId.Value);
                if (formatArgs != null) args.AddRange(formatArgs);

                string message = args.Count > 0 ? string.Format(templateText, args.ToArray()) : templateText;

                var notification = new Notification
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
                context.Notifications.Add(notification);
                context.SaveChanges();
            }
        }

        public static int GetUnreadCount(int userId)
        {
            using (var context = new ITHelpDeskEntities())
            {
                return context.Notifications.Count(n => n.userID == userId && !n.isRead);
            }
        }

        public static List<NotificationItem> GetNotificationsPage(int userId, int skip, int take)
        {
            using (var context = new ITHelpDeskEntities())
            {
                return context.Notifications
                    .Where(n => n.userID == userId)
                    .OrderByDescending(n => n.createdAt)
                    .Skip(skip).Take(take)
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
            }
        }

        public static void MarkAsRead(int notificationId)
        {
            using (var context = new ITHelpDeskEntities())
            {
                var n = context.Notifications.Find(notificationId);
                if (n != null && !n.isRead)
                {
                    n.isRead = true;
                    n.notificationStatusID = 2;
                    context.SaveChanges();
                }
            }
        }

        public static void MarkAllAsRead(int userId)
        {
            using (var context = new ITHelpDeskEntities())
            {
                var notifications = context.Notifications.Where(n => n.userID == userId && !n.isRead);
                foreach (var n in notifications)
                {
                    n.isRead = true;
                    n.notificationStatusID = 2;
                }
                context.SaveChanges();
            }
        }

        public static int GetTotalCount(int userId)
        {
            using (var context = new ITHelpDeskEntities())
            {
                return context.Notifications.Count(n => n.userID == userId);
            }
        }

        public static void NotifyAllAdmins(string templateKey, int? requestId = null, int? initiatorId = null, params object[] formatArgs)
        {
            using (var context = new ITHelpDeskEntities())
            {
                var admins = context.Users.Where(u => u.roleID == 1 && u.statusID == 1).ToList();
                foreach (var admin in admins)
                {
                    Create(admin.userID, templateKey, requestId, initiatorId, formatArgs);
                }
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