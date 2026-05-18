using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IT_HelpDesk.Data.Classes
{
    public static class CommentHelper
    {
        public static void AddSystemComment(int requestId, string eventType, params object[] args)
        {
            string parameters = args != null && args.Length > 0 ? args[0]?.ToString() : "";
            if (string.IsNullOrEmpty(parameters)) parameters = " ";

            Comment comment = new Comment
            {
                requestID = requestId,
                userID = null,          
                isSystem = true,
                text = parameters,
                createdAt = DateTime.Now,
                isEdited = false,
                eventID = GetEventId(eventType)
            };
            ConnectObject.GetConnect().Comments.Add(comment);
            ConnectObject.GetConnect().SaveChanges();
        }

        public static int GetEventId(string eventType)
        {
                CommentEvent ev = ConnectObject.GetConnect().CommentEvents.FirstOrDefault(e => e.eventType == eventType);
                if (ev == null) throw new Exception($"Event type '{eventType}' not found");
                return ev.eventID;
            
        }
    }
}
