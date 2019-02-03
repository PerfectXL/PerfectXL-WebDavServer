using System;
using System.Security.Cryptography;
using System.Text;
using NLog;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Stores.DiskStore;

namespace PerfectXL.WebDavServer
{
    public class WebDavService
    {
        private static readonly Logger MyLogger = LogManager.GetCurrentClassLogger();

        public void Start()
        {
            MyLogger.Info($"Start {nameof(WebDavService)}");
            StartServer();
        }

        public void Stop()
        {
            MyLogger.Info($"Stop {nameof(WebDavService)}");
        }

        private static string Hash(string password)
        {
            return Convert.ToBase64String(new SHA256CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private static void StartServer()
        {
            IWebDavStore store = new WebDavDiskStore(AppSettings.FilePath);
            MyLogger.Debug($"{nameof(IWebDavStore)} @ {AppSettings.FilePath}");
            var listenerAdapter = new BasicAuthHttpListenerAdapter();
            var server = new WebDAVSharp.Server.WebDavServer(store, listenerAdapter)
            {
                VerifyUserNameAndPasswordFunc = (username, password) => username == AppSettings.UserName && Hash(password) == AppSettings.Password
            };
            var url = $"https://{AppSettings.Host}:{AppSettings.Port}/";
            MyLogger.Debug($"Listening on {url}");
            server.Start(url);
        }
    }
}