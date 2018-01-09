using System;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata.Helpers;
using DevExpress.Xpo.Metadata;

namespace XpoMembership
{
    [NonPersistent()]
    public class BaseObject : XPBaseObject
    {
        public BaseObject()
            : base()
        {
            // This constructor is used when an object is loaded from a persistent storage.
            // Do not place any code here.
        }

        public BaseObject(Session session)
            : base(session)
        {
            // This constructor is used when an object is loaded from a persistent storage.
            // Do not place any code here. 
        }

        public override void AfterConstruction()
        {
            // Place here your initialization code.
            base.AfterConstruction();
        }

        private MemberInfoCollection _ChangedMembers;
        public MemberInfoCollection ChangedMembers
        {
            get
            {
                if (_ChangedMembers == null)
                {
                    _ChangedMembers = new MemberInfoCollection(ClassInfo);
                }
                return _ChangedMembers;
            }
        }

        protected override void OnChanged(string propertyName, object oldValue, object newValue)
        {
            base.OnChanged(propertyName, oldValue, newValue);

            if (!this.IsLoading)
            {
                XPMemberInfo Member = ClassInfo.GetPersistentMember(propertyName);
                if (Member != null && !ChangedMembers.Contains(Member))
                {
                    ChangedMembers.Add(ClassInfo.GetMember(propertyName));
                }
            }
        }

        protected override void OnSaved()
        {
            base.OnSaved();

            if (Session is NestedUnitOfWork)
            {
                BaseObject parentitem = ((NestedUnitOfWork)Session).GetParentObject(this);
                foreach (XPMemberInfo ChangedProperty in ChangedMembers)
                {
                    if (!parentitem.ChangedMembers.Contains(ChangedProperty))
                    {
                        parentitem.ChangedMembers.Add(ChangedProperty);
                    }
                }
            }
            ChangedMembers.Clear();
        }
    }
}