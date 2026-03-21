using NHibernate;
using NHibernate.Bytecode;
using NHibernate.Cfg;
using NHibernate.Context;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.Attributes;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Electrons.Core.Net8.Infrastructure
{
    internal class NHibernateHelper
    {
        /// <summary>
        /// ISessionFactory implementation is thread safe...
        /// thus static allows all threads to share it.
        /// It is created in the static constructor below...
        /// </summary>
        public static ISessionFactory SessionFactory;
        private static string _configuredConnectionString;
        /// <summary>
        /// Constructor creates the ISessionFactory implementation.
        /// Executes the first time the helper class is called.
        /// </summary>
        public NHibernateHelper(DatabaseConfig config)
        {
            try
            {
                _configuredConnectionString = $"User Id={config.UserId};Password={config.Password};Host={config.Host};Database={config.Database}";                
                var currentProcess = Process.GetCurrentProcess();
                string sessionContext = currentProcess.ProcessName == "w3wp" ? "web" : "thread_static";

                var cfg = new Configuration();
                cfg.DataBaseIntegration(x =>
                {
                    x.ConnectionString = _configuredConnectionString;
                    x.Dialect<MySQL55Dialect>();
                    x.Driver<MySqlDataDriver>();
                });
                cfg.Proxy(p => p.ProxyFactoryFactory<StaticProxyFactoryFactory>());
                //cfg.SetProperty(NhEnvironment.DefaultSchema, NHibernateConfig.DefaultSchema ?? "TRPDTA160");
                cfg.AddAssembly(Assembly.GetAssembly(typeof(NHibernateHelper)));
                cfg.AddInputStream(HbmSerializer.Default.Serialize(Assembly.Load("Electrons.Core.Net8")));
                //if (NHibernateConfig.DataAssemblyName is null)
                //..  throw new TmcConfigurationException("Key DataAssemblyName Not Present in config");

                //cfg.AddAssembly(NHibernateConfig.DataAssemblyName);

                cfg.Properties["current_session_context_class"] = sessionContext;
                cfg.Properties["show_sql"] = "true";
                SessionFactory = cfg.BuildSessionFactory();

            }
            catch (Exception ex)
            {
                //ExceptionPublisher.Publish(ex, "PSS.Web");
                throw new Exception("NHibernate initialization failed", ex);
            }
        }

        /// <summary>
        /// Factory method for new ISessions.
        /// A convenient method to shorten the code required for
        /// the most common usage of this class: opening new sessions.
        /// </summary>
        public ISession OpenSession(bool appendToCurrentSessionContext = true)
        {
            if (SessionFactory == null)
                throw new Exception("SessionFactory is null. Make sure TmcDict.UseNHibernate is returning true for your app.");

            var dBSession = SessionFactory.OpenSession();
            dBSession.CreateSQLQuery($"SET SESSION sql_mode=(SELECT REPLACE(@@sql_mode,'ONLY_FULL_GROUP_BY',''));").ExecuteUpdate();
            if (appendToCurrentSessionContext)
                CurrentSessionContext.Bind(dBSession);
            return dBSession;
        }


        public static void DisposeOfCurrentSession(bool hasErrors, string appName)
        {
            var session = SessionFactory.GetCurrentSession();
            
            var trans = session.GetCurrentTransaction();
            if (trans != null)
            {
                if (!hasErrors && trans.IsActive)
                {
                    trans.Commit();
                }
                else if (hasErrors && trans.IsActive)
                    trans.Rollback();
                trans.Dispose();
            }

            CurrentSessionContext.Unbind(SessionFactory);
            session.Dispose();
        }
    }    
    public class DatabaseConfig
    {
        public string UserId { get; set; } = "test";
        public string Password { get; set; } = "test";
        public string Host { get; set; } = "test";
        public string Database { get; set; } = "test";
    }
}


