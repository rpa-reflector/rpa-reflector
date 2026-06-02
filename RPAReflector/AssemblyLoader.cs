using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RPAReflector
{
    internal class AssemblyLoader
    {       
        private static List<string> _patterns = new List<string>
        {
            // All HiX versions
            "Chipsoft.Publics.*.dll",
            "Chipsoft.Services.*.dll",
            "Borland.*.dll",
            "Borland.Vcl.resources.dll",
            "ChipSoft.Comez.RTL.dll",
            "ChipSoft.Comez.SSL.dll",
            "ChipSoft.FCL.Base.dll",
            "ChipSoft.FCL.ExtendedBase.dll",
            "ChipSoft.FCL.ClassRegistry.dll",
            "ChipSoft.FCL.RBL.dll",
            "ChipSoft.FCL.RTL.dll",
            "ChipSoft.FCL.RTL.Logging.dll",
            "ChipSoft.FLCL.UZI.Core.dll",
            "ChipSoft.FCL.UZI.dll",
            "ChipSoft.Comez.Watches.dll",
            "ChipSoft.Publics.Comez.dll",
            "ChipSoft.Comez.EzisUtil2.dll",

            // 6.3
            "ChipSoft.Core.Base.dll",
            "ChipSoft.Core.Base.NetFramework.dll"
        };

        private static List<Assembly> _loadedAssemblies = new List<Assembly>();

        public static List<Assembly> GetLoadedAssemblies()
        {
            return _loadedAssemblies;
        }

        public static List<Assembly> LoadAssemblies(string version, HixVersionStrategy versionStrategy)
        {
            string[] basePaths = { @"C:\Windows\Microsoft.NET\assembly", @"C:\Windows\assembly" };
            Regex fileRegex = new Regex(GenerateRegexPattern(_patterns), RegexOptions.IgnoreCase);

            foreach (string basePath in basePaths)
            {
                var matchingFiles = new DirectoryInfo(basePath)
                    .GetDirectories("*", SearchOption.AllDirectories)
                    .Where(dir => IsVersionMatchingDirectory(dir, version))
                    .SelectMany(dir => dir.GetFiles())
                    .Where(file => fileRegex.IsMatch(file.Name));

                foreach (var file in matchingFiles)
                    LoadAssembly(file);
            }

            _loadedAssemblies.AddRange(versionStrategy.LoadAdditionalAssemblies());

            return _loadedAssemblies;
        }

        private static bool IsVersionMatchingDirectory(DirectoryInfo dir, string version)
        {
            bool isChipSoft = dir.FullName.ToLower().Contains("chipsoft");
            return !isChipSoft || dir.Name.Contains(version + "__");
        }

        private static void LoadAssembly(FileInfo file)
        {
            try
            {
                _loadedAssemblies.Add(Assembly.LoadFrom(file.FullName));
                Console.WriteLine($"Loaded: {file.FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load {file.FullName}: {ex.Message}");
            }
        }

        public static object CreateInstance(Assembly assembly, string typeName, object[] constructorArgs)
        {
            Type type = assembly.GetType(typeName);
            if (type == null)
            {
                Console.WriteLine($"Type {typeName} not found in assembly.");
                return null;
            }

            try
            {
                return Activator.CreateInstance(type, constructorArgs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating instance of {typeName}: {ex.Message}");
                return null;
            }
        }

        private static string GenerateRegexPattern(List<string> patterns)
        {
            return "^(" + string.Join("|", patterns.Select(p => Regex.Escape(p).Replace("\\*", ".*"))) + ")$";
        }
    }
}
