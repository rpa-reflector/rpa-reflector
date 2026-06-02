// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using RPAReflector;
using Microsoft.Owin.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject
{
    [TestClass]
    public class ApiTests
    {
        [TestMethod]
        public async Task Test_GetValues_Unauthorized()
        {
            RPAReflector.Integration.PrepareEnvironment();
            using (var server = TestServer.Create<API>())
            {
                var response = await server.HttpClient.GetAsync("/api/system/info");
                Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [TestMethod]
        public async Task Test_GetValues_Authorized()
        {
            RPAReflector.Integration.PrepareEnvironment();
            using (var server = TestServer.Create<API>())
            {
                var byteArray = Encoding.ASCII.GetBytes("testuser:testpass");
                var base64String = Convert.ToBase64String(byteArray);
                var client = server.HttpClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);

                // Check if the header is set
                var headers = client.DefaultRequestHeaders.ToString();
                Console.WriteLine(headers);  // Or use Debug.WriteLine(headers);

                HttpResponseMessage response = null;
                using (var requestMessage =  new HttpRequestMessage(HttpMethod.Get, "/api/system/info"))
                {
                    requestMessage.Headers.Authorization =
                        new AuthenticationHeaderValue("Basic", base64String);

                    response =  await client.SendAsync(requestMessage);
                }

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                JObject body = JObject.Parse(json);

                Assert.AreEqual("Running", (string)body["Status"]);
            }
        }
    }
}