using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using DevExpress.Xpo.DB;
using DevExpress.Xpo;
using System.Configuration;

namespace XpoMembershipDemo
{
    public class Global : System.Web.HttpApplication
    {

        void Application_Start(object sender, EventArgs e)
        {
            // 更新数据库结构
            // 因为需要较高的数据库帐户权限
            // 出于安全性考虑的话，可以另起一个项目
            // 分配一个较高权限的数据库帐户用以执行此操作
            XpoInitializer.UpdateDatabaseSchema(typeof(XpoMembership.XpoUser).Assembly);

            // 创建并获取一个线程安全的XpoDataLayer
            IDataLayer dataLayer = XpoInitializer.CreateThreadSafeDataLayer(typeof(XpoMembership.XpoUser).Assembly);

            // 上句代码执行完毕以后，新建的ThreadSafeDataLayer已经被指定给XpoDefault.DataLayer。
            // 若有需要也可以另行保存一份，便于后续调用。
            Application.Add("XpoLayer", dataLayer);
        }

        void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown

        }

        void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs

        }

        void Session_Start(object sender, EventArgs e)
        {
            // Code that runs when a new session is started

        }

        void Session_End(object sender, EventArgs e)
        {
            // Code that runs when a session ends. 
            // Note: The Session_End event is raised only when the sessionstate mode
            // is set to InProc in the Web.config file. If session mode is set to StateServer 
            // or SQLServer, the event is not raised.

        }
    }
}
