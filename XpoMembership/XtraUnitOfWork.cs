using DevExpress.Xpo;
using DevExpress.Data.Filtering;
using System;

namespace XpoMembership
{
    public class XtraUnitOfWork : UnitOfWork
    {
        public XtraUnitOfWork(DevExpress.Xpo.Metadata.XPDictionary dictionary)
            : base(dictionary)
        {
        }

        public XtraUnitOfWork(IDataLayer layer, params IDisposable[] DisposeOnDisconnect)
            : base(layer, DisposeOnDisconnect)
        {
        }

        public XtraUnitOfWork()
            : base()
        {
        }

        protected override DevExpress.Xpo.Metadata.Helpers.MemberInfoCollection GetPropertiesListForUpdateInsert(object theObject, bool isUpdate,bool addDelayedReference)
        {
            //Check if the Object is our "change tracking object"
            if ((theObject is BaseObject) && (!(((PersistentBase)theObject).Session.IsNewObject(theObject))))
            {
                DevExpress.Xpo.Metadata.XPClassInfo ci = GetClassInfo(theObject);
                //obtain the class info so we can walk through the members
                DevExpress.Xpo.Metadata.Helpers.MemberInfoCollection list = new DevExpress.Xpo.Metadata.Helpers.MemberInfoCollection(ci);
                //obtain a list of members
                int count = 0;
                
                foreach (DevExpress.Xpo.Metadata.XPMemberInfo m in base.GetPropertiesListForUpdateInsert(theObject, isUpdate,addDelayedReference))
                {
                    //If it is a servicefield this is required (OID, GCRecord etc)
                    if (m is DevExpress.Xpo.Metadata.Helpers.ServiceField | ((BaseObject)theObject).ChangedMembers.Contains(m))
                    {
                        list.Add(m);
                    }
                }

                //return out list of "changed" members (plus the service fields)
                return list;
            }
            else
            {
                return base.GetPropertiesListForUpdateInsert(theObject, isUpdate,addDelayedReference);
            }
        }
    }
}