using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.ServiceProcess;

namespace RPAReflector
{
  
    internal static class Program
    {
        public const string EventSource = "RPA .NET Reflector";
        public const string EventLogName = "Application";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (!EventLog.SourceExists(EventSource))
            {
                EventLog.CreateEventSource(EventSource, EventLogName);
            }

            try
            {
                Integration.PrepareEnvironment();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Program.EventSource, "Error in PrepareEnvironment: " + ex.ToString(), EventLogEntryType.Error);
                throw;
            }

            var startupImplementationsSection = StartupImplementationsSection.GetStartupImplementationsSectionSection();
            foreach (StartupImplementationElement startupImplEl in startupImplementationsSection.AllValues)
            {
                EventLog.WriteEntry(Program.EventSource, "Loading implementation " + startupImplEl.Name + " from source " + startupImplEl.Source, EventLogEntryType.Information);
                try
                {
                    string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
                    string sourceFilePath = Path.Combine(strWorkPath, startupImplEl.Source);
                    if (File.Exists(sourceFilePath))
                    {
                        string nameValue = startupImplEl.Name;

                        string implementationName = nameValue;
                        string strVersion = null;
                        if (nameValue.Contains("-"))
                        {
                            int lastSepPos = nameValue.LastIndexOf('-');

                            if (lastSepPos != -1)
                            {
                                implementationName = nameValue.Substring(0, lastSepPos);
                                strVersion = nameValue.Substring(lastSepPos + 1);
                            }
                        }

                        string implementationContents = null;
                        using (StreamReader streamReader = new StreamReader(sourceFilePath, Encoding.UTF8))
                        {
                            implementationContents = streamReader.ReadToEnd();
                        }

                        DynamicCompiler.CompileImplementation(implementationName, implementationContents, DynamicCompiler.GetImplementationCompilationParameters(), strVersion);
                    }
                    else
                    {
                        EventLog.WriteEntry(Program.EventSource, "Could not load implementation " + startupImplEl.Name + " from source " + sourceFilePath, EventLogEntryType.Warning);
                    }
                } catch (Exception ex)
                {
                    EventLog.WriteEntry(Program.EventSource, "Error loading implementation " + startupImplEl.Name + " from source " + startupImplEl.Source + ": " + ex.Message, EventLogEntryType.Error);
                }
            }

#if DEBUG
                // If in debug mode, run as console application
                var service = new RPAReflector();
            service.OnDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new RPAReflector()
            };
            ServiceBase.Run(ServicesToRun);
#endif

        }
    }
}
