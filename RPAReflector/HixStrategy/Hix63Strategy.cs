using System;
using System.Collections.Generic;
using System.Reflection;

namespace RPAReflector
{
    internal class Hix63Strategy : HixVersionStrategy
    {
        protected override string BaseAssemblyName => "ChipSoft.Core.Base, Version=6.3.0.0";
        public override string EnvProviderResourceName => "EnvProvider63.cs";

        public override List<Assembly> LoadAdditionalAssemblies()
        {
            return new List<Assembly>{Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51")};
        }

        public override void InitializeBaseBridge(Assembly extendedBase, Assembly coreNetFw)
        {
            if (coreNetFw == null)
                throw new Exception("Assembly ChipSoft.Core.Base.NetFramework was not found!");

            Type baseBridgeType = coreNetFw.GetType("ChipSoft.Core.Base.NetFramework.NetFrameworkBaseBridge")
                ?? throw new Exception("Failed to find type for NetFrameworkBaseBridge!");

            MethodInfo miBaseBridgeInitialize = baseBridgeType.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public)
                ?? throw new Exception("Failed to find Initialize method for BaseBridge!");

            miBaseBridgeInitialize.Invoke(null, null);
        }
    }
}
