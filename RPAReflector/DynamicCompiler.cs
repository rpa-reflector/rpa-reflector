// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT


using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using System.IO;
using System.CodeDom;

namespace RPAReflector
{
    public class DynamicCompiler
    {
        public static Dictionary<string, Type> RequestHandlerTypes { get; private set; } = new Dictionary<string, Type>();
        public static Dictionary<string, string> ImplementationVersions { get; private set; } = new Dictionary<string, string>();

        public static CompilerParameters GetImplementationCompilationParameters()
        {
            CompilerParameters parameters = new CompilerParameters
            {
                GenerateInMemory = true,
                ReferencedAssemblies = { "mscorlib.dll", "System.dll", "System.Xml.dll", "System.Core.dll", "System.Xml.Linq.dll", "System.Drawing.dll", "System.Xml.Serialization.dll", "System.Net.Http.dll" }
            };

            foreach (Assembly loadedAssembly in AssemblyLoader.GetLoadedAssemblies())
            {
                parameters.ReferencedAssemblies.Add(loadedAssembly.Location);
            }

            return parameters;
        }

        public class CompilationException : Exception
        {
            public CompilationException(string message, CompilerErrorCollection errors) : base(message)
            {
                this.errors = errors;
            }

            public CompilerErrorCollection errors;
        }

        public static Assembly CompileImplementation(string implementationName, string sourceCode, CompilerParameters compar, string version)
        {
            // Compile the source code dynamically
            ImplementationVersions[implementationName] = version != null ? ("v" + version) : "Unknown";
            CompilerResults results = CompileSourceCode(sourceCode, compar, version);
            ThrowIfCompilationErrors(results);

            foreach (Type t in results.CompiledAssembly.GetTypes())
            {
                //todo: make annotation for this
                if (t.Name.Equals("RequestHandler"))
                {
                    RequestHandlerTypes[implementationName] = t;
                }
            }

            return results.CompiledAssembly;
        }

        public static object CompileEnvProvider(CompilerParameters comPar, HixVersionStrategy HixProvider, string env)
        {
            string resourceName = $"RPAReflector.EnvProviderTemplates.{HixProvider.EnvProviderResourceName}";
            string sourceCode;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
                sourceCode = reader.ReadToEnd();

            CompilerResults results = CompileSourceCode(sourceCode, comPar, null);
            ThrowIfCompilationErrors(results);

            Console.WriteLine("Environment provider compiled successfully!");

            // Load the compiled assembly and create an instance of the class
            Assembly compiledAssembly = results.CompiledAssembly;

            foreach (Type t in compiledAssembly.DefinedTypes)
            {
                Console.WriteLine("Creating instance of Environment provider...");
                object envProviderInstance = null;

                try
                {
                    envProviderInstance = Activator.CreateInstance(t, env);
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry(Program.EventSource, "Error setting up envProvider: " + ex.InnerException.Message, EventLogEntryType.Information);
                    throw new Exception("Error setting up envProviderInstance!", ex.InnerException);
                }

                return envProviderInstance;
            }
            return null;
        }

        private static void ThrowIfCompilationErrors(CompilerResults results)
        {
            if (!results.Errors.HasErrors)
                return;

            Console.WriteLine("Compilation errors:");
            foreach (CompilerError error in results.Errors)
                Console.WriteLine(error.ErrorText);

            throw new CompilationException("", results.Errors);
        }

        private static CompilerResults CompileSourceCode(string sourceCode, CompilerParameters comPar, string version)
        {
            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                var unit = new CodeCompileUnit();
                var attr = new CodeTypeReference(typeof(AssemblyVersionAttribute));
                
                if (version != null)
                {
                    var decl = new CodeAttributeDeclaration(attr, new CodeAttributeArgument(new CodePrimitiveExpression(version)));
                    unit.AssemblyCustomAttributes.Add(decl);
                    EventLog.WriteEntry(Program.EventSource, "Setting compiled assembly version to: " + version, EventLogEntryType.Information);
                }
                else
                {
                    EventLog.WriteEntry(Program.EventSource, "Compiled assembly does not contain version info", EventLogEntryType.Information);
                }

                var assemblyInfo = new StringWriter();
                provider.GenerateCodeFromCompileUnit(unit, assemblyInfo, new CodeGeneratorOptions());

                return provider.CompileAssemblyFromSource(comPar, sourceCode, assemblyInfo.ToString());
            }
        }
    }
}