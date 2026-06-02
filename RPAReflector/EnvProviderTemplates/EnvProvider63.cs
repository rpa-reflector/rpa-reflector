using ChipSoft.FCL.Base;
using ChipSoft.Publics.DataDictionary;
using System.Diagnostics.CodeAnalysis;

namespace ChipSoft.Comez.EzisUtil2
{
    public class RPAReflectorEnvironmentProvider : HiXEnvironmentProvider
    {
        private string PreferredEnvironment;

        public RPAReflectorEnvironmentProvider(string preferredEnvironment)
        {
            this.PreferredEnvironment = preferredEnvironment;
        }

        protected override IEnvironment SelectEnvironment()
        {
            IEnvironment environment = (IEnvironment) null;
            if (!string.IsNullOrEmpty(this.PreferredEnvironment))
            {
                environment = this.FindEnvironmentById(this.PreferredEnvironment);
            }

            int i = 0;
            if (environment == null)
            {
                foreach (var env in this.Environments)
                {
                    if (environment == null)
                        environment = env;
                    i++;
                }
            }

            if (i == 0)
                throw new ENoEnvironments("No Env");

            return environment;
        }
    }
}
