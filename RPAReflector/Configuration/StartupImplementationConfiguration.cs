// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using System.Configuration;

namespace RPAReflector
{
    public class StartupImplementationElement : System.Configuration.ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
        }

        [ConfigurationProperty("source", IsRequired = true)]
        public string Source
        {
            get
            {
                return this["source"] as string;
            }
        }
    }

    public class ImplementationCollection : ConfigurationElementCollection
    {
        public StartupImplementationElement this[int index]
        {
            get
            {
                return base.BaseGet(index) as StartupImplementationElement;
            }

        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new StartupImplementationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((StartupImplementationElement)(element)).Name;
        }
    }

    public class StartupImplementationsSection : ConfigurationSection
    {

        public StartupImplementationsSection()
        {

        }

        [ConfigurationProperty("startupImplementationCollection")]
        public ImplementationCollection AllValues
        {
            get
            {
                return this["startupImplementationCollection"] as ImplementationCollection;
            }
        }

        public static StartupImplementationsSection GetStartupImplementationsSectionSection()
        {
            return ConfigurationManager.GetSection("startupImplementationsSection") as StartupImplementationsSection;
        }
    }
}