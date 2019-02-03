using System;
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

            var mustStop = false;

            if (args.Any(s => string.Equals(s, "-bind", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine(HttpListenerSslEnabler.BindCertificate() == HttpListenerSslEnabler.BindingResult.Success
                    ? "Certificate binding succeeded."
                    : "Could not bind the certificate to the IP End Point. HTTPS is not possible now.");
                mustStop = true;
            }

            if (!mustStop && !HttpListenerSslEnabler.HasBinding())
            {
                Console.WriteLine("We must bind the certificate to the IP End Point first. This will happen in an elevated command.");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo {FileName = GetExecutingAssemblyCodeBasePath(), Arguments = "-bind", Verb = "runas"}
                };
                process.Start();
                process.WaitForExit();
                mustStop = true;
            }

            if (!mustStop)
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

            if (!ConsoleWillBeDestroyedAtTheEnd())
            {
                return;
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
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

        private static bool ConsoleWillBeDestroyedAtTheEnd()
        {
            var processList = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);
            return processCount == 1;
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