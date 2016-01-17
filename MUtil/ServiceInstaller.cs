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

        public static void RunService(System.ServiceProcess.ServiceBase service, string[] args)
        {
            var method = service.GetType().GetMethod("OnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(service, new object[] { args });

            System.Threading.Thread.Sleep(-1);


        }

        class CommandFormatException : FormatException
        {
            public CommandFormatException(string msg) : base(msg) { }
        }

        class CommandParser
        {
            public Func<string, string[], int> Start { get; set; }
            public Func<string, int> Stop { get; set; }
            public Func<string, int> Pause { get; set; }
            public Func<string, int> Continue { get; set; }

            public CommandParser(System.ServiceProcess.ServiceBase[] services)
            {
                Services = services;
            }
            System.ServiceProcess.ServiceBase[] Services;


            Tuple<string, string[]> ParseArguments(string[] args, bool allowarguments)
            {
                if (args.Length == 0)
                {
                    if (Services.Length != 1) throw new CommandFormatException("Multiple services are available. Please specify servicename.");
                    return new Tuple<string, string[]>(Services[0].ServiceName, new string[] { });
                }
                else
                {
                    if (args.Length > 1 && !allowarguments) throw new CommandFormatException("Command did not expect parameters.");
                    return new Tuple<string, string[]>(args[0], args.Skip(1).ToArray());
                }
            }

            public int Parse(string[] args)
            {

                var command = args[0];

                var commandarguments = args.Skip(1).ToArray();

                switch (command)
                {
                    case "start":
                        var startargs = ParseArguments(commandarguments, allowarguments: true);
                        return Start(startargs.Item1, startargs.Item2);
                    case "stop":
                        var stopargs = ParseArguments(commandarguments, allowarguments: false);
                        return Stop(stopargs.Item1);
                    case "pause":
                        var pauseargs = ParseArguments(commandarguments, allowarguments: false);
                        return Pause(pauseargs.Item1);
                    case "continue":
                        var continueargs = ParseArguments(commandarguments, allowarguments: false);
                        return Pause(continueargs.Item1);
                    default:
                        throw new CommandFormatException($"Invalid command <{command}>");
                }

            }
        }


        public static int ExecuteCommand(System.Reflection.Assembly assembly, string[] args, System.ServiceProcess.ServiceBase[] services)
        {
            if (args.Length == 0) throw new CommandFormatException("Please specify a command.");

            if (args.Length == 1)
            {
                if (args[0] == "list")
                {
                    System.Console.WriteLine($"Available services:\n\t{services.Select(x => x.ServiceName).Aggregate((src, next) => $"{src}\n\t{next}")}\n");
                    return 0;
                }
                if (args[0] == "install") return InstallService(assembly);
                if (args[0] == "uninstall") return UninstallService(assembly);
                if (args[0] == "startconsole") return InteractiveManager.StartNew(services);
            }


            var parser = new CommandParser(services);

            parser.Start = (servicename, serviceargs) => StartService(servicename, serviceargs);
            parser.Stop = servicename => StopService(servicename);
            parser.Pause = servicename => PauseService(servicename);
            parser.Continue = servicename => ContinueService(servicename);

            return parser.Parse(args);
        }



        public static int ServiceMain(System.Reflection.Assembly assembly, string[] args, System.ServiceProcess.ServiceBase[] services)
        {
            if (services.Length == 0) throw new InvalidOperationException("No services in assembly");

            try
            {
                return ExecuteCommand(assembly, args, services);
            }
            catch (CommandFormatException e)
            {
                System.Console.WriteLine(
$@"{e.Message}
Usage: 
    install
    uninstall
    start [service] [args...]
    stop [service]
    pause [service]
    continue [service]
    list
    startconsole");
                return 1;
            }
        }


        public static int InstallService(System.Reflection.Assembly assembly)
        {
            System.Configuration.Install.AssemblyInstaller Installer = new System.Configuration.Install.AssemblyInstaller(assembly.Location, null);
            Installer.UseNewContext = true;
            Installer.Install(null);
            Installer.Commit(null);
            return 0;
        }

        public static int UninstallService(System.Reflection.Assembly assembly)
        {
            System.Configuration.Install.AssemblyInstaller Installer = new System.Configuration.Install.AssemblyInstaller(assembly.Location, null);
            Installer.UseNewContext = true;
            Installer.Uninstall(null);
            return 0;
        }

        public static int StartService(string servicename, string[] args)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(servicename);
            sc.Start();
            return 0;
        }

        public static int StopService(string servicename)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(servicename);
            sc.Stop();
            return 0;
        }

        public static int PauseService(string servicename)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(servicename);
            sc.Pause();
            return 0;
        }

        public static int ContinueService(string servicename)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(servicename);
            sc.Continue();
            return 0;
        }



        class InteractiveManager
        {

            public InteractiveManager(System.ServiceProcess.ServiceBase[] services)
            {
                Services = services;
                Parser = new CommandParser(services);

                Parser.Start = (name, args) =>
                {
                    StartAsync(name, args).ContinueWith(task => ShowException(task.Exception));
                    return 0;
                };
                Parser.Stop = name =>
                {
                    StopAsync(name).ContinueWith(task => ShowException(task.Exception));
                    return 0;
                };
                Parser.Pause = name =>
                {
                    PauseAsync(name).ContinueWith(task => ShowException(task.Exception));
                    return 0;
                };
                Parser.Continue = name =>
                {
                    ContinueAsync(name).ContinueWith(task => ShowException(task.Exception));
                    return 0;
                };
            }

            System.ServiceProcess.ServiceBase[] Services;

            System.ServiceProcess.ServiceBase GetService(string name)
            {
                var ret = Services.FirstOrDefault(x => x.ServiceName == name);
                if (ret == null) throw new CommandFormatException($"Service \"{name}\" is not available.");
                return ret;
            }

            CommandParser Parser;
            Dictionary<string, Task> runningservices = new Dictionary<string, Task>();

            object reportlocker = new object();
            void SendReport(string msg)
            {
                lock (reportlocker)
                {
                    System.Console.WriteLine(msg);
                }
            }
            void ShowException(Exception e)
            {
                if (e == null) return;
                if (e is AggregateException)
                {
                    foreach (var sube in ((AggregateException)e).InnerExceptions)
                    {
                        ShowException(sube);
                    }
                }
                else
                    SendReport($"{e.GetType().Name}: {e.Message}");
            }


            async Task StartAsync(string servicename, string[] args)
            {
                Task servicetask;
                lock (runningservices)
                {
                    if (runningservices.ContainsKey(servicename)) throw new InvalidOperationException($"Service \"{servicename}\" is already started.");
                    var service = GetService(servicename);
                    var method = service.GetType().GetMethod("OnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    SendReport($"Service {servicename} is starting.");
                    servicetask = Task.Run(() => method.Invoke(service, new object[] { args }));
                    runningservices[servicename] = servicetask;
                }
                await servicetask;
                lock (runningservices)
                {
                    runningservices.Remove(servicename);
                }
                SendReport($"Service {servicename} stopped.");
            }

            async Task StopAsync(string servicename)
            {
                var service = GetService(servicename);
                Task servicetask;
                lock (runningservices)
                {
                    if (!runningservices.ContainsKey(servicename)) throw new KeyNotFoundException($"Service {servicename} is not running.");
                    var method = service.GetType().GetMethod("OnStop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    servicetask = Task.Run(() => method.Invoke(service, new object[] { }));
                }
                await servicetask;
                SendReport($"Stop signal sent to {servicename}.");
            }

            async Task PauseAsync(string servicename)
            {
                var service = GetService(servicename);
                Task servicetask;
                lock (runningservices)
                {
                    if (!runningservices.ContainsKey(servicename)) throw new KeyNotFoundException($"Service {servicename} is not running.");
                    var method = service.GetType().GetMethod("OnPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    servicetask = Task.Run(() => method.Invoke(service, new object[] { }));
                }
                await servicetask;
                SendReport($"Pause signal sent to {servicename}.");
            }

            async Task ContinueAsync(string servicename)
            {
                var service = GetService(servicename);
                Task servicetask;
                lock (runningservices)
                {
                    if (!runningservices.ContainsKey(servicename)) throw new KeyNotFoundException($"Service {servicename} is not running.");
                    var method = service.GetType().GetMethod("OnContinue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    servicetask = Task.Run(() => method.Invoke(service, new object[] { }));
                }
                await servicetask;
                SendReport($"Continue signal sent to {servicename}.");
            }




            public int Run()
            {
                while (true)
                {
                    var args = System.Console.ReadLine().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (args.Length == 1)
                    {
                        if (args[0] == "quit") break;
                    }
                    try
                    {
                        Parser.Parse(args);
                    }
                    catch (Exception e)
                    {
                        ShowException(e);
                    }
                }
                return 0;
            }

            public static int StartNew(System.ServiceProcess.ServiceBase[] services) => new InteractiveManager(services).Run();

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

