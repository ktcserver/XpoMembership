using System.Configuration;
using System.Reflection;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Metadata;

namespace XpoMembershipDemo
{
    public static class XpoInitializer
    {
        /// <summary>
        /// 调用XPO更新数据库结构。
        /// 需要在具有相应权限的数据库帐户下运行。
        /// </summary>
        /// <param name="assemblies">整个项目中所有包含了XPO持久类的程序集。如：typeof(LibraryA.ClassA).Assembly, typeof(LibraryB.ClassB).Assembly...</param>
        public static void UpdateDatabaseSchema(params Assembly[] assemblies)
        {
            string conn = GetConnectionString();
            IDataLayer datalayer = XpoDefault.GetDataLayer(conn, AutoCreateOption.DatabaseAndSchema);
            using (Session session = new Session(datalayer))
            {
                session.UpdateSchema(assemblies);
                session.CreateObjectTypeRecords(assemblies);
            }
        }

        /// <summary>
        /// 根据配置文件创建并返回一个线程安全的XpoDataLayer。
        /// 用在有多线程并发访问的场合，如ASP.NET项目。
        /// 此方法同时会将该DataLayer指定给XpoDefault.DataLayer。
        /// 并且将XpoDefault.Session置空。
        /// </summary>
        /// <param name="assemblies">整个项目中所有包含了XPO持久类的程序集。如：typeof(LibraryA.ClassA).Assembly, typeof(LibraryB.ClassB).Assembly...</param>
        /// <returns>创建完成的ThreadSafeDataLayer</returns>
        public static IDataLayer CreateThreadSafeDataLayer(params Assembly[] assemblies)
        {
            IDataLayer datalayer = GetDataLayer(assemblies);

            XpoDefault.DataLayer = datalayer;

            XpoDefault.Session = null;

            return datalayer;
        }

        /// <summary>
        /// 根据连接字符串创建 XPO 的 DataStore
        /// </summary>
        /// <returns></returns>
        static IDataStore GetDataStore()
        {
            string conn = GetConnectionString();

            IDataStore store = XpoDefault.GetConnectionProvider(conn, AutoCreateOption.SchemaAlreadyExists);

            return store;
        }

        /// <summary>
        /// 创建 XPO 的 ThreadSafeDataLayer
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        static IDataLayer GetDataLayer(params Assembly[] assemblies)
        {
            ReflectionDictionary dict = new ReflectionDictionary();
            dict.CollectClassInfos(assemblies);

            IDataLayer dataLayer;

            int maxConnections = GetMaxConnections();

            if (maxConnections > 1)
            {
                IDataStore[] stores = new IDataStore[maxConnections];
                for (int i = 0; i < maxConnections; i++)
                    stores[i] = GetDataStore();
                dataLayer = new ThreadSafeDataLayer(dict, new DataStoreFork(stores));
            }
            else
            {
                dataLayer = new ThreadSafeDataLayer(dict, GetDataStore());
            }

            return dataLayer;
        }

        /// <summary>
        /// 根据配置节获取XPO数据库最大并发连接数
        /// </summary>
        /// <returns>若配置节存在，返回配置节设置值，否则返回 1</returns>
        static int GetMaxConnections()
        {
            AppSettingsReader config = new AppSettingsReader();
            int conns;
            
            try
            {
                conns = (int)config.GetValue("DatabaseMaxConnections", typeof(int));            
            }
            catch
            {
                conns = 1;
            }
            return conns;
        }

        /// <summary>
        /// 尝试根据配置文件中在appSettings节下指定的设置构造数据库连接字符串。
        /// 若失败则尝试获取connectionStrings节下指定的第一个连接字符串。
        /// </summary>
        /// <returns>构造或获取到的数据库连接字符串</returns>
        public static string GetConnectionString()
        {
            try
            {
                AppSettingsReader config = new AppSettingsReader();
                string serverType, server, database, user, password;
                serverType = ((string)(config.GetValue("ServerType", typeof(string))));
                server = ((string)(config.GetValue("Server", typeof(string))));
                database = ((string)(config.GetValue("Database", typeof(string))));
                user = ((string)(config.GetValue("User", typeof(string))));
                password = ((string)(config.GetValue("Password", typeof(string))));

                switch (serverType.ToUpper())
                {
                    case "ACCESS":
                        return AccessConnectionProvider.GetConnectionString(database, user, password);
                    case "MSSQL":
                        return MSSqlConnectionProvider.GetConnectionString(server, user, password, database);
                    case "MYSQL":
                        return MySqlConnectionProvider.GetConnectionString(server, user, password, database);
                    case "ORACLE":
                        return OracleConnectionProvider.GetConnectionString(server, user, password);
                    // ... generate connection strings for other providers, e.g. Firebird, etc.
                    default:
                        return ConfigurationManager.ConnectionStrings[0].ToString();
                }
            }
            catch
            {
                return ConfigurationManager.ConnectionStrings[0].ToString();
            }
        }
    }
}