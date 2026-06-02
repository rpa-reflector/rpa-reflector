// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace RPAReflector.Controllers
{
    [DataContract(Name = "Info", Namespace = "RPAReflector")]
    internal class RPAReflectorInfo
    {
        [DataMemberAttribute]
        public string Status;

        [DataMemberAttribute]
        public string HixVersion;

        [DataMemberAttribute]
        public string HixVersionConfigured;

        public string HixSelectedEnvironment;

        [DataMemberAttribute]
        public Dictionary<string, string> ImplementationModules;

        [DataMemberAttribute]
        public List<string> LoadedAssemblies;
    }


    [CollectionDataContract]
    internal class LogicsCollection : Collection<string>
    {
    }

    [Authorize]
    public class SystemController : ApiController
    {
        [HttpGet]
        [Route("api/system/info")]
        public ResponseMessageResult GetInfo()
        {
            API.LogRequest(Request);
            List<String> assemblyNames = new List<String>();
            foreach (Assembly assembly in AssemblyLoader.GetLoadedAssemblies())
            {
                assemblyNames.Add(assembly.FullName);
            }

            var result = new RPAReflectorInfo {
                Status = "Running",
                HixVersion = Integration.HixVersionDetected?.FileVersion,
                HixVersionConfigured = ConfigurationManager.AppSettings["HixVersion"],
                HixSelectedEnvironment = ConfigurationManager.AppSettings["SelectedEnvironment"],
                ImplementationModules = DynamicCompiler.ImplementationVersions,
                LoadedAssemblies = assemblyNames
            };

            var response = Request.CreateResponse(HttpStatusCode.OK, result);
            
            return ResponseMessage(response);
        }

        [HttpPost]
        [Route("api/system/fully-cached-logics")]

        public IHttpActionResult SetFullyCachedLogics([FromBody] List<string> postedLogics)
        {
            API.LogRequest(Request);
            
            if (postedLogics == null)
            {
                return BadRequest("The request body is null or invalid.");
            }

            // Currently only 6.1 env provider supports this
            PropertyInfo fctp = Integration.EnvironmentProvider.GetType()
                .GetProperty("CurrentFullyCachedLogics", BindingFlags.Static | BindingFlags.Public);
            if (fctp == null)
            {
                return base.ResponseMessage(new HttpResponseMessage(HttpStatusCode.NotImplemented));
            }

            List<string> logicList = (List<string>)fctp.GetValue(null);
            logicList.Clear();
            foreach (string str in postedLogics)
            {
                logicList.Add(str);
            }

            return Ok(logicList);
        }

        [HttpGet]
        [Route("api/system/fully-cached-logics")]
        public IHttpActionResult GetFullyCachedLogics()
        {
            API.LogRequest(Request);

            // Currently only 6.1 env provider supports this
            PropertyInfo fctp = Integration.EnvironmentProvider.GetType()
                .GetProperty("CurrentFullyCachedLogics", BindingFlags.Static | BindingFlags.Public);
            if (fctp == null)
            {
                return base.ResponseMessage(new HttpResponseMessage(HttpStatusCode.NotImplemented));
            }

            try
            {
                LogicsCollection currentFullyCachedLogics = new LogicsCollection();
                List<string> logicList = (List<string>)fctp.GetValue(null);

                foreach (string logic in logicList)
                {
                    currentFullyCachedLogics.Add(logic);
                }

                return Ok(currentFullyCachedLogics);
            }
            catch (Exception ex)
            {
                return base.ResponseMessage(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        ex.Message,
                        Encoding.UTF8,
                        "text/plain"
                    )

                });
            }
        }

        [HttpPost]
        [Route("api/system/load-implementation")]
        public async Task<IHttpActionResult> LoadImplementationAsync()
        {
            API.LogRequest(Request);
            
            string implementationContents = null;

            if (this.Request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MultipartFormDataStreamProvider(Filesystem.GetTemporaryDirectory());
                await Request.Content.ReadAsMultipartAsync(streamProvider);
                foreach (MultipartFileData fileData in streamProvider.FileData)
                {
                    IEnumerable<string> values;
                    if (fileData.Headers.TryGetValues("Content-Disposition", out values))
                    {
                        string contentDisposition = values.First();
                        string pattern = @"name=""([^""]+)""";

                        Match match = Regex.Match(contentDisposition, pattern);
                        if (match.Success)
                        {
                            string nameValue = match.Groups[1].Value;
                            EventLog.WriteEntry(Program.EventSource, "Received implementation code for module [" + nameValue + "] on path " + fileData.LocalFileName, EventLogEntryType.Information);

                            using (StreamReader streamReader = new StreamReader(fileData.LocalFileName, Encoding.UTF8))
                            {
                                implementationContents = streamReader.ReadToEnd();
                            }

                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();
                   
                            try
                            {
                                //do we have a version specification for this implemention within the multipart request?
                                string implementationName = nameValue;
                                string strVersion = null;
                                if (nameValue.Contains("-"))
                                {

                                    int lastSepPos = nameValue.LastIndexOf('-');
                                   
                                    if (lastSepPos != -1)
                                    {
                                        implementationName = nameValue.Substring(0, lastSepPos);
                                        strVersion = nameValue.Substring(lastSepPos + 1);
                                        EventLog.WriteEntry(Program.EventSource, "Received tagged payload of version " + strVersion + "...", EventLogEntryType.Information);

                                    }
                                }
                                DynamicCompiler.CompileImplementation(implementationName, implementationContents, DynamicCompiler.GetImplementationCompilationParameters(), strVersion);
                            }
                            catch (DynamicCompiler.CompilationException ex)
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine("Compilation errors in module " + nameValue + ":");

                                foreach (var error in ex.errors)
                                {
                                    sb.AppendLine(error.ToString());
                                }

                                return base.ResponseMessage(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                {
                                    Content = new StringContent(
                                       sb.ToString(),
                                       Encoding.UTF8,
                                       "text/plain"
                                    )

                                });
                            }
                            finally
                            {
                                stopwatch.Stop();
                            }

                            // Get the elapsed time as a TimeSpan value
                            TimeSpan ts = stopwatch.Elapsed;
                            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                            Console.WriteLine("Compilation of implementation code for module " + nameValue + " took " + elapsedTime + "!");

                            return Ok();

                        }
                        
                    }
                    
                }
               
            }


            return base.ResponseMessage(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    "Please perform multipart http requests with (optionally) versioned implementation modules as a file payload!",
                    Encoding.UTF8,
                    "text/plain"
                )
            });
        }
    }
}