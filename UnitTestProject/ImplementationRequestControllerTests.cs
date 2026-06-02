// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using Microsoft.Owin.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using RPAReflector;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace UnitTestProject
{
    public class TestDynamicModuleException : Exception
    {
        public TestDynamicModuleException(string message) : base(message) { }
    }

    public class TestSqlException : Exception
    {
        public TestSqlException(string message) : base(message) { }
    }

    public abstract class BaseRequestHandler
    {
        public string WebRootPath   { get; set; }
        public string MsgControlId  { get; set; }
        public string PayloadFolder { get; set; }

        public abstract void HandleXMLRequest(string id, XmlDocument req, XmlDocument res,
            IEnumerable<KeyValuePair<string, string>> queryParams);
    }

    public class SuccessRequestHandler : BaseRequestHandler
    {
        public override void HandleXMLRequest(string id, XmlDocument req, XmlDocument res,
            IEnumerable<KeyValuePair<string, string>> queryParams) { }
    }

    public class KeyNotFoundRequestHandler : BaseRequestHandler
    {
        public override void HandleXMLRequest(string id, XmlDocument req, XmlDocument res,
            IEnumerable<KeyValuePair<string, string>> queryParams)
            => throw new KeyNotFoundException("Resource not found");
    }

    public class DynamicModuleRequestHandler : BaseRequestHandler
    {
        public override void HandleXMLRequest(string id, XmlDocument req, XmlDocument res,
            IEnumerable<KeyValuePair<string, string>> queryParams)
            => throw new TestDynamicModuleException("Dynamic module error");
    }

    public class SqlExceptionRequestHandler : BaseRequestHandler
    {
        public override void HandleXMLRequest(string id, XmlDocument req, XmlDocument res,
            IEnumerable<KeyValuePair<string, string>> queryParams)
            => throw new TestSqlException("SQL error occurred");
    }

    public class GenericExceptionRequestHandler : BaseRequestHandler
    {
        public override void HandleXMLRequest(string id, XmlDocument req, XmlDocument res,
            IEnumerable<KeyValuePair<string, string>> queryParams)
            => throw new InvalidOperationException("Something went wrong");
    }

    [TestClass]
    public class ImplementationRequestControllerTests
    {
        private const string ValidXml  = "<?xml version=\"1.0\"?><Request/>";
        private const string InvalidXml = "this is not xml";
        private const string TestUser  = "testuser";
        private const string TestPass  = "testpass";

        /// <summary>Creates an authorized POST request to the execute endpoint.</summary>
        private static HttpRequestMessage Post(string module, string xml, string accept = "application/xml")
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestUser}:{TestPass}"));
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/implementation/{module}/execute")
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
            return request;
        }

        private static void Register(string module, Type handlerType)
            => DynamicCompiler.RequestHandlerTypes[module] = handlerType;

        private static void Unregister(string module)
            => DynamicCompiler.RequestHandlerTypes.Remove(module);

        private static async Task<HttpResponseMessage> ExecuteWithHandler(
            string module, Type handlerType, string accept = "application/xml")
        {
            Register(module, handlerType);
            try
            {
                using (var server = TestServer.Create<API>())
                using (var request = Post(module, ValidXml, accept))
                    return await server.HttpClient.SendAsync(request);
            }
            finally { Unregister(module); }
        }

        [TestMethod]
        public async Task Execute_NoCredentials_Returns401()
        {
            using (var server = TestServer.Create<API>())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/implementation/any/execute")
            {
                Content = new StringContent(ValidXml, Encoding.UTF8, "application/xml")
            })
            {
                var response = await server.HttpClient.SendAsync(request);
                Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [TestMethod]
        public async Task Execute_ModuleNotRegistered_Returns400WithModuleName()
        {
            Unregister("unregistered");
            using (var server = TestServer.Create<API>())
            using (var request = Post("unregistered", ValidXml))
            {
                var response = await server.HttpClient.SendAsync(request);
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
                StringAssert.Contains(await response.Content.ReadAsStringAsync(), "RequestHandlerType for module [unregistered] is not set!");
            }
        }

        [TestMethod]
        public async Task Execute_InvalidXml_Returns400WithMessage()
        {
            Register("any", typeof(SuccessRequestHandler));
            using (var server = TestServer.Create<API>())
            using (var request = Post("any", InvalidXml))
            {
                var response = await server.HttpClient.SendAsync(request);
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
                StringAssert.Contains(await response.Content.ReadAsStringAsync(), "Invalid XML provided");
            }
        }

        [TestMethod]
        public async Task Execute_Success_ApplicationXmlAccept_ReturnsApplicationXml()
        {
            var response = await ExecuteWithHandler("ok_appxml", typeof(SuccessRequestHandler));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("application/xml", response.Content.Headers.ContentType.MediaType);
        }

        [TestMethod]
        public async Task Execute_Success_TextXmlAccept_ReturnsTextXml()
        {
            var response = await ExecuteWithHandler("ok_textxml", typeof(SuccessRequestHandler), "text/xml");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("text/xml", response.Content.Headers.ContentType.MediaType);
        }

        [TestMethod]
        public async Task Execute_KeyNotFoundException_Returns404WithErrorJson()
        {
            var response = await ExecuteWithHandler("knf", typeof(KeyNotFoundRequestHandler));
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.IsNotNull(body["Type"]);
            Assert.AreEqual("Resource not found", (string)body["Message"]);
        }

        [TestMethod]
        public async Task Execute_DynamicModuleException_Returns500WithErrorJson()
        {
            var response = await ExecuteWithHandler("dme", typeof(DynamicModuleRequestHandler));
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            StringAssert.Contains((string)body["Type"], "DynamicModuleException");
            Assert.AreEqual("Dynamic module error", (string)body["Message"]);
        }

        [TestMethod]
        public async Task Execute_SqlException_IsRethrown_Returns500()
        {
            var response = await ExecuteWithHandler("sql", typeof(SqlExceptionRequestHandler));
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [TestMethod]
        public async Task Execute_GenericException_Returns500WithErrorJson()
        {
            var response = await ExecuteWithHandler("generic", typeof(GenericExceptionRequestHandler));
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.IsNotNull(body["Type"]);
            Assert.AreEqual("Something went wrong", (string)body["Message"]);
        }
    }
}