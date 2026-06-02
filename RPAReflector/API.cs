// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Host.HttpListener;
using Microsoft.Owin.StaticFiles;
using Owin;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace RPAReflector
{
    public class API
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();

            // Configure Web API routes
            config.MapHttpAttributeRoutes();

            var options = new FileServerOptions
            {
                RequestPath = new PathString("/payload"),
                FileSystem = new PhysicalFileSystem(Filesystem.GetWebPayloadPath()),
#if DEBUG
                EnableDirectoryBrowsing = true
#else
                EnableDirectoryBrowsing = false
#endif
            };
            app.UseFileServer(options);

            app.Use<BasicAuthenticationMiddleware>();
            app.UseWebApi(config);

            var owinListenerName = "Microsoft.Owin.Host.HttpListener.OwinHttpListener";
            if (app.Properties.TryGetValue(owinListenerName, out object listenerObj) && listenerObj is OwinHttpListener owinListener)
            {
                owinListener.SetRequestQueueLimit(10 * 1024 * 1024);
            }
        }

        public static void LogRequest(HttpRequestMessage request, string postPayload)
        {
            var context = request.GetOwinContext() as IOwinContext;


            string queryParams = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "No query parameters";
            StringBuilder headerBuilder = new StringBuilder();
            foreach (var header in context.Request.Headers)
            {
                headerBuilder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            string headers = headerBuilder.ToString();
            string message = $"URI: {context.Request.Uri} Method: {context.Request.Method}\nQuery Parameters: {queryParams}\nHeaders: {headers}\nPOST Payload: {postPayload}";
            WriteToEventLog(message);
        }

        public static void LogRequest(HttpRequestMessage request)
        {
            var context = request.GetOwinContext() as IOwinContext;


            string queryParams = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "No query parameters";
            StringBuilder headerBuilder = new StringBuilder();
            foreach (var header in context.Request.Headers)
            {
                headerBuilder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            string headers = headerBuilder.ToString();
            string message = $"URI: {context.Request.Uri} Method: {context.Request.Method}\nQuery Parameters: {queryParams}\nHeaders: {headers}";
            WriteToEventLog(message);
        }

        private static void WriteToEventLog(string message)
        {
            try
            {
                if (!EventLog.SourceExists(Program.EventSource))
                {
                    EventLog.CreateEventSource(Program.EventSource, Program.EventLogName);
                }

                EventLog.WriteEntry(Program.EventSource, message, EventLogEntryType.Information);
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"Failed to log to Event Log: {ex.Message}");
            }
        }
    }
}