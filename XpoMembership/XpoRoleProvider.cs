/* 
 * Author: Elvin Chen
 * Email:  isilcala@gmail.com
 * (c) 2010
 * Tis code is provided "as is", without warranty of any kind.
 * Any damage caused by this software is responsibility of the developer who use it.
 * */
using System;
using System.Configuration.Provider;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;

namespace XpoMembership
{
    public sealed class XpoRoleProvider : System.Web.Security.RoleProvider
    {
        private const string eventSource = "XpoRoleProvider";
        private const string eventLog = "Application";
        private const string exceptionMessage = "An exception occurred. Please check the Event Log.";

        public bool WriteExceptionsToEventLog { get; set; }

        public static IDataLayer DataLayer
        {
            get
            {
                return Helper.DataLayer;
            }
        }

        public override string ApplicationName { get; set; }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (String.IsNullOrEmpty(name))
            {
                name = "XpoRoleProvider";
            }
            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Xpo Role provider");
            }
            base.Initialize(name, config);
            if (config["writeExceptionsToEventLog"] != null)
            {
                WriteExceptionsToEventLog = bool.Parse(config["writeExceptionsToEventLog"]);
            }
            ApplicationName = Helper.GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            if (ApplicationName == null) ApplicationName = String.Empty;
        }

        public override void AddUsersToRoles(string[] userNames, string[] roleNames)
        {
            foreach (string roleName in roleNames)
            {
                if (String.IsNullOrEmpty(roleName))
                    throw new ProviderException("Role name cannot be empty or null.");
                if (roleName.Contains(","))
                    throw new ArgumentException("Role name cannot contain commas.");
                if (!RoleExists(roleName))
                    throw new ProviderException("Role name not found.");
            }

            foreach (string userName in userNames)
            {
                if (String.IsNullOrEmpty(userName))
                    throw new ProviderException("User name cannot be empty or null.");
                if (userName.Contains(","))
                    throw new ArgumentException("User names cannot contain commas.");

                //foreach (string rolename in roleNames)
                //{
                //    if (IsUserInRole(userName, rolename))
                //        throw new ProviderException("User is already in role.");
                //}
            }

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPCollection<XpoUser> xpcUsers = new XPCollection<XpoUser>(uow, new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new InOperator(XpoUser.Fields.UserName.PropertyName, userNames)));
                    XPCollection<XpoRole> xpcRoles = new XPCollection<XpoRole>(uow, new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoRole.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new InOperator(XpoRole.Fields.RoleName.PropertyName, roleNames)));
                    foreach (XpoUser user in xpcUsers)
                    {
                        user.Roles.AddRange(xpcRoles);
                    }
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "AddUsersToRoles");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }
        }

        public override void CreateRole(string roleName)
        {
            CreateRole(roleName, String.Empty);
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole = true)
        {
            if (!RoleExists(roleName))
            {
                throw new ProviderException("Role does not exist.");
            }

            if (throwOnPopulatedRole && GetUsersInRole(roleName).Length > 0)
            {
                throw new ProviderException("Cannot delete a populated role.");
            }

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoRole role = uow.FindObject<XpoRole>(new GroupOperator(
                            GroupOperatorType.And, 
                            new BinaryOperator(XpoRole.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal), 
                            new BinaryOperator(XpoRole.Fields.RoleName, roleName, BinaryOperatorType.Equal)));
                    uow.Delete(role);
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "DeleteRole");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return !RoleExists(roleName);
        }

        public override string[] FindUsersInRole(string roleName, string userNameToMatch)
        {
            string[] users;

            if (!RoleExists(roleName))
            {
                throw new ProviderException("Role does not exist.");
            }

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPView xpvUsers = new XPView(uow, typeof(XpoUser), 
                        new CriteriaOperatorCollection(){XpoUser.Fields.UserName},
                        new GroupOperator(
                            GroupOperatorType.And,
                            new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                            new BinaryOperator(XpoUser.Fields.UserName, userNameToMatch, BinaryOperatorType.Like),
                            new ContainsOperator(XpoUser.Fields.Roles, new BinaryOperator(XpoRole.Fields.RoleName, roleName, BinaryOperatorType.Equal))));
                    users = Helper.GetUserNameStrings(xpvUsers);
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "FindUsersInRole");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return users;
        }

        public override string[] GetAllRoles()
        {
            string[] roles;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPView xpvRoles = new XPView(uow, typeof(XpoRole), 
                        new CriteriaOperatorCollection(){XpoRole.Fields.RoleName}, 
                        new BinaryOperator(XpoRole.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal));
                    roles = Helper.GetRoleNameStrings(xpvRoles);
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "GetAllRoles");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return roles;
        }

        public override string[] GetRolesForUser(string userName)
        {
            if (String.IsNullOrEmpty(userName))
                throw new ProviderException("User name cannot be empty or null.");

            string[] roles;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPView xpvRoles = new XPView(uow, typeof(XpoRole),
                        new CriteriaOperatorCollection() { XpoRole.Fields.RoleName }, 
                        new GroupOperator(
                            GroupOperatorType.And,
                            new BinaryOperator(XpoRole.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                            new ContainsOperator(XpoRole.Fields.Users, new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal))));
                    roles = Helper.GetRoleNameStrings(xpvRoles);
                }

            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "GetRolesForUser");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return roles;
        }

        public override string[] GetUsersInRole(string roleName)
        {
            if (String.IsNullOrEmpty(roleName))
                throw new ProviderException("Role name cannot be empty or null.");
            if (!RoleExists(roleName))
                throw new ProviderException("Role does not exist.");

            string[] users;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPView xpvUsers = new XPView(uow, typeof(XpoUser), 
                        new CriteriaOperatorCollection(){XpoUser.Fields.UserName}, 
                        new GroupOperator(
                            GroupOperatorType.And,
                            new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                            new ContainsOperator(XpoUser.Fields.Roles, new BinaryOperator(XpoRole.Fields.RoleName, roleName, BinaryOperatorType.Equal))));
                    users = Helper.GetUserNameStrings(xpvUsers);
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "GetUsersInRole");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return users;
        }

        public override bool IsUserInRole(string userName, string roleName)
        {
            if (String.IsNullOrEmpty(userName))
                throw new ProviderException("User name cannot be empty or null.");
            if (String.IsNullOrEmpty(roleName))
                throw new ProviderException("Role name cannot be empty or null.");

            int numRole;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    numRole = (int)uow.Evaluate<XpoUser>(
                        CriteriaOperator.Parse("Count()"), new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal),
                        new ContainsOperator(XpoUser.Fields.Roles, new BinaryOperator(XpoRole.Fields.RoleName, roleName, BinaryOperatorType.Equal))));
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "IsUserInRole");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return numRole > 0 ? true : false;
        }

        public override void RemoveUsersFromRoles(string[] userNames, string[] roleNames)
        {
            foreach (string rolename in roleNames)
            {
                if (String.IsNullOrEmpty(rolename))
                    throw new ProviderException("Role name cannot be empty or null.");
                if (!RoleExists(rolename))
                    throw new ProviderException("Role name not found.");
            }

            foreach (string username in userNames)
            {
                if (String.IsNullOrEmpty(username))
                    throw new ProviderException("User name cannot be empty or null.");

                foreach (string rolename in roleNames)
                {
                    if (!IsUserInRole(username, rolename))
                        throw new ProviderException("User is not in role.");
                }
            }

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPCollection<XpoRole> xpcRoles = new XPCollection<XpoRole>(uow, new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoRole.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new InOperator(XpoRole.Fields.RoleName.PropertyName, roleNames)));

                    XPCollection<XpoUser> xpcUsers;
                    foreach (XpoRole role in xpcRoles)
                    {
                        xpcUsers = new XPCollection<XpoUser>(uow, new GroupOperator(
                            GroupOperatorType.And,
                            new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                            new InOperator(XpoUser.Fields.UserName.PropertyName, userNames),
                            new ContainsOperator(XpoUser.Fields.Roles, new BinaryOperator(XpoRole.Fields.RoleName, role.RoleName, BinaryOperatorType.Equal))));
                        for (int i = xpcUsers.Count - 1; i >= 0; i--)
                        {
                            role.Users.Remove(xpcUsers[i]);
                        }
                    }
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "RemoveUsersFromRoles");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }
        }

        public override bool RoleExists(string roleName)
        {
            if (String.IsNullOrEmpty(roleName))
                throw new ProviderException("Role name cannot be empty or null.");

            int numRole;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    numRole = (int)uow.Evaluate<XpoRole>(
                        CriteriaOperator.Parse("Count()"), new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoRole.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoRole.Fields.RoleName, roleName, BinaryOperatorType.Equal)));
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "RoleExists");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return numRole > 0 ? true : false;
        }

        public void CreateRole(string roleName, string description)
        {
            if (String.IsNullOrEmpty(roleName))
                throw new ProviderException("Role name cannot be empty or null.");
            if (roleName.Contains(","))
                throw new ArgumentException("Role names cannot contain commas.");
            if (RoleExists(roleName))
                throw new ProviderException("Role name already exists.");
            if (roleName.Length > 256)
                throw new ProviderException("Role name cannot exceed 256 characters.");

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoRole role = new XpoRole(uow) { RoleName = roleName, ApplicationName = this.ApplicationName, Description = description };
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "CreateRole");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }
        }

        private static void WriteToEventLog(Exception e, string action)
        {
            Helper.WriteToEventLog(e, action, eventSource, eventLog);
        }
    }
}
