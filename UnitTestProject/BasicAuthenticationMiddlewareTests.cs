// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using Microsoft.Owin.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RPAReflector;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject
{
    /// <summary>
    /// Tests for BasicAuthenticationMiddleware using the OWIN in-memory TestServer.
    /// These tests do NOT require Integration.PrepareEnvironment() or any HiX DLLs.
    ///
    /// Credentials used: ApiUsername="testuser" / ApiPassword="testpass"
    /// (configured in app.config of this test project).
    ///
    /// The tests hit api/system/info as a probe endpoint because SystemController
    /// is decorated with [Authorize] and has no HiX-dependency in the GET path.
    /// </summary>
    [TestClass]
    public class BasicAuthenticationMiddlewareTests
    {
        private static readonly string ProbeUrl = "/api/system/info";

        // Helper: build a Basic-auth header value
        private static string BasicHeader(string username, string password)
        {
            string raw = $"{username}:{password}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        // Helper: send a GET to ProbeUrl with an optional Authorization header
        private static async Task<HttpResponseMessage> SendRequest(
            TestServer server,
            string authHeaderValue = null)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, ProbeUrl))
            {
                if (authHeaderValue != null)
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Basic", authHeaderValue);
                }
                return await server.HttpClient.SendAsync(request);
            }
        }

        // --- No credentials ---

        [TestMethod]
        public async Task NoAuthHeader_PassesThroughMiddleware_ButControllerReturnsUnauthorized()
        {
            // Without any Authorization header the middleware lets the request through
            // but the [Authorize] attribute on SystemController still returns 401.
            using (var server = TestServer.Create<API>())
            {
                var response = await SendRequest(server);

                Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        // --- Wrong credentials ---

        [TestMethod]
        public async Task WrongPassword_MiddlewareReturns401()
        {
            using (var server = TestServer.Create<API>())
            {
                var response = await SendRequest(server, BasicHeader("testuser", "wrongpassword"));

                Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [TestMethod]
        public async Task WrongUsername_MiddlewareReturns401()
        {
            using (var server = TestServer.Create<API>())
            {
                var response = await SendRequest(server, BasicHeader("nobody", "testpass"));

                Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [TestMethod]
        public async Task BothWrong_MiddlewareReturns401()
        {
            using (var server = TestServer.Create<API>())
            {
                var response = await SendRequest(server, BasicHeader("bad", "bad"));

                Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        // --- Correct credentials ---

        [TestMethod]
        public async Task CorrectCredentials_MiddlewareAllowsRequest_ControllerResponds()
        {
            using (var server = TestServer.Create<API>())
            {
                var response = await SendRequest(server, BasicHeader("testuser", "testpass"));

                // 401 would mean the middleware rejected the credentials.
                // Any other status (200, 500, ...) means the middleware passed the request on.
                Assert.AreNotEqual(
                    HttpStatusCode.Unauthorized,
                    response.StatusCode,
                    "Valid credentials should not produce a 401 from the auth middleware");
            }
        }

        // --- Malformed Authorization header ---

        [TestMethod]
        public async Task MalformedBase64_DoesNotCrashServer()
        {
            using (var server = TestServer.Create<API>())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, ProbeUrl))
                {
                    // Bypass AuthenticationHeaderValue validation by setting the raw header
                    request.Headers.TryAddWithoutValidation("Authorization", "Basic !!!not-base64!!!");
                    var response = await server.HttpClient.SendAsync(request);

                    // The server should return some HTTP response rather than crashing
                    Assert.IsNotNull(response);
                }
            }
        }

        [TestMethod]
        public async Task EmptyBase64Payload_DoesNotCrashServer()
        {
            using (var server = TestServer.Create<API>())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, ProbeUrl))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Basic ");
                    var response = await server.HttpClient.SendAsync(request);

                    Assert.IsNotNull(response);
                }
            }
        }
    }
}