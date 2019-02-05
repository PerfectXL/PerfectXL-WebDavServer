using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using NLog;
using NLog.Config;
using NLog.Targets;
using Topshelf;

namespace PerfectXL.WebDavServer
{
    internal class Program
    {
        private static readonly Logger MyLogger = LogManager.GetCurrentClassLogger();

        internal static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ConfigureNLog();

            if (args.Length == 6 && args[0] == "-configure")
            {
                ConfigureSettings(args[1], args[2], args[3], args[4], args[5]);
                return;
            }

            var mustStop = false;

            if (args.Any(s => string.Equals(s, "-bind", StringComparison.OrdinalIgnoreCase)))
            {
                var message = HttpListenerSslEnabler.BindCertificate() == HttpListenerSslEnabler.BindingResult.Success
                    ? "Certificate binding succeeded."
                    : "Could not bind the certificate to the IP End Point. HTTPS is not possible now.";
                MyLogger.Info(message);
                Console.WriteLine(message);
                mustStop = true;
            }

            if (!mustStop && !HttpListenerSslEnabler.HasBinding())
            {
                mustStop = EnsureBinding();
            }

            if (mustStop)
            {
                if (!ConsoleWillBeDestroyedAtTheEnd())
                {
                    return;
                }

                Console.WriteLine("Press any key to close...");
                Console.ReadKey();
            }

            try
            {
                TopshelfExitCode rc = HostFactory.Run(x =>
                {
                    x.Service<WebDavService>(s =>
                    {
                        s.ConstructUsing(name => new WebDavService());
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());
                    });
                    x.RunAsLocalSystem();

                    x.SetDescription("PerfectXL.WebDavServer");
                    x.SetDisplayName("PerfectXL.WebDavServer");
                    x.SetServiceName("PerfectXL.WebDavServer");
                });

                var exitCode = (int) Convert.ChangeType(rc, rc.GetTypeCode());
                Environment.ExitCode = exitCode;
            }
            catch (Exception ex)
            {
                MyLogger.Error(ex, ex.Message);
            }
        }

        private static void ConfigureNLog()
        {
            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget("fileTarget")
            {
                FileName = "${basedir}/PerfectXL-WebDavServer.log",
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveFileName = "${basedir}/PerfectXL-WebDavServer-{#}.log",
                MaxArchiveFiles = 9,
                Layout = "${longdate} [${threadid:padding=4}] ${level:uppercase=true:padding=-5} ${logger} - ${message} ${exception}"
            };
            config.AddTarget(fileTarget);
            config.AddRuleForAllLevels(fileTarget);

            LogManager.Configuration = config;
        }

        private static void ConfigureSettings(string path, string host, string port, string user, string password)
        {
            var encrypted = WebDavService.Hash(password);
            try
            {
                Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                KeyValueConfigurationCollection settings = configFile.AppSettings.Settings;
                foreach (var kvp in new Dictionary<string, string> {{"path", path}, {"host", host}, {"port", port}, {"user", user}, {"password", encrypted}})
                {
                    if (settings[kvp.Key] == null)
                    {
                        settings.Add(kvp.Key, kvp.Value);
                    }
                    else
                    {
                        settings[kvp.Key].Value = kvp.Value;
                    }
                }

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException ex)
            {
                MyLogger.Error(ex, ex.Message);
            }
        }

        private static bool ConsoleWillBeDestroyedAtTheEnd()
        {
            var processList = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);
            return processCount == 1;
        }

        private static bool EnsureBinding()
        {
            if (UACHelper.UACHelper.IsElevated)
            {
                bool mustStop;
                string message;
                if (HttpListenerSslEnabler.BindCertificate() != HttpListenerSslEnabler.BindingResult.Success)
                {
                    message = "Certificate binding succeeded.";
                    mustStop = false;
                }
                else
                {
                    message = "Could not bind the certificate to the IP End Point. HTTPS is not possible now.";
                    mustStop = true;
                }

                MyLogger.Info(message);
                Console.WriteLine(message);
                return mustStop;
            }

            const string elevatedMessage = "We must bind the certificate to the IP End Point first. This will happen in an elevated command.";
            MyLogger.Info(elevatedMessage);
            Console.WriteLine(elevatedMessage);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo {FileName = GetExecutingAssemblyCodeBasePath(), Arguments = "-bind", Verb = "runas"}
            };
            process.Start();
            process.WaitForExit();
            return true;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

        private static string GetExecutingAssemblyCodeBasePath()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Uri.UnescapeDataString(uri.Path);
        }
    }
}