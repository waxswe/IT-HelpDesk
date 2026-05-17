using IT_HelpDesk.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IT_HelpDesk.Data.Classes
{
    internal class ConnectObject
    {
        public static ITHelpDeskEntities connect;

        public static ITHelpDeskEntities GetConnect()
        {
            if (connect == null) connect = new ITHelpDeskEntities();
            return connect;
        }
    }
}
