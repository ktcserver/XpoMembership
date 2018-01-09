/* 
 * Author: Elvin Chen
 * Email:  isilcala@gmail.com
 * (c) 2010
 * Tis code is provided "as is", without warranty of any kind.
 * Any damage caused by this software is responsibility of the developer who use it.
 * */
using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;
using System.Web.Security;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using System.Text.RegularExpressions;

namespace XpoMembership
{
    public sealed class XpoMembershipProvider : MembershipProvider
    {
        #region Class Variables
        private const string eventSource = "XpoMembershipProvider";
        private const string eventLog = "Application";
        private const string exceptionMessage = "An exception occurred. Please check the Event Log.";
        private bool enablePasswordReset;
        private bool enablePasswordRetrieval;
        private bool requiresQuestionAndAnswer;
        private bool requiresUniqueEmail;
        private int maxInvalidPasswordAttempts;
        private int passwordAttemptWindow;
        private MembershipPasswordFormat passwordFormat;
        private int minRequiredNonAlphanumericCharacters;
        private int minRequiredPasswordLength;
        private string passwordStrengthRegularExpression;
        private MachineKeySection machineKey;
        #endregion

        #region Properties

        public static IDataLayer DataLayer
        {
            get
            {
                return Helper.DataLayer;
            }
        }

        public override string ApplicationName { get; set; }

        public override bool EnablePasswordReset
        {
            get
            {
                return enablePasswordReset;
            }
        }

        public override bool EnablePasswordRetrieval
        {
            get
            {
                return enablePasswordRetrieval;
            }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get
            {
                return requiresQuestionAndAnswer;
            }
        }

        public override bool RequiresUniqueEmail
        {
            get
            {
                return requiresUniqueEmail;
            }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get
            {
                return maxInvalidPasswordAttempts;
            }
        }

        public override int PasswordAttemptWindow
        {
            get
            {
                return passwordAttemptWindow;
            }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get
            {
                return passwordFormat;
            }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get
            {
                return minRequiredNonAlphanumericCharacters;
            }
        }

        public override int MinRequiredPasswordLength
        {
            get
            {
                return minRequiredPasswordLength;
            }
        }

        public override string PasswordStrengthRegularExpression
        {
            get
            {
                return passwordStrengthRegularExpression;
            }
        }

        public bool WriteExceptionsToEventLog { get; set; }
        #endregion

        #region Enums

        private enum FailureType
        {
            Password = 1,
            PasswordAnswer = 2
        }

        #endregion

        #region Override Methods

