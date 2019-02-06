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

            ContinueState continueState;
            if (args.Length == 6 && args[0] == "-configure")
            {
                continueState = ConfigureSettings(args[1], args[2], args[3], args[4], args[5]);
            }
            else if (args.Any(s => string.Equals(s, "-bind", StringComparison.OrdinalIgnoreCase)))
            {
                continueState = TryBind(true);
            }
            else if (HttpListenerSslEnabler.HasBinding())
            {
                continueState = ContinueState.Continue;
            }
            else
            {
                continueState = EnsureBinding();
            }

            switch (continueState)
            {
                case ContinueState.SuccessAndStop:
                case ContinueState.ErrorAndStop when !ConsoleWillBeDestroyedAtTheEnd():
                    return;
                case ContinueState.ErrorAndStop:
                    Console.WriteLine("Press any key to close...");
                    Console.ReadKey();
                    return;
                case ContinueState.Continue:
                    RunMain();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
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

        private static ContinueState ConfigureSettings(string path, string host, string port, string user, string password)
        {
            var encrypted = WebDavService.Hash(password);
            try
            {
                Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                configFile.AppSettings.Settings.AddOrUpdateSettings(new Dictionary<string, string>
                    {{"path", path}, {"host", host}, {"port", port}, {"user", user}, {"password", encrypted}});

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                return ContinueState.SuccessAndStop;
            }
            catch (ConfigurationErrorsException ex)
            {
                MyLogger.Error(ex, ex.Message);
                return ContinueState.ErrorAndStop;
            }
        }

        private static bool ConsoleWillBeDestroyedAtTheEnd()
        {
            var processList = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);
            return processCount == 1;
        }

        private static ContinueState EnsureBinding()
        {
            if (UACHelper.UACHelper.IsElevated)
            {
                return TryBind(false);
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
            return HttpListenerSslEnabler.HasBinding() ? ContinueState.Continue : ContinueState.ErrorAndStop;
        }

        private static void RunMain()
        {
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

        private static ContinueState TryBind(bool isSeparateProcess)
        {
            var isSuccess = HttpListenerSslEnabler.BindCertificate() == HttpListenerSslEnabler.BindingResult.Success;
            if (isSuccess)
            {
                const string successMessage = "Certificate binding succeeded.";
                MyLogger.Info(successMessage);
                Console.WriteLine(successMessage);
                return isSeparateProcess ? ContinueState.SuccessAndStop : ContinueState.Continue;
            }

            const string failureMessage = "Could not bind the certificate to the IP End Point. HTTPS is not possible now.";
            MyLogger.Info(failureMessage);
            Console.WriteLine(failureMessage);
            return ContinueState.ErrorAndStop;
        }

        #region Nested type
        private enum ContinueState
        {
            Unknown,
            SuccessAndStop,
            ErrorAndStop,
            Continue
        }
        #endregion

        #region Helpers
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

        private static string GetExecutingAssemblyCodeBasePath()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Uri.UnescapeDataString(uri.Path);
        }
        #endregion
    }
}