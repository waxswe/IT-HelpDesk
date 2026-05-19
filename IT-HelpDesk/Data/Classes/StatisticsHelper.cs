using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IT_HelpDesk.Data.Classes
{
    public static class StatisticsHelper
    {
        /// <summary>
        /// Получить статистику по исполнителю за период
        /// </summary>
        public static ExecutorStat GetExecutorStat(User executor, DateTime startDate, DateTime endDate)
        {
            List<Request> requests = ConnectObject.GetConnect().Requests.Where(r => r.workerID == executor.userID && r.requestStatusID != 6 && 
            (startDate == DateTime.MinValue || r.createdAt >= startDate) && (endDate == DateTime.MaxValue || r.createdAt <= endDate)).ToList();

            int total = requests.Count;
            int assigned = requests.Count(r => r.requestStatusID == 2);
            int inProgress = requests.Count(r => r.requestStatusID >= 2 && r.requestStatusID <= 4);
            int completed = requests.Count(r => r.requestStatusID == 5 || r.requestStatusID == 7);
            double completionPercent = total == 0 ? 0 : (double)completed / total * 100;

            LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
            string profession = executor.professionID.HasValue ?
                (loc?.GetProfessionTranslation(executor.professionID.Value) ?? "—") : "—";
            string fullName = (loc?.CurrentLanguage == "en" && loc != null) ? loc.Transliterate(executor.name) : executor.name;

            return new ExecutorStat
            {
                FullName = fullName,
                Profession = profession,
                TotalRequests = total,
                AssignedRequests = assigned,
                InProgressRequests = inProgress,
                CompletedRequests = completed,
                CompletionPercent = completionPercent.ToString("F1") + "%",
                LoadPercent = "0%"
            };
        }

        /// <summary>
        /// Получить статистику по всем исполнителям за период (с расчётом нагрузки)
        /// </summary>
        public static List<ExecutorStat> GetExecutorStats(DateTime startDate, DateTime endDate)
        {
            List<User> executors = ConnectObject.GetConnect().Users.Where(u => u.roleID == 4 && u.statusID == 1).ToList();
            List<Request> allRequests = ConnectObject.GetConnect().Requests.Where(r => r.workerID != null && r.requestStatusID != 6 && (startDate == DateTime.MinValue || r.createdAt >= startDate) &&
                            (endDate == DateTime.MaxValue || r.createdAt <= endDate)).ToList();

            int globalAssignedInProgress = allRequests.Count(r => r.requestStatusID == 2 || r.requestStatusID == 3 || r.requestStatusID == 4);

            List<ExecutorStat> stats = new List<ExecutorStat>();
            foreach (var executor in executors)
            {
                List<Request> requests = allRequests.Where(r => r.workerID == executor.userID).ToList();
                int total = requests.Count;
                int assigned = requests.Count(r => r.requestStatusID == 2);
                int inProgress = requests.Count(r => r.requestStatusID == 3 || r.requestStatusID == 4);
                int completed = requests.Count(r => r.requestStatusID == 5 || r.requestStatusID == 7);
                double completionPercent = total == 0 ? 0 : (double)completed / total * 100;
                double loadPercent = globalAssignedInProgress == 0 ? 0 : (double)(assigned + inProgress) / globalAssignedInProgress * 100;

                LocalizationManager loc = Application.Current.Resources["LocalizationManager"] as LocalizationManager;
                string profession = executor.professionID.HasValue ?
                    (loc?.GetProfessionTranslation(executor.professionID.Value) ?? "—") : "—";
                string fullName = (loc?.CurrentLanguage == "en" && loc != null) ? loc.Transliterate(executor.name) : executor.name;

                stats.Add(new ExecutorStat
                {
                    UserID = executor.userID,
                    FullName = fullName,
                    Profession = profession,
                    TotalRequests = total,
                    AssignedRequests = assigned,
                    InProgressRequests = inProgress,
                    CompletedRequests = completed,
                    CompletionPercent = completionPercent.ToString("F1") + "%",
                    LoadPercent = loadPercent.ToString("F1") + "%"
                });
            }
            return stats.OrderBy(s => s.FullName).ToList();
        }

        /// <summary>
        /// Получить статистику по клиенту за период
        /// </summary>
        public static ClientStat GetClientStat(User client, DateTime startDate, DateTime endDate)
        {
            List<Request> requests = ConnectObject.GetConnect().Requests.Where(r => r.clientID == client.userID && (startDate == DateTime.MinValue || r.createdAt >= startDate) && (endDate == DateTime.MaxValue || r.createdAt <= endDate)).ToList();

            int total = requests.Count;
            int active = requests.Count(r => r.requestStatusID >= 1 && r.requestStatusID <= 4);
            return new ClientStat
            {
                TotalRequests = total,
                ActiveRequests = active
            };
        }

        /// <summary>
        /// Получить статистику по менеджеру (количество заявок в обработке)
        /// </summary>
        public static int GetManagerStat(DateTime startDate, DateTime endDate)
        {
            return ConnectObject.GetConnect().Requests.Count(r => r.requestStatusID >= 1 && r.requestStatusID <= 4 && (startDate == DateTime.MinValue || r.createdAt >= startDate) && (endDate == DateTime.MaxValue || r.createdAt <= endDate));
        }
    }

    public class ClientStat
    {
        public int TotalRequests { get; set; }
        public int ActiveRequests { get; set; }
    }
}