        public override bool ChangePassword(string userName, string oldPassword, string newPassword)
        {
            if (!ValidateUser(userName, oldPassword))
            {
                return false;
            }

            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(userName, newPassword, true);

            OnValidatingPassword(args);

            if (args.Cancel)
            {
                if (args.FailureInformation != null)
                {
                    throw args.FailureInformation;
                }
                else
                {
                    throw new Exception("Change password canceled due to new password validation failure.");
                }
            }

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser user = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal)));
                    if (user != null)
                    {
                        user.Password = EncodePassword(newPassword);
                        user.LastPasswordChangedDate = DateTime.Now;
                    }
                    else
                    {
                        return false;
                    }
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "ChangePassword");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return true;
        }

        public override bool ChangePasswordQuestionAndAnswer(string userName, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            if (!ValidateUser(userName, password))
            {
                return false;
            }

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser user = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal)));
                    if (user == null)
                    {
                        return false;
                    }
                    user.PasswordQuestion = newPasswordQuestion;
                    user.PasswordAnswer = EncodePassword(newPasswordAnswer);
                    uow.CommitChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "ChangePasswordQuestionAndAnswer");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }
        }

        public override MembershipUser CreateUser(string userName, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(userName, password, true);

            OnValidatingPassword(args);

            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (requiresQuestionAndAnswer && String.IsNullOrEmpty(passwordAnswer))
            {
                status = MembershipCreateStatus.InvalidAnswer;
                return null;
            }

            if (RequiresUniqueEmail)
            {
                if (!IsEmail(email))
                {
                    status = MembershipCreateStatus.InvalidEmail;
                    return null;
                }
                if (!String.IsNullOrEmpty(GetUserNameByEmail(email)))
                {
                    status = MembershipCreateStatus.DuplicateEmail;
                    return null;
                }
            }

            MembershipUser mUser = GetUser(userName, false);

            if (mUser != null)
            {
                status = MembershipCreateStatus.DuplicateUserName;
                return null;
            }

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    DateTime creationDate = DateTime.Now;
                    DateTime minDate = DateTime.MinValue;

                    XpoUser xUser = new XpoUser(uow)
                    {
                        ApplicationName = this.ApplicationName,
                        UserName = userName,
                        Password = EncodePassword(password),
                        Email = String.IsNullOrEmpty(email) ? String.Empty : email,
                        PasswordQuestion = passwordQuestion,
                        PasswordAnswer = EncodePassword(passwordAnswer),
                        IsApproved = isApproved,
                        CreationDate = creationDate,
                        LastPasswordChangedDate = creationDate,
                        LastActivityDate = creationDate,
                        IsLockedOut = false,
                        LastLockedOutDate = minDate,
                        LastLoginDate = creationDate,
                        FailedPasswordAnswerAttemptCount = 0,
                        FailedPasswordAnswerAttemptWindowStart = minDate,
                        FailedPasswordAttemptCount = 0,
                        FailedPasswordAttemptWindowStart = minDate,
                    };
                    uow.CommitChanges();
                    status = MembershipCreateStatus.Success;
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "CreateUser");
                }

                status = MembershipCreateStatus.ProviderError;
            }

            return GetUser(userName, false);
        }

        public override bool DeleteUser(string userName, bool deleteAllRelatedData)
        {
            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser user = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal)));
                    if (user == null)
                    {
                        return false;
                    }
                    uow.Delete(user);
                    uow.CommitChanges();
                }
                return true;
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "DeleteUser");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            emailToMatch = emailToMatch.Trim();

            MembershipUserCollection mclUsers = new MembershipUserCollection();

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPCollection<XpoUser> xpcUsers = new XPCollection<XpoUser>(uow, new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.Email, emailToMatch, BinaryOperatorType.Like)),
                        new SortProperty(XpoUser.Fields.UserName, DevExpress.Xpo.DB.SortingDirection.Ascending));

                    totalRecords = xpcUsers.Count;
                    int startIndex = pageSize * pageIndex;
                    int endIndex = startIndex + pageSize;
                    endIndex = totalRecords > endIndex ? endIndex : totalRecords;
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        MembershipUser mUser = GetUserFromXpoUser(xpcUsers[i]);
                        mclUsers.Add(mUser);
                    }
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "FindUsersByEmail");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return mclUsers;
        }

        public override MembershipUserCollection FindUsersByName(string userNameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            userNameToMatch = userNameToMatch.Trim();

            MembershipUserCollection mclUsers = new MembershipUserCollection();

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPCollection<XpoUser> xpcUsers = new XPCollection<XpoUser>(uow, new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userNameToMatch, BinaryOperatorType.Like)),
                        new SortProperty(XpoUser.Fields.UserName, DevExpress.Xpo.DB.SortingDirection.Ascending));

                    totalRecords = xpcUsers.Count;
                    int startIndex = pageSize * pageIndex;
                    int endIndex = startIndex + pageSize;
                    endIndex = totalRecords > endIndex ? endIndex : totalRecords;
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        MembershipUser mUser = GetUserFromXpoUser(xpcUsers[i]);
                        mclUsers.Add(mUser);
                    }
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "FindUsersByName");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }
            
            return mclUsers;
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection mclUsers = new MembershipUserCollection();

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPCollection<XpoUser> xpcUsers = new XPCollection<XpoUser>(uow,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new SortProperty(XpoUser.Fields.UserName, DevExpress.Xpo.DB.SortingDirection.Ascending));

                    totalRecords = xpcUsers.Count;
                    int startIndex = pageSize * pageIndex;
                    int endIndex = startIndex + pageSize;
                    endIndex = totalRecords > endIndex ? endIndex : totalRecords;
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        MembershipUser mUser = GetUserFromXpoUser(xpcUsers[i]);
                        mclUsers.Add(mUser);
                    }
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "GetAllUsers");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return mclUsers;
        }

        public override int GetNumberOfUsersOnline()
        {
            TimeSpan onlineSpan = new TimeSpan(0, Membership.UserIsOnlineTimeWindow, 0);
            DateTime compareTime = DateTime.Now.Subtract(onlineSpan);

            int numOnline;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    numOnline = (int)uow.Evaluate<XpoUser>(
                        CriteriaOperator.Parse("Count()"),
                        new GroupOperator(
                            GroupOperatorType.And,
                            new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                            new BinaryOperator(XpoUser.Fields.LastActivityDate, compareTime, BinaryOperatorType.Greater)));
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "GetNumberOfUsersOnline");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return numOnline;
        }

        public override string GetPassword(string userName, string answer)
        {
            if (!EnablePasswordRetrieval)
            {
                throw new ProviderException("Password Retrieval Not Enabled.");
            }

            if (PasswordFormat == MembershipPasswordFormat.Hashed)
            {
                throw new ProviderException("Cannot retrieve Hashed passwords.");
            }

            string password;
            string passwordAnswer;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser user = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal)));

                    if (user == null)
                    {
                        throw new MembershipPasswordException("The specified user is not found.");
                    }
                    if (user.IsLockedOut)
                    {
                        throw new MembershipPasswordException("The specified user is locked out.");
                    }
                    password = user.Password;
                    passwordAnswer = user.PasswordAnswer;
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "GetPassword");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            if (RequiresQuestionAndAnswer && !CheckPassword(answer, passwordAnswer))
            {
                UpdateFailureCount(userName, FailureType.PasswordAnswer);

                throw new MembershipPasswordException("Incorrect password answer.");
            }

            if (PasswordFormat == MembershipPasswordFormat.Encrypted)
            {
                password = DecodePassword(password);
            }

            return password;
        }

        public override MembershipUser GetUser(string userName, bool userIsOnline)
        {
            MembershipUser mUser;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser xUser = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal)));

                    if (xUser == null)
                    {
                        return null;
                    }

                    mUser = GetUserFromXpoUser(xUser);

                    if (userIsOnline)
                    {
                        xUser.LastActivityDate = DateTime.Now;
                    }
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "GetUser(String, Boolean)");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return mUser;
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            MembershipUser mUser;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser xUser = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserID, new OperandValue(providerUserKey), BinaryOperatorType.Equal)));

                    if (xUser == null)
                    {
                        return null;
                    }

                    mUser = GetUserFromXpoUser(xUser);
                    if (userIsOnline)
                    {
                        xUser.LastActivityDate = DateTime.Now;
                    }
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "GetUser(Object, Boolean)");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return mUser;
        }

        public override string GetUserNameByEmail(string email)
        {
            string userName;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XPView xpvUser = new XPView(uow, typeof(XpoUser), 
                        new CriteriaOperatorCollection(){XpoUser.Fields.UserName}, 
                        new GroupOperator(
                            GroupOperatorType.And,
                            new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                            new BinaryOperator(XpoUser.Fields.Email, email, BinaryOperatorType.Equal)));

                    if (xpvUser.Count > 0)
                    {
                        userName = xpvUser[0][XpoUser.Fields.UserName.PropertyName].ToString();
                        if (String.IsNullOrEmpty(userName))
                        {
                            userName = String.Empty;
                        }
                    }
                    else
                    {
                        userName = null;
                    }
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "GetUserNameByEmail");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return userName;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (String.IsNullOrEmpty(name))
            {
                name = "XpoMembershipProvider";
            }

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Xpo Membership provider");
            }

            //Initialize the abstract base class.
            base.Initialize(name, config);

            ApplicationName = Helper.GetConfigValue(config["applicationName"],
                System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            if (ApplicationName == null) ApplicationName = String.Empty;
            maxInvalidPasswordAttempts = Convert.ToInt32(Helper.GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            passwordAttemptWindow = Convert.ToInt32(Helper.GetConfigValue(config["passwordAttemptWindow"], "10"));
            minRequiredNonAlphanumericCharacters = Convert.ToInt32(Helper.GetConfigValue(config["minRequiredAlphaNumericCharacters"], "1"));
            minRequiredPasswordLength = Convert.ToInt32(Helper.GetConfigValue(config["minRequiredPasswordLength"], "7"));
            passwordStrengthRegularExpression = Convert.ToString(Helper.GetConfigValue(config["passwordStrengthRegularExpression"], String.Empty));
            enablePasswordReset = Convert.ToBoolean(Helper.GetConfigValue(config["enablePasswordReset"], "true"));
            enablePasswordRetrieval = Convert.ToBoolean(Helper.GetConfigValue(config["enablePasswordRetrieval"], "true"));
            requiresQuestionAndAnswer = Convert.ToBoolean(Helper.GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
            requiresUniqueEmail = Convert.ToBoolean(Helper.GetConfigValue(config["requiresUniqueEmail"], "true"));

            string temp_format = config["passwordFormat"];
            if (temp_format == null)
            {
                temp_format = "Hashed";
            }

            switch (temp_format)
            {
                case "Hashed":
                    passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
                case "Encrypted":
                    passwordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Clear":
                    passwordFormat = MembershipPasswordFormat.Clear;
                    break;
                default:
                    throw new ProviderException("Password format not supported.");
            }

            //Get encryption and decryption key information from the configuration.
            System.Configuration.Configuration cfg = WebConfigurationManager.OpenWebConfiguration(
                System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            machineKey = cfg.GetSection("system.web/machineKey") as MachineKeySection;

            if (machineKey.ValidationKey.Contains("AutoGenerate"))
            {
                if (PasswordFormat != MembershipPasswordFormat.Clear)
                {
                    throw new ProviderException("Hashed or Encrypted passwords are not supported with auto-generated keys.");
                }
            }
        }

        public override string ResetPassword(string userName, string answer)
        {
            if (!EnablePasswordReset)
            {
                throw new NotSupportedException("Password Reset is not enabled.");
            }

            if ((answer == null) && (RequiresQuestionAndAnswer))
            {
                UpdateFailureCount(userName, FailureType.PasswordAnswer);
                throw new ProviderException("Password answer required for password Reset.");
            }

            int minPasswordLenth = MinRequiredPasswordLength > 8 ? MinRequiredPasswordLength : 8;
            string newPassword = Membership.GeneratePassword(minPasswordLenth,
                MinRequiredNonAlphanumericCharacters);

            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(userName, newPassword, true);

            OnValidatingPassword(args);

            if (args.Cancel)
            {
                if (args.FailureInformation != null)
                {
                    throw args.FailureInformation;
                }
                else
                {
                    throw new MembershipPasswordException("Reset password canceled due to password answer validation failure.");
                }
            }

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser user = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal)));

                    if (user == null)
                    {
                        throw new MembershipPasswordException("The specified user is not found.");
                    }
                    if (user.IsLockedOut)
                    {
                        throw new MembershipPasswordException("The specified user is locked out.");
                    }

                    if (RequiresQuestionAndAnswer && (!CheckPassword(answer, user.PasswordAnswer)))
                    {
                        UpdateFailureCount(userName, FailureType.PasswordAnswer);

                        throw new MembershipPasswordException("Incorrect password answer.");
                    }

                    user.Password = EncodePassword(newPassword);
                    user.LastPasswordChangedDate = DateTime.Now;
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "ResetPassword");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return newPassword;
        }

        public override bool UnlockUser(string userName)
        {
            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser user = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal)));

                    if (user == null)
                    {
                        return false;
                    }

                    user.IsLockedOut = false;
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "UnlockUser");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return true;
        }

        public override void UpdateUser(MembershipUser mUser)
        {
            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser xUser = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, mUser.UserName, BinaryOperatorType.Equal)));

                    if (xUser == null)
                    {
                        throw new ProviderException("The specified user is not found.");
                    }

                    xUser.Email = mUser.Email;
                    xUser.Comment = mUser.Comment;
                    xUser.IsApproved = mUser.IsApproved;
                    xUser.LastLoginDate = mUser.LastLoginDate;
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "UpdateUser");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }
        }

        public override bool ValidateUser(string userName, string password)
        {
            bool isValid = false;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser user = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal)));

                    if (user == null)
                    {
                        return false;
                    }

                    if (CheckPassword(password, user.Password))
                    {
                        if((!user.IsLockedOut) && (user.IsApproved))
                        {
                            isValid = true;
                            user.LastLoginDate = DateTime.Now;
                            uow.CommitChanges();
                        }
                    }
                    else
                    {
                        UpdateFailureCount(userName, FailureType.Password);
                    }
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "ValidateUser");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }

            return isValid;
        }
        #endregion

        #region "Utility Functions"

        private MembershipUser GetUserFromXpoUser(XpoUser xUser)
        {
            MembershipUser mUser = new MembershipUser(
                this.Name, 
                xUser.UserName,
                xUser.UserID,
                xUser.Email,
                xUser.PasswordQuestion,
                xUser.Comment,
                xUser.IsApproved,
                xUser.IsLockedOut,
                xUser.CreationDate,
                xUser.LastLoginDate,
                xUser.LastActivityDate,
                xUser.LastPasswordChangedDate,
                xUser.LastLockedOutDate
                );
            return mUser;

        }

        private void UpdateFailureCount(string userName, FailureType failureType)
        {
            DateTime windowStart;
            DateTime windowEnd;
            int failureCount;

            try
            {
                using (XtraUnitOfWork uow = new XtraUnitOfWork(DataLayer))
                {
                    XpoUser user = uow.FindObject<XpoUser>(new GroupOperator(
                        GroupOperatorType.And,
                        new BinaryOperator(XpoUser.Fields.ApplicationName, ApplicationName, BinaryOperatorType.Equal),
                        new BinaryOperator(XpoUser.Fields.UserName, userName, BinaryOperatorType.Equal)));

                    switch (failureType)
                    {
                        case FailureType.Password:
                            failureCount = user.FailedPasswordAttemptCount;
                            windowStart = user.FailedPasswordAttemptWindowStart;
                            windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

                            user.FailedPasswordAttemptWindowStart = DateTime.Now;

                            if (DateTime.Now > windowEnd)
                            {
                                user.FailedPasswordAttemptCount = 1;
                            }
                            else
                            {
                                user.FailedPasswordAttemptCount++;
                            }

                            if (user.FailedPasswordAttemptCount >= MaxInvalidPasswordAttempts)
                            {
                                if (!user.IsLockedOut)
                                {
                                    user.LastLockedOutDate = DateTime.Now;
                                    user.IsLockedOut = true;
                                }
                            }
                            break;

                        case FailureType.PasswordAnswer:
                            failureCount = user.FailedPasswordAnswerAttemptCount;
                            windowStart = user.FailedPasswordAnswerAttemptWindowStart;
                            windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

                            user.FailedPasswordAnswerAttemptWindowStart = DateTime.Now;

                            if (DateTime.Now > windowEnd)
                            {
                                user.FailedPasswordAnswerAttemptCount = 1;
                            }
                            else
                            {
                                user.FailedPasswordAnswerAttemptCount++;
                            }

                            if (user.FailedPasswordAnswerAttemptCount >= MaxInvalidPasswordAttempts)
                            {
                                if (!user.IsLockedOut)
                                {
                                    user.LastLockedOutDate = DateTime.Now;
                                    user.IsLockedOut = true;
                                }
                            }
                            break;
                    }
                    uow.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(ex, "UpdateFailureCount");

                    throw new ProviderException(exceptionMessage);
                }
                else
                {
                    throw ex;
                }
            }
        }

        private bool CheckPassword(string password, string dbpassword)
        {
            string pass1 = password;
            string pass2 = dbpassword;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    pass2 = DecodePassword(dbpassword);
                    break;
                case MembershipPasswordFormat.Hashed:
                    pass1 = EncodePassword(password);
                    break;
                default:
                    break;
            }

            return pass1 == pass2;
        }

        private string EncodePassword(string password)
        {
            string encodedPassword = password;

            if (String.IsNullOrEmpty(encodedPassword))
            {
                return encodedPassword;
            }

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    encodedPassword =
                      Convert.ToBase64String(EncryptPassword(Encoding.Unicode.GetBytes(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    HMACSHA1 hash = new HMACSHA1 { Key = HexToByte(machineKey.ValidationKey) };
                    encodedPassword =
                      Convert.ToBase64String(hash.ComputeHash(Encoding.Unicode.GetBytes(password)));
                    break;
                default:
                    throw new ProviderException("Unsupported password format.");
            }

            return encodedPassword;
        }

        private string DecodePassword(string encodedPassword)
        {
            string password = encodedPassword;

            if (String.IsNullOrEmpty(password))
            {
                return password;
            }

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    password =
                      Encoding.Unicode.GetString(DecryptPassword(Convert.FromBase64String(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    throw new ProviderException("Cannot unencode a hashed password.");
                default:
                    throw new ProviderException("Unsupported password format.");
            }

            return password;
        }

        private static byte[] HexToByte(string hexString)
        {
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        private static void WriteToEventLog(Exception e, string action)
        {
            Helper.WriteToEventLog(e, action, eventSource, eventLog);
        }

        private static bool IsEmail(string inputEmail)
        {
            string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                  @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
            Regex re = new Regex(strRegex);
            if (re.IsMatch(inputEmail))
                return (true);
            else
                return (false);
        }
        #endregion
    }
}
