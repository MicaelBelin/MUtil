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
    public class ExecutableServiceInstaller : System.Configuration.Install.Installer
    {


        public System.ServiceProcess.ServiceProcessInstaller ProcessInstaller { get; }

        public ExecutableServiceInstaller()
        {
            //# Service Account Information
            ProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            /*
                        ProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
                        ProcessInstaller.Username = null;
                        ProcessInstaller.Password = null;
            */
            this.Installers.Add(ProcessInstaller);
            /*
                        System.ServiceProcess.ServiceInstaller serviceInstaller = new System.ServiceProcess.ServiceInstaller();
                        //# Service Information
                        serviceInstaller.DisplayName = displayname;
                        serviceInstaller.Description = description;
                        serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;

                        serviceInstaller.ServiceName = servicename;

                        this.Installers.Add(serviceInstaller);
            */
        }

        public static void RunService(System.ServiceProcess.ServiceBase service, string[] args)
        {
            var method = service.GetType().GetMethod("OnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(service, new object[] { args });
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
            public Func<string, int, int> Execute { get; set; }

            public CommandParser(IEnumerable<System.ServiceProcess.ServiceBase> services)
            {
                Services = services.ToDictionary(x => x.ServiceName, x => x);
                DefaultServiceName = Services.Count == 1 ? Services.Values.First().ServiceName : null;
            }
            string DefaultServiceName;
            Dictionary<string, System.ServiceProcess.ServiceBase> Services;




            public int Parse(string[] args)
            {

                var command = args[0];

                string servicename;
                string[] commandarguments;

                if (Services.Count == 1) //single service mode
                {
                    if (args.Length == 1) //no service specified
                    {
                        servicename = Services.Values.First().ServiceName;
                        commandarguments = new string[0];
                    }
                    else
                    {
                        servicename = args[1];
                        commandarguments = args.Skip(2).ToArray();
                    }
                }
                else //multi service mode
                {
                    if (args.Length == 1) //no service specified
                    {
                        throw new CommandFormatException("Multiple services are available. Please specify servicename.");
                    }
                    else
                    {
                        servicename = args[1];
                        commandarguments = args.Skip(2).ToArray();
                    }
                }

                if (!Services.ContainsKey(servicename)) throw new CommandFormatException($"Invalid service \"{servicename}\".");
                var service = Services[servicename];


                switch (command)
                {
                    case "start":
                        return Start(servicename, commandarguments);
                    case "stop":
                        if (commandarguments.Length != 0) throw new CommandFormatException("stop command did not expect arguments.");
                        if (!service.CanStop) throw new CommandFormatException($"Service \"{servicename}\" does not support stop.");
                        return Stop(servicename);
                    case "pause":
                        if (commandarguments.Length != 0) throw new CommandFormatException("pause command did not expect arguments.");
                        if (!service.CanPauseAndContinue) throw new CommandFormatException($"Service \"{servicename}\" does not support pause and continue.");
                        return Pause(servicename);
                    case "continue":
                        if (commandarguments.Length != 0) throw new CommandFormatException("continue command did not expect arguments.");
                        if (!service.CanPauseAndContinue) throw new CommandFormatException($"Service \"{servicename}\" does not support pause and continue.");
                        return Continue(servicename);
                    case "execute":
                        if (commandarguments.Length != 1) throw new CommandFormatException("Expected <value> as parameter for executecommand.");
                        return Execute(servicename, Convert.ToInt32(commandarguments[0]));
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
                if (args[0] == "startconsole") return InteractiveConsole.StartNew(services);
                if (args[0] == "status")
                {
                    ShowStatus(services);
                    return 0;
                }
            }

            var parser = new CommandParser(services);

            parser.Start = (servicename, serviceargs) => StartService(servicename, serviceargs);
            parser.Stop = servicename => StopService(servicename);
            parser.Pause = servicename => PauseService(servicename);
            parser.Continue = servicename => ContinueService(servicename);
            parser.Execute = (servicename, value) => ExecuteServiceCommand(servicename, value);
            return parser.Parse(args);
        }

        public static void ShowStatus(System.ServiceProcess.ServiceBase[] services)
        {
            foreach (var service in services)
            {
                System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(service.ServiceName);
                System.Console.WriteLine(
                    $@" 
{ service.ServiceName}:
    Status: { sc.Status }
    CanHandlePowerEvent: {service.CanHandlePowerEvent}
    CanHandleSessionChangeEvent: {service.CanHandleSessionChangeEvent}
    CanPauseAndContinue: {service.CanPauseAndContinue}
    CanShutdown: {service.CanShutdown}
    CanStop: {service.CanStop}

");
            }
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
    execute <service> <value>
    run [service] [args...]
    list
    status
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

        public static int ExecuteServiceCommand(string servicename, int value)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(servicename);
            sc.ExecuteCommand(value);
            return 0;
        }


        class InteractiveConsole
        {

            public InteractiveConsole(System.ServiceProcess.ServiceBase[] services)
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
                Parser.Execute = (name, value) =>
                {
                    ExecuteServiceCommandAsync(name, value).ContinueWith(task => ShowException(task.Exception));
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
            Dictionary<string, Task> servicetasks = new Dictionary<string, Task>();
            Dictionary<string, System.ServiceProcess.ServiceControllerStatus> servicestatuses = new Dictionary<string, System.ServiceProcess.ServiceControllerStatus>();

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
                lock (servicetasks)
                {
                    if (servicetasks.ContainsKey(servicename)) throw new InvalidOperationException($"Service \"{servicename}\" is already started.");
                    var service = GetService(servicename);
                    var method = service.GetType().GetMethod("OnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    SendReport($"Service {servicename} is starting.");
                    servicestatuses[servicename] = System.ServiceProcess.ServiceControllerStatus.Running;
                    servicetask = Task.Run(() => method.Invoke(service, new object[] { args }));
                    servicetasks[servicename] = servicetask;
                }
                await servicetask;
                lock (servicetasks)
                {
                    servicetasks.Remove(servicename);
                    servicestatuses.Remove(servicename);
                }
                SendReport($"Service {servicename} stopped.");
            }

            async Task StopAsync(string servicename)
            {
                var service = GetService(servicename);
                Task servicetask;
                lock (servicetasks)
                {
                    if (!servicetasks.ContainsKey(servicename)) throw new KeyNotFoundException($"Service {servicename} is not running.");
                    var method = service.GetType().GetMethod("OnStop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    servicestatuses[servicename] = System.ServiceProcess.ServiceControllerStatus.StopPending;
                    servicetask = Task.Run(() => method.Invoke(service, new object[] { }));
                }
                await servicetask;
                SendReport($"Stop signal sent to {servicename}.");
            }

            async Task PauseAsync(string servicename)
            {
                var service = GetService(servicename);
                Task servicetask;
                lock (servicetasks)
                {
                    if (!servicetasks.ContainsKey(servicename)) throw new KeyNotFoundException($"Service {servicename} is not running.");
                    var method = service.GetType().GetMethod("OnPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    servicestatuses[servicename] = System.ServiceProcess.ServiceControllerStatus.PausePending;
                    servicetask = Task.Run(() => method.Invoke(service, new object[] { }));
                }
                await servicetask;
                lock (servicetasks)
                {
                    servicestatuses[servicename] = System.ServiceProcess.ServiceControllerStatus.Paused;
                }
                SendReport($"Pause signal sent to {servicename}.");
            }

            async Task ContinueAsync(string servicename)
            {
                var service = GetService(servicename);
                Task servicetask;
                lock (servicetasks)
                {
                    if (!servicetasks.ContainsKey(servicename)) throw new KeyNotFoundException($"Service {servicename} is not running.");
                    var method = service.GetType().GetMethod("OnContinue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    servicestatuses[servicename] = System.ServiceProcess.ServiceControllerStatus.ContinuePending;
                    servicetask = Task.Run(() => method.Invoke(service, new object[] { }));
                }
                await servicetask;
                lock (servicetasks)
                {
                    servicestatuses[servicename] = System.ServiceProcess.ServiceControllerStatus.Running;
                }
                SendReport($"Continue signal sent to {servicename}.");
            }

            async Task ExecuteServiceCommandAsync(string servicename, int value)
            {
                var service = GetService(servicename);
                Task servicetask;
                lock (servicetasks)
                {
                    if (!servicetasks.ContainsKey(servicename)) throw new KeyNotFoundException($"Service {servicename} is not running.");
                    var method = service.GetType().GetMethod("OnCustomCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    servicetask = Task.Run(() => method.Invoke(service, new object[] { value }));
                }
                await servicetask;
                SendReport($"ExecuteCommand ({value}) signal sent to {servicename}.");

            }


            void ShowUsage()
            {
                System.Console.WriteLine(
@"Available commands:
    start [service] [args...]
    stop [service]
    pause [service]
    continue [service]
    execute <service> <value>
    list
    status
    help
    quit");

            }


            void ShowStatus()
            {
                lock (servicetasks)
                {
                    foreach (var service in Services)
                    {
                        System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(service.ServiceName);
                        System.Console.WriteLine(
                            $@" 
{ service.ServiceName}:
    Status: { (servicestatuses.ContainsKey(service.ServiceName) ? servicestatuses[service.ServiceName] : System.ServiceProcess.ServiceControllerStatus.Stopped) }
    CanHandlePowerEvent: {service.CanHandlePowerEvent}
    CanHandleSessionChangeEvent: {service.CanHandleSessionChangeEvent}
    CanPauseAndContinue: {service.CanPauseAndContinue}
    CanShutdown: {service.CanShutdown}
    CanStop: {service.CanStop}

");
                    }
                }
            }


            public int Run()
            {
                while (true)
                {
                    var args = System.Console.ReadLine().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (args.Length == 1)
                    {
                        if (args[0] == "quit") break;
                        if (args[0] == "help")
                        {
                            ShowUsage();
                            continue;
                        }
                        if (args[0] == "status")
                        {
                            ShowStatus();
                            continue;
                        }
                        if (args[0] == "list")
                        {
                            System.Console.WriteLine($"Available services:\n\t{Services.Select(x => x.ServiceName).Aggregate((src, next) => $"{src}\n\t{next}")}\n");
                            continue;
                        }
                    }
                    try
                    {
                        Parser.Parse(args);
                    }
                    catch (CommandFormatException e)
                    {
                        ShowException(e);
                        ShowUsage();
                    }
                    catch (Exception e)
                    {
                        ShowException(e);
                    }
                }
                return 0;
            }

            public static int StartNew(System.ServiceProcess.ServiceBase[] services) => new InteractiveConsole(services).Run();

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

