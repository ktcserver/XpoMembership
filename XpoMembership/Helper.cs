/* 
 * Author: Elvin Chen
 * Email:  isilcala@gmail.com
 * (c) 2010
 * Tis code is provided "as is", without warranty of any kind.
 * Any damage caused by this software is responsibility of the developer who use it.
 * */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using DevExpress.Xpo;

namespace XpoMembership
{
    internal static class Helper
    {
        internal static string GetConfigValue(string configValue, string defaultValue)
        {
            return string.IsNullOrEmpty(configValue) ? defaultValue : configValue;
        }

        internal static string[] GetRoleNameStrings(XPView xpvRoles)
        {
            List<string> rolesList = new List<string>();
            for (int i = 0; i < xpvRoles.Count; i++)
            {
                rolesList.Add(xpvRoles[i][0].ToString());
            }
            return rolesList.ToArray();
        }

        internal static string[] GetUserNameStrings(XPView xpvUsers)
        {
            List<string> usersList = new List<string>();
            for (int i = 0; i < xpvUsers.Count; i++)
            {
                usersList.Add(xpvUsers[i][0].ToString());
            }
            return usersList.ToArray();
        }


        internal static void WriteToEventLog(Exception e, string action, string eventSource, string eventLog)
        {
            using (EventLog log = new EventLog { Source = eventSource, Log = eventLog })
            {
                string message = String.Format("An exception occurred communicating with the data source.{0}{0}Action: {1}{0}{0}Exception: {2}", Environment.NewLine, action, e);
                log.WriteEntry(message);
            }
        }

        internal static IDataLayer DataLayer
        {
            get
            {
                return XpoDefault.DataLayer;
            }
        }

    }
}
