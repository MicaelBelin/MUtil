using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xintric.MUtil
{



    /// <summary>
    /// 
    /// * Create a service class, based on ServiceBase. Implement its methods.
    /// * Create an installer class, based on ServiceInstaller. Simply create a default constructor and pass the parameters you wish to base. ServiceName must match the ServiceName in the service object! Add the [RunInstaller(true)] attribute on the class.
    /// </summary>
    public class ServiceInstaller : System.Configuration.Install.Installer
    {
        /// <summary>
        /// Public Constructor for WindowsServiceInstaller.
        /// - Put all of your Initialization code here.
        /// </summary>
        public ServiceInstaller(string servicename, string displayname, string description)
        {
            //# Service Account Information
            System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller =
                               new System.ServiceProcess.ServiceProcessInstaller();
            serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;
            this.Installers.Add(serviceProcessInstaller);

            System.ServiceProcess.ServiceInstaller serviceInstaller = new System.ServiceProcess.ServiceInstaller();
            //# Service Information
            serviceInstaller.DisplayName = displayname;
            serviceInstaller.Description = description;
            serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;

            serviceInstaller.ServiceName = servicename;

            this.Installers.Add(serviceInstaller);
        }



        public static int ServiceMain(System.Reflection.Assembly assembly, string[] args, System.ServiceProcess.ServiceBase[] services)
        {
            if (services.Length == 0)
            {
                System.Console.Error.WriteLine("No services registered in assembly");
                return 0;
            }

            if (args.Length == 1 && args[0] == "install")
            {
                return InstallService(assembly);
            }
            else if (args.Length == 1 && args[0] == "uninstall")
            {
                return UninstallService(assembly);
            }
            else if (args.Length >= 1 && args.Length <= 2 && args[0] == "start")
            {
                if (args.Length == 2)
                {
                    StartService(args[1]);
                }
                else
                {
                    if (services.Length == 1) StartService(services.First().ServiceName);
                    else
                    {
                        System.Console.Error.WriteLine(String.Format("Multiple services are available. Please specify which service to start.\nAvailable services:\n{0}",
                            services.Select(x => x.ServiceName).Aggregate((src, next) => src + "\n" + next)));
                        return 1;
                    }
                }
            }
            else if (args.Length >= 1 && args.Length <= 2 && args[0] == "stop")
            {
                if (args.Length == 2)
                {
                    StopService(args[1]);
                }
                else
                {
                    if (services.Length == 1)
                    {
                        StopService(services.First().ServiceName);
                    }
                    else
                    {
                        System.Console.Error.WriteLine(String.Format("Multiple services are available. Please specify which service to stop.\nAvailable services:\n{0}",
                            services.Select(x => x.ServiceName).Aggregate((src, next) => src + "\n" + next)));
                        return 1;
                    }
                }
            }
            else if (args.Length == 1 && args[0] == "list")
            {
                System.Console.WriteLine($"Available services:\n{services.Select(x => x.ServiceName).Aggregate((src, next) => $"{src}\n{next}")}");              
            }
            else if (args.Length > 1 && args[0] == "run")
            {

                var servicename = args[1];
                var service = services.FirstOrDefault(x => x.ServiceName == servicename);
                if (service == null)
                {
                    System.Console.WriteLine($"Invalid service name \"{servicename}\".");
                    return 1;
                }

                var method = service.GetType().GetMethod("OnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method.Invoke(service, new object[] { args.Skip(2).ToArray() });

                System.Threading.Thread.Sleep(-1);


                return 0;
            }
            else if (args.Length == 0)
            {
                System.ServiceProcess.ServiceBase.Run(services);
            }
            else
            {
                System.Console.WriteLine("Invalid parameter. valid parameters are:\ninstall\nuninstall\nstart [servicename]\nstop [servicename]\nrun [servicename] [parameters...]\nlist");
                return 1;
            }


            return 0;
        }


        public static int InstallService(System.Reflection.Assembly assembly)
        {
            try
            {
                System.Configuration.Install.AssemblyInstaller Installer = new System.Configuration.Install.AssemblyInstaller(assembly.Location, null);
                Installer.UseNewContext = true;
                Installer.Install(null);
                Installer.Commit(null);
                return 0;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Unable to install service: " + e.Message);
                return 1;
            }
        }

        public static int UninstallService(System.Reflection.Assembly assembly)
        {
            try
            {
                System.Configuration.Install.AssemblyInstaller Installer = new System.Configuration.Install.AssemblyInstaller(assembly.Location, null);
                Installer.UseNewContext = true;
                Installer.Uninstall(null);
                return 0;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Unable to uninstall service: " + e.Message);
                return 1;
            }

        }

        public static int StartService(string servicename)
        {
            try
            {
                System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(servicename);
                sc.Start();
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Unable to start service: " + e.Message);
                return 1;
            }
            return 0;
        }

        public static int StopService(string servicename)
        {
            try
            {
                System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(servicename);
                sc.Stop();
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Unable to stop service: " + e.Message);
                return 1;
            }
            return 0;
        }


    }



    //Sample implementation
    //[System.ComponentModel.RunInstaller(true)]
    //public class Installer : Xintric.MUtil.ServiceInstaller
    //{
    //    public Installer()
    //        : base("TestService", "my test agent", "This is a test agent for [TEST]")
    //    {
    //        System.Console.WriteLine("Got here!");
    //    }
    //}

    //public class Service1 : System.ServiceProcess.ServiceBase
    //{



    //    public Service1()
    //    {
    //        this.ServiceName = "TestService";
    //    }

    //    protected override void OnStart(string[] args)
    //    {
    //    }

    //    protected override void OnStop()
    //    {
    //    }
    //}


}
