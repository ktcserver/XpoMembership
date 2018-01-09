/* 
 * Author: Elvin Chen
 * Email:  isilcala@gmail.com
 * (c) 2010
 * Tis code is provided "as is", without warranty of any kind.
 * Any damage caused by this software is responsibility of the developer who use it.
 * */
using System;
using System.Web.Security;
using DevExpress.Xpo;

namespace XpoMembership
{

    public class XpoUser : BaseObject
    {
        public XpoUser()
            : base()
        {
            // This constructor is used when an object is loaded from a persistent storage.
            // Do not place any code here.
        }

        public XpoUser(Session session)
            : base(session)
        {
            // This constructor is used when an object is loaded from a persistent storage.
            // Do not place any code here.
        }

        public override void AfterConstruction()
        {
            base.AfterConstruction();
            // Place here your initialization code.

            if (this.Session.IsNewObject(this))
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            DateTime creationDate = DateTime.Now;
            DateTime minDate = DateTime.MinValue;
            string nullString = null;

            UserID = Guid.NewGuid();
            ApplicationName = Membership.ApplicationName;
            UserName = String.Empty;
            Password = String.Empty;
            Email = nullString;
            CreationDate = creationDate;
            LastPasswordChangedDate = creationDate;
            LastActivityDate = creationDate;
            IsApproved = true;
            Comment = nullString;
            IsLockedOut = false;
            LastLockedOutDate = minDate;
            LastLoginDate = creationDate;
            PasswordQuestion = nullString;
            PasswordAnswer = nullString;
            FailedPasswordAnswerAttemptCount = 0;
            FailedPasswordAnswerAttemptWindowStart = minDate;
            FailedPasswordAttemptCount = 0;
            FailedPasswordAttemptWindowStart = minDate;
        }

        #region Standard Fields
        private Guid _UserID;
        [Key(true)]
        public Guid UserID
        {
            get
            {
                return _UserID;
            }
            set
            {
                SetPropertyValue("UserID", ref _UserID, value);
            }
        }

        private string _ApplicationName;
        [Size(255)]
        [Indexed("UserName", Unique = true)]
        public string ApplicationName
        {
            get
            {
                return _ApplicationName;
            }
            set
            {
                SetPropertyValue("ApplicationName", ref _ApplicationName, value);
            }
        }

        private string _UserName;
        [Size(255)]
        [Indexed("ApplicationName", Unique = true)]
        public string UserName
        {
            get
            {
                return _UserName;
            }
            set
            {
                SetPropertyValue("UserName", ref _UserName, value);
            }
        }

        private string _Email;
        [Size(128)]
        public string Email
        {
            get
            {
                return _Email;
            }
            set
            {
                SetPropertyValue("Email", ref _Email, value);
            }
        }

        private string _Comment;
        [Size(SizeAttribute.Unlimited)]
        public string Comment
        {
            get
            {
                return _Comment;
            }
            set
            {
                SetPropertyValue("Comment", ref _Comment, value);
            }
        }

        private string _Password;
        [Size(128)]
        public string Password
        {
            get
            {
                return _Password;
            }
            set
            {
                SetPropertyValue("Password", ref _Password, value);
            }
        }

        private string _PasswordQuestion;
        [Size(255)]
        public string PasswordQuestion
        {
            get
            {
                return _PasswordQuestion;
            }
            set
            {
                SetPropertyValue("PasswordQuestion", ref _PasswordQuestion, value);
            }
        }

        private string _PasswordAnswer;
        [Size(255)]
        public string PasswordAnswer
        {
            get
            {
                return _PasswordAnswer;
            }
            set
            {
                SetPropertyValue("PasswordAnswer", ref _PasswordAnswer, value);
            }
        }

        private bool _IsApproved;
        public bool IsApproved
        {
            get
            {
                return _IsApproved;
            }
            set
            {
                SetPropertyValue("IsApproved", ref _IsApproved, value);
            }
        }

        private DateTime _LastActivityDate;
        public DateTime LastActivityDate
        {
            get
            {
                return _LastActivityDate;
            }
            set
            {
                SetPropertyValue("LastActivityDate", ref _LastActivityDate, value);
            }
        }

        private DateTime _LastLoginDate;
        public DateTime LastLoginDate
        {
            get
            {
                return _LastLoginDate;
            }
            set
            {
                SetPropertyValue("LastLoginDate", ref _LastLoginDate, value);
            }
        }

        private DateTime _LastPasswordChangedDate;
        public DateTime LastPasswordChangedDate
        {
            get
            {
                return _LastPasswordChangedDate;
            }
            set
            {
                SetPropertyValue("LastPasswordChangedDate", ref _LastPasswordChangedDate, value);
            }
        }

        private DateTime _CreationDate;
        public DateTime CreationDate
        {
            get
            {
                return _CreationDate;
            }
            set
            {
                SetPropertyValue("CreationDate", ref _CreationDate, value);
            }
        }

        private bool _IsOnline;
        public bool IsOnline
        {
            get
            {
                return _IsOnline;
            }
            set
            {
                SetPropertyValue("IsOnline", ref _IsOnline, value);
            }
        }

        private bool _IsLockedOut;
        public bool IsLockedOut
        {
            get
            {
                return _IsLockedOut;
            }
            set
            {
                SetPropertyValue("IsLockedOut", ref _IsLockedOut, value);
            }
        }

        private DateTime _LastLockedOutDate;
        public DateTime LastLockedOutDate
        {
            get
            {
                return _LastLockedOutDate;
            }
            set
            {
                SetPropertyValue("LastLockedOutDate", ref _LastLockedOutDate, value);
            }
        }

        private int _FailedPasswordAttemptCount;
        public int FailedPasswordAttemptCount
        {
            get
            {
                return _FailedPasswordAttemptCount;
            }
            set
            {
                SetPropertyValue("FailedPasswordAttemptCount", ref _FailedPasswordAttemptCount, value);
            }
        }

        private DateTime _FailedPasswordAttemptWindowStart;
        public DateTime FailedPasswordAttemptWindowStart
        {
            get
            {
                return _FailedPasswordAttemptWindowStart;
            }
            set
            {
                SetPropertyValue("FailedPasswordAttemptWindowStart", ref _FailedPasswordAttemptWindowStart, value);
            }
        }

        private int _FailedPasswordAnswerAttemptCount;
        public int FailedPasswordAnswerAttemptCount
        {
            get
            {
                return _FailedPasswordAnswerAttemptCount;
            }
            set
            {
                SetPropertyValue("FailedPasswordAnswerAttemptCount", ref _FailedPasswordAnswerAttemptCount, value);
            }
        }

        private DateTime _FailedPasswordAnswerAttemptWindowStart;
        public DateTime FailedPasswordAnswerAttemptWindowStart
        {
            get
            {
                return _FailedPasswordAnswerAttemptWindowStart;
            }
            set
            {
                SetPropertyValue("FailedPasswordAnswerAttemptWindowStart", ref _FailedPasswordAnswerAttemptWindowStart, value);
            }
        }
        #endregion

        [Association("XpoRole-XpoUsers")]
        public XPCollection<XpoRole> Roles
        {
            get
            {
                return GetCollection<XpoRole>("Roles");
            }
        }

        #region EasyFields
        private static FieldsClass _fields;
        public new static FieldsClass Fields
        {
            get
            {
                if (ReferenceEquals(_fields, null))
                    _fields = new FieldsClass();
                return _fields;
            }
        }
        //Created/Updated: Îå 09-ËÄÔÂ-2010 17:01:17
        public new class FieldsClass : XPCustomObject.FieldsClass
        {
            public FieldsClass()
                : base()
            {
            }
            public FieldsClass(string propertyName)
                : base(propertyName)
            {
            }
            public DevExpress.Data.Filtering.OperandProperty UserID
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("UserID"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty ApplicationName
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("ApplicationName"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty UserName
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("UserName"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty Email
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("Email"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty Comment
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("Comment"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty Password
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("Password"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty PasswordQuestion
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("PasswordQuestion"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty PasswordAnswer
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("PasswordAnswer"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty IsApproved
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("IsApproved"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty LastActivityDate
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("LastActivityDate"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty LastLoginDate
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("LastLoginDate"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty LastPasswordChangedDate
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("LastPasswordChangedDate"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty CreationDate
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("CreationDate"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty IsOnline
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("IsOnline"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty IsLockedOut
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("IsLockedOut"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty LastLockedOutDate
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("LastLockedOutDate"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty FailedPasswordAttemptCount
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("FailedPasswordAttemptCount"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty FailedPasswordAttemptWindowStart
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("FailedPasswordAttemptWindowStart"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty FailedPasswordAnswerAttemptCount
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("FailedPasswordAnswerAttemptCount"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty FailedPasswordAnswerAttemptWindowStart
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("FailedPasswordAnswerAttemptWindowStart"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty Roles
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("Roles"));
                }
            }
        }
        #endregion
    }

}