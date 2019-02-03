using System.Configuration;

namespace PerfectXL.WebDavServer
{
    internal class AppSettings
    {
        public static readonly string FilePath = ConfigurationManager.AppSettings["path"];
        public static readonly string Host = ConfigurationManager.AppSettings["host"];
        public static readonly int Port = int.Parse(ConfigurationManager.AppSettings["port"]);
        public static readonly string UserName = ConfigurationManager.AppSettings["user"];
        public static readonly string Password = ConfigurationManager.AppSettings["password"];
    }
}