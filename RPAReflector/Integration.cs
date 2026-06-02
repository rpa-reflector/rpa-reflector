// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RPAReflector
{
    public static class Integration
    {
        public static HixVersionStrategy HixProvider { get; private set; }
        public static object EnvironmentProvider { get; private set; }
        public static FileVersionInfo HixVersionDetected { get; private set; } = null;

        private static string _hixVersionConfigured;
        private static string _selectedEnvironmentConfigured;

        public static void PrepareEnvironment()
        {
            if (!CanPrepareEnvironment())
                return;
            
            string hixMinor = _hixVersionConfigured.Substring(0, 3);            
            HixProvider = HixVersionStrategy.Create(hixMinor);
            var loadedAssemblies = AssemblyLoader.LoadAssemblies(_hixVersionConfigured, HixProvider);

            var parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                ReferencedAssemblies = { "mscorlib.dll", "System.dll" }
            };

            var (extendedBase, assemblyBase, coreNetFw) = ProcessAssemblies(HixProvider, loadedAssemblies, parameters);

            Console.WriteLine("Initializing BaseBridge...");
            HixProvider.InitializeBaseBridge(extendedBase, coreNetFw);

            SetupEnvironmentProvider(
                HixProvider,
                hixMinor,
                _selectedEnvironmentConfigured,
                parameters,
                HixProvider.ImpEnvironmentProviderType,
                extendedBase,
                assemblyBase);
        }

        private static bool CanPrepareEnvironment()
        {
            _hixVersionConfigured = AppSettingsHelper.GetAppSetting("HixVersion", null, true);

            if (_hixVersionConfigured == "none")
            {
                Console.WriteLine("No HixVersion specified. Cannot loading any assemblies and prepare environment.");
                return false;
            }

            if (_hixVersionConfigured.Split('.').Length != 4)
                throw new Exception("HixVersion needs to contain 4 levels, e.g. 6.1.0.0");

            _selectedEnvironmentConfigured = AppSettingsHelper.GetAppSetting("SelectedEnvironment", null, true);

            return true;
        }

        private static (Assembly extendedBase, Assembly assemblyBase, Assembly coreNetFw)
            ProcessAssemblies(HixVersionStrategy strategy, IEnumerable<Assembly> assemblies, CompilerParameters parameters)
        {
            Assembly extendedBase = null;
            Assembly assemblyBase = null;
            Assembly coreNetFw = null;

            foreach (Assembly asm in assemblies)
            {
                DetectHixVersion(asm);
                strategy.ExtractAssemblyTypes(asm);

                string name = asm.GetName().Name;
                switch (name)
                {
                    case "ChipSoft.FCL.ExtendedBase":
                        extendedBase = asm;
                        break;
                    case "ChipSoft.FCL.Base":
                        assemblyBase = asm;
                        break;
                    case "ChipSoft.Core.Base.NetFramework":
                        coreNetFw = asm;
                        break;
                    default:
                        break;
                }

                parameters.ReferencedAssemblies.Add(asm.Location);
            }

            if (strategy.ImpEnvironmentProviderType == null)
                throw new Exception($"No impEnvironmentProviderType could be determined!");

            return (extendedBase, assemblyBase, coreNetFw);
        }

        private static void DetectHixVersion(Assembly asm)
        {
            if (!asm.FullName.Contains("ChipSoft.FCL.ClassRegistry"))
                return;
            
            string location = asm.Location;
            if (Directory.Exists(Path.GetDirectoryName(location) + Path.DirectorySeparatorChar))
                HixVersionDetected = FileVersionInfo.GetVersionInfo(location);
        }

        private static void SetupEnvironmentProvider(
            HixVersionStrategy strategy,
            string hixMinor,
            string preSelectedEnv,
            CompilerParameters parameters,
            Type impEnvProviderType,
            Assembly extendedBase,
            Assembly assemblyBase)
        {
            Console.WriteLine("Creating environment provider...");
            EnvironmentProvider = DynamicCompiler.CompileEnvProvider(parameters, strategy, preSelectedEnv)
                ?? throw new Exception("Unable to compile environment provider!");

            strategy.ConfigureEnvironment(extendedBase, assemblyBase, impEnvProviderType);

            var envProviderField = impEnvProviderType.GetField("EnvironmentProvider", BindingFlags.Static | BindingFlags.Public);
            Console.WriteLine("Setting dynamically created environment provider on impEnvironmentProvider...");
            envProviderField.SetValue(null, EnvironmentProvider);

            LoginToHix();
        }

        internal static void RegisterEnvironments(Type impEnvProviderType)
        {
            var environmentSection = (EnvironmentsSection)ConfigurationManager.GetSection("environmentsSection");
            if (environmentSection == null) return;

            MethodInfo addEnvMi = EnvironmentProvider.GetType().GetMethod("AddEnvironment", BindingFlags.Instance | BindingFlags.Public)
                ?? throw new Exception("Unable to locate AddEnvironment method dynamic environment provider!");

            foreach (EnvironmentElement envEl in environmentSection.AllValues)
            {
                Console.WriteLine($"Env Code: {envEl.Code} Description: {envEl.Description}");

                if (ConfigurationManager.ConnectionStrings[envEl.Code] == null)
                    throw new Exception("Unable to find connection string for env: " + envEl.Code);

                string connectionString = ConfigurationManager.ConnectionStrings[envEl.Code].ConnectionString;
                Console.WriteLine("Adding target Hix Environment into environmentProvider...");
                addEnvMi.Invoke(EnvironmentProvider, new object[] { envEl.Code, envEl.Description, connectionString, false, false, "" });
            }
        }

        internal static void SetupCachingProvider(Assembly extendedBase, Assembly assemblyBase)
        {
            Console.WriteLine("Setting caching configuration provider...");
            extendedBase.GetType("ChipSoft.FCL.Base.BaseBridge").GetMethod("Initialize").Invoke(null, null);

            Type basePoliciesType   = assemblyBase.GetType("ChipSoft.FCL.Base.TBasePolicies");
            Type coerceDelegateType = assemblyBase.GetType("ChipSoft.FCL.Base.TCoercePreferredFactoryCacheType");

            if (basePoliciesType == null || coerceDelegateType == null)
                throw new Exception("Failed to get types for TBasePolicies and/or TCoercePreferredFactoryCacheType.");

            EventInfo eventInfo = basePoliciesType.GetEvent("OnCoercePreferredFactoryCacheType")
                ?? throw new Exception("Failed to get event info for OnCoercePreferredFactoryCacheType.");

            MethodInfo miSetFactoryCacheType = EnvironmentProvider.GetType().GetMethod("SetFactoryCacheType", BindingFlags.Static | BindingFlags.Public)
                ?? throw new Exception("Failed to get method info for SetFactoryCacheType.");

            eventInfo.AddEventHandler(null, Delegate.CreateDelegate(coerceDelegateType, miSetFactoryCacheType));
        }

        private static void LoginToHix()
        {
            string username = AppSettingsHelper.GetAppSetting("HixServiceUsername", null, true);
            string password   = AppSettingsHelper.GetAppSetting("HixServicePassword", null, true);
            string clientType  = AppSettingsHelper.GetAppSetting("HixClientType", "CONSOLE", false);
            string application = AppSettingsHelper.GetAppSetting("HixApplication", "COMEZ", false);

            MethodInfo createObjectByClassIDMi = HixProvider.BaseUtilType.GetMethod("CreateObjectByClassID", BindingFlags.Public | BindingFlags.Static)
                ?? throw new Exception("Failed to get method info for CreateObjectByClassID.");

            Console.WriteLine("Logging in to Chipsoft. This may take a few seconds...");
            var zisconServiceObj = createObjectByClassIDMi.Invoke(null, new object[] { "ZISCON_SERVICE" });

            var loginMi = zisconServiceObj.GetType().GetMethod("Login",
                new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) })
                ?? throw new Exception("Could not find method info for login method!");

            // the happy flow will throw these exceptions for null string values!
            var loginRet = loginMi.Invoke(zisconServiceObj, new object[] { username, password, "", clientType, application });
            
            string logMsg = "Hix Login returned: " + loginRet;
            Console.WriteLine(logMsg);
            EventLog.WriteEntry(Program.EventSource, logMsg, EventLogEntryType.Information);
        }
    }
}