using System;
using System.Collections.Generic;
using System.Reflection;

namespace RPAReflector
{
    internal class Hix61Strategy : HixVersionStrategy
    {
        protected override string BaseAssemblyName => "ChipSoft.FCL.Base, Version=6.1.0.0";
        public override string EnvProviderResourceName => "EnvProvider61.cs";

        public override List<Assembly> LoadAdditionalAssemblies()
        {
            return new List<Assembly>{Assembly.Load("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")};
        }

        public override void InitializeBaseBridge(Assembly extendedBase, Assembly coreNetFw)
        {
            if (extendedBase == null)
                throw new Exception("Failed to initialize base bridge, no extended base Assembly is found.");

            Type baseBridgeType = extendedBase.GetType("ChipSoft.FCL.Base.BaseBridge")
                ?? throw new Exception("Failed to find type for NetFrameworkBaseBridge!");

            MethodInfo miBaseBridgeInitialize = baseBridgeType.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public)
                ?? throw new Exception("Failed to find Initialize method for NetFrameworkBaseBridge!");

            miBaseBridgeInitialize.Invoke(null, null);
        }

        public override void ConfigureEnvironment(Assembly extendedBase, Assembly assemblyBase, Type impEnvProviderType)
        {
            Integration.RegisterEnvironments(impEnvProviderType);
            Integration.SetupCachingProvider(extendedBase, assemblyBase);
        }
    }
}