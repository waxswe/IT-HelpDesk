using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IT_HelpDesk.Data.Classes
{
    internal class NotificationService
    {
        public static void Create(int userId, string templateKey, int? requestId = null, int? initiatorId = null, params object[] args)
        {

            NotificationTemplate template = ConnectObject.GetConnect().NotificationTemplates.FirstOrDefault(t => t.templateKey == templateKey);
            if (template == null) return;

            Notification notification = new Notification
            {
                userID = userId,
                notificationStatusID = 1,
                templateID = template.templateID,
                initiatorID = initiatorId,
                requestID = requestId,
                createdAt = DateTime.Now,
                isRead = false
            };
            ConnectObject.GetConnect().Notifications.Add(notification);
            ConnectObject.GetConnect().SaveChanges();

        }

        public static int GetUnreadCount(int userId)
        {
            return ConnectObject.GetConnect().Notifications.Count(n => n.userID == userId && !n.isRead);

        }

        public static List<NotificationItem> GetNotificationsPage(int userId, int skip, int take, LocalizationManager loc)
        {
            var notifications = ConnectObject.GetConnect().Notifications.Where(n => n.userID == userId).OrderByDescending(n => n.createdAt).Skip(skip).Take(take)
                .Select(n => new
                {
                    n.notificationID,
                    n.createdAt,
                    n.isRead,
                    n.requestID,
                    n.initiatorID,
                    Template = n.NotificationTemplate,
                    n.notificationStatusID
                })
                .ToList();

            List<NotificationItem> result = new List<NotificationItem>();
            foreach (var n in notifications)
            {
                string messageTemplate = loc[n.Template.templateKey] ?? n.Template.template;
                object[] args = BuildArgs(n.requestID, n.initiatorID, loc);
                string message = string.Format(messageTemplate, args);
                result.Add(new NotificationItem
                {
                    NotificationID = n.notificationID,
                    TemplateKey = n.Template.templateKey,
                    Message = message,
                    CreatedAt = n.createdAt.Value,
                    IsRead = n.isRead,
                    RequestID = n.requestID,
                    InitiatorID = n.initiatorID
                });
            }
            return result;

        }

        private static object[] BuildArgs(int? requestId, int? initiatorId, LocalizationManager loc)
        {
            List<object> args = new List<object>();
            if (requestId.HasValue) args.Add(requestId.Value);
            if (initiatorId.HasValue)
            {
                User user = ConnectObject.GetConnect().Users.Find(initiatorId.Value);
                string name = (loc.CurrentLanguage == "en" && user != null) ? loc.Transliterate(user.name) : user?.name;
                args.Add(name ?? "");

            }
            return args.ToArray();
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
        public string TemplateKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int? RequestID { get; set; }
        public int? InitiatorID { get; set; }
    }
}

