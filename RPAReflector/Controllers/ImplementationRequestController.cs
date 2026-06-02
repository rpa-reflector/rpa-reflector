
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web.Http;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace RPAReflector.Controllers
{
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    public class ImplementationRequestController : ApiController
    {

        private static Exception ContainsTargetException(Exception ex, string exceptionType)
        {
            Exception currentException = ex;
            while (currentException != null)
            {
                if (currentException.GetType().Name.Contains(exceptionType))
                {
                    return currentException;
                }
                
                currentException = currentException.InnerException;
            }

            return null;
        }

        void AddInnerExceptionDetails(Exception ex, int level, Dictionary<string, object> errorDict)
        {
            if (ex == null || ex.InnerException == null)
            {
                return;
            }

            var innerError = new Dictionary<string, object>
            {
                {"Type", ex.InnerException.GetType().ToString()},
                {"Message", ex.InnerException.Message},
                {"StackTrace", ex.InnerException.StackTrace}
            };

            foreach (DictionaryEntry data in ex.InnerException.Data)
            {
                innerError.Add(data.Key.ToString(), data.Value.ToString());
            }

            errorDict.Add($"InnerException_Level_{level}", innerError);
            AddInnerExceptionDetails(ex.InnerException, level + 1, innerError);
        }

        [Authorize]
        [HttpPost]
        [Route("api/implementation/{module}/execute")]
        public IHttpActionResult Execute()
        {
            try
            {
                string postPayload = Request.Content.ReadAsStringAsync().Result;
                API.LogRequest(Request, postPayload);

                var rd = Request.GetRouteData();
                if (!rd.Values.TryGetValue("module", out object targetModule) ||
                    !DynamicCompiler.RequestHandlerTypes.ContainsKey((string)targetModule))
                    return BadRequest($"RequestHandlerType for module [{targetModule}] is not set! Please set implementation code before attempting to execute any implementation requests!");

                Type handlerType = DynamicCompiler.RequestHandlerTypes[(string)targetModule];
                var requestDoc = new XmlDocument();
                requestDoc.LoadXml(postPayload);

                var responseDoc = new XmlDocument();
                var handler = CreateRequestHandler(handlerType);
                InvokeHandler(handler, handlerType, requestDoc, responseDoc);

                return BuildXmlResponse(responseDoc);
            }
            catch (XmlException xmlEx)
            {
                return BadRequest("Invalid XML provided: " + xmlEx.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        private object CreateRequestHandler(Type type)
        {
            var handler = type.Assembly.CreateInstance(type.FullName)
                ?? throw new Exception("requestHandler is null!");

            SetRequiredProperty(type, handler, "WebRootPath", Filesystem.GetWebRootPath());
            SetRequiredProperty(type, handler, "MsgControlId", Guid.NewGuid().ToString());
            SetRequiredProperty(type, handler, "PayloadFolder", "payload");

            return handler;
        }

        private static void SetRequiredProperty(Type type, object target, string propertyName, object value)
        {
            var prop = type.GetProperty(propertyName)
                ?? throw new Exception($"RequestHandler does not expose a {propertyName} property!");
            prop.SetValue(target, value, null);
        }

        private void InvokeHandler(object handler, Type type, XmlDocument requestDoc, XmlDocument responseDoc)
        {
            var mi = type.GetMethod("HandleXMLRequest")
                ?? throw new Exception($"Could not find HandleXMLRequest in type {type.FullName}!");

            mi.Invoke(handler, new object[] { "Fernandez", requestDoc, responseDoc, Request.GetQueryNameValuePairs() });
        }

        private IHttpActionResult BuildXmlResponse(XmlDocument responseDoc)
        {
            bool prettyPrint = Request.Headers.TryGetValues("Accept", out var acceptValues)
                && acceptValues.Contains("text/xml");

            string mediaType = prettyPrint ? "text/xml" : "application/xml";
            var settings = prettyPrint
                ? new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true, IndentChars = "  ", NewLineChars = "\n", NewLineHandling = NewLineHandling.Replace }
                : new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false };

            using (var sw = new Utf8StringWriter())
            using (var writer = XmlWriter.Create(sw, settings))
            {
                responseDoc.Save(writer);
                return ResponseMessage(new HttpResponseMessage
                {
                    Content = new StringContent(sw.ToString(), Encoding.UTF8, mediaType)
                });
            }
        }

        private IHttpActionResult BuildErrorResponse(Exception ex, System.Net.HttpStatusCode statusCode)
        {
            var error = new Dictionary<string, object>
            {
                {"Type", ex.GetType().ToString()},
                {"Message", ex.Message},
                {"StackTrace", ex.StackTrace}
            };

            foreach (DictionaryEntry data in ex.Data)
            {
                error.Add(data.Key.ToString(), data.Value.ToString());
            }

            AddInnerExceptionDetails(ex, 1, error);

            return base.ResponseMessage(new HttpResponseMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(error, Formatting.Indented)),
                StatusCode = statusCode
            });
        }

        private IHttpActionResult HandleException(Exception ex)
        {
            // Unwrap TargetInvocationException before any type-specific checks
            if (ex is TargetInvocationException tie && tie.InnerException != null)
            {
                ex = tie.InnerException;
            }

            var sqlEx = ContainsTargetException(ex, "SqlException");
            if (sqlEx != null){
                throw sqlEx;
            }

            return BuildTypedErrorResponse(ex);
        }

        private IHttpActionResult BuildTypedErrorResponse(Exception ex)
        {
            var knfEx = ContainsTargetException(ex, "KeyNotFoundException");
            if (knfEx != null)
            {
                return BuildErrorResponse(knfEx, System.Net.HttpStatusCode.NotFound);
            }

            var dmeEx = ContainsTargetException(ex, "DynamicModuleException");
            return BuildErrorResponse(dmeEx ?? ex, System.Net.HttpStatusCode.InternalServerError);
        }
    }
}
