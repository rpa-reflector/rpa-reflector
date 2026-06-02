// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using System.Configuration;
using System;

namespace RPAReflector
{
    public static class AppSettingsHelper
    {
        public static string GetAppSetting(string key, string defaultValue = null, bool throwable = false)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrEmpty(value) && throwable)
                throw new Exception(key + " is not configured in App.config!");

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return value;
        }
    }

    public class EnvironmentElement : System.Configuration.ConfigurationElement
    {
        [ConfigurationProperty("code", IsRequired = true)]
        public string Code
        {
            get
            {
                return this["code"] as string;
            }
        }

        [ConfigurationProperty("description", IsRequired = true)]
        public string Description
        {
            get
            {
                return this["description"] as string;
            }
        }
    }

    public class ConfigElementCollection : ConfigurationElementCollection
    {
        public EnvironmentElement this[int index]
        {
            get
            {
                return base.BaseGet(index) as EnvironmentElement;
            }

        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new EnvironmentElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((EnvironmentElement)(element)).Code;
        }
    }

    public class EnvironmentsSection : ConfigurationSection
    {

        public EnvironmentsSection()
        {

        }

        [ConfigurationProperty("environmentCollection")]
        public ConfigElementCollection AllValues
        {
            get
            {
                return this["environmentCollection"] as ConfigElementCollection;
            }
        }

        public static EnvironmentsSection GetEnvironmentsSection()
        {
            return ConfigurationManager.GetSection("environmentsSection") as EnvironmentsSection;
        }
    }
}