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

    public class XpoRole : BaseObject
    {
        public XpoRole()
            : base()
        {
            // This constructor is used when an object is loaded from a persistent storage.
            // Do not place any code here.
        }

        public XpoRole(Session session)
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
            RoleID = Guid.NewGuid();
            ApplicationName = Roles.ApplicationName;
            CreationDate = DateTime.Now;
            Description = string.Empty;
        }

        #region Standard Fields
        private Guid _RoleID;
        [Key(true)]
        public Guid RoleID
        {
            get
            {
                return _RoleID;
            }
            set
            {
                SetPropertyValue("RoleID", ref _RoleID, value);
            }
        }

        private string _ApplicationName;
        [Size(256)]
        [Indexed("RoleName", Unique = true)]
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

        private string _RoleName;
        [Size(256)]
        [Indexed("ApplicationName", Unique = true)]
        public string RoleName
        {
            get
            {
                return _RoleName;
            }
            set
            {
                SetPropertyValue("RoleName", ref _RoleName, value);
            }
        }

        private string _Description;
        [Size(256)]
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                SetPropertyValue("Description", ref _Description, value);
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
        #endregion

        [Association("XpoRole-XpoUsers")]
        public XPCollection<XpoUser> Users
        {
            get
            {
                return GetCollection<XpoUser>("Users");
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
        //Created/Updated: Îå 09-ËÄÔÂ-2010 16:57:26
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
            public DevExpress.Data.Filtering.OperandProperty RoleID
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("RoleID"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty ApplicationName
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("ApplicationName"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty RoleName
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("RoleName"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty Description
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("Description"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty CreationDate
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("CreationDate"));
                }
            }
            public DevExpress.Data.Filtering.OperandProperty Users
            {
                get
                {
                    return new DevExpress.Data.Filtering.OperandProperty(GetNestedName("Users"));
                }
            }
        }
        #endregion
    }

}