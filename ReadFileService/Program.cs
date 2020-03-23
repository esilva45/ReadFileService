using System;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace ReadFileService {
    static class Program {
        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        /// 
        static void Main(string[] args) {
            if (Environment.UserInteractive) {
                if (args.Length > 0) {
                    switch (args[0]) {
                        case "-install": {
                                ManagedInstallerClass.InstallHelper(new string[] {
                                Assembly.GetExecutingAssembly().Location }
                            );
                            License.LicenseGenerator();
                            break;
                        }
                        case "-uninstall": {
                            ManagedInstallerClass.InstallHelper(new string[] {
                                "/u", Assembly.GetExecutingAssembly().Location }
                            );
                            break;
                        }
                    }
                }
            } else {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] {
                    new Service()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
