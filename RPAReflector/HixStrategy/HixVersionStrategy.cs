using System;
using System.Collections.Generic;
using System.Reflection;

namespace RPAReflector
{
    public abstract class HixVersionStrategy
    {
        public Type BaseUtilType { get; protected set; }
        public Type ImpEnvironmentProviderType { get; protected set; }
        public Type EzisUtil2RegisterGuidListHolderType { get; protected set; }
        public Type EzisUtil2UtilitiesHolderType { get; protected set; }
        
        public abstract string EnvProviderResourceName { get; }
        protected abstract string BaseAssemblyName { get; }
        public abstract List<Assembly> LoadAdditionalAssemblies();
        public abstract void InitializeBaseBridge(Assembly extendedBase, Assembly coreNetFw);

        // Override to add version-specific post-compile steps (environment registration, caching).
        // Default is a no-op so versions that need nothing (e.g. 6.3) require no override.
        public virtual void ConfigureEnvironment(Assembly extendedBase, Assembly assemblyBase, Type impEnvProviderType) { }

        public static HixVersionStrategy Create(string hixMinor)
        {
            switch (hixMinor)
            {
                case "6.1": return new Hix61Strategy();
                case "6.3": return new Hix63Strategy();
                default: throw new Exception($"Unsupported HiX version: {hixMinor}");
            }
        }

        // Returns the impEnvironmentProviderType when the base assembly is matched,
        // null otherwise. Also handles the shared EzisUtil2 initialisation.
        public void ExtractAssemblyTypes(Assembly asm)
        {
            if (asm.FullName.Contains(BaseAssemblyName))
            {
                BaseUtilType = asm.GetType("ChipSoft.FCL.Base.Units.BaseUtil");
                ImpEnvironmentProviderType = asm.GetType("ChipSoft.FCL.Base.Units.impEnvironmentProvider");
            }

            if (asm.FullName.Contains("ChipSoft.Comez.EzisUtil2"))
            {
                EzisUtil2UtilitiesHolderType = asm.GetType("ChipSoft.Comez.EzisUtil2.Utilities");
                EzisUtil2RegisterGuidListHolderType = asm.GetType("ChipSoft.Comez.EzisUtil2.RegisterGuidListHolder");
                MethodInfo initMi = EzisUtil2RegisterGuidListHolderType.GetMethod("InitRegGuidList", BindingFlags.Public | BindingFlags.Static);
                initMi.Invoke(null, null);
            }
        }
    }
}
