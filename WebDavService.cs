using System;
using System.Security.Cryptography;
using System.Text;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Stores.DiskStore;

namespace PerfectXL.WebDavServer
{
    public class WebDavService
    {
        public void Start()
        {
            StartServer();
        }

        public void Stop()
        {
        }

        private static string Hash(string password)
        {
            return Convert.ToBase64String(new SHA256CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private static void StartServer()
        {
            IWebDavStore store = new WebDavDiskStore(AppSettings.FilePath);
            var listenerAdapter = new BasicAuthHttpListenerAdapter();
            var server = new WebDAVSharp.Server.WebDavServer(store, listenerAdapter)
            {
                VerifyUserNameAndPasswordFunc = (username, password) => username == AppSettings.UserName && Hash(password) == AppSettings.Password
            };
            server.Start($"https://{AppSettings.Host}:{AppSettings.Port}/");
        }
    }
}