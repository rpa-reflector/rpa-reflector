// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;

namespace RPAReflector
{    
    [Authorize]
    public class ObjectFieldValueController : ApiController
    {
        [HttpGet, HttpPost]
        [Route("api/object-field-value/{logicname}/{id}")]
        public async Task<IHttpActionResult> GetValues()
        {
            API.LogRequest(Request);

            if (Integration.HixProvider.EzisUtil2RegisterGuidListHolderType == null)
                throw new Exception("EzisUtil2Type could not be found! Are you missing installed assemblies?");

            if (!TryGetRouteValues(out object targetId, out object targetLogicName))
                return BadRequest("Please supply a patientIdentifier!");

            try
            {
                var obj = SpawnObject(targetLogicName, targetId);
                if (obj == null)
                    return NotFound();

                object veldenValue = obj.GetType().GetProperty("Velden").GetValue(obj, null);

                return Request.Method == HttpMethod.Post
                    ? await HandlePostAsync(veldenValue)
                    : HandleGet(veldenValue);
            }
            catch (Exception ex)
            {
                return InternalServerError(UnwrapException(ex));
            }
        }

        private bool TryGetRouteValues(out object id, out object logicName)
        {
            var rd = Request.GetRouteData();
            bool hasId = rd.Values.TryGetValue("id", out id);
            bool hasLogicName = rd.Values.TryGetValue("logicname", out logicName);
            return hasId && hasLogicName;
        }

        private static Exception UnwrapException(Exception ex) =>
            ex.GetType().FullName == "System.Reflection.TargetInvocationException" ? ex.InnerException : ex;

        private object SpawnObject(object logicName, object id)
        {
            MethodInfo getClassId = Integration.HixProvider.EzisUtil2RegisterGuidListHolderType
                .GetMethod("GetClassIdFromName", BindingFlags.Static | BindingFlags.Public);
            var classId = getClassId.Invoke(null, new[] { logicName });
            if (classId == null)
                return null;

            MethodInfo createObj = Integration.HixProvider.BaseUtilType
                .GetMethod("CreateObjectByClassID", BindingFlags.Public | BindingFlags.Static);
            var logicObj = createObj.Invoke(null, new[] { classId });

            MethodInfo spawn = logicObj.GetType().GetMethod("Spawn");
            var obj = spawn.Invoke(logicObj, new[] { id });
            var objectState = obj?.GetType().GetProperty("ObjectState")?.GetValue(obj)?.ToString();
            return objectState == "osValid" ? obj : null;
        }

        private async Task<IHttpActionResult> HandlePostAsync(object veldenValue)
        {
            MethodInfo fieldByFieldPath = Integration.HixProvider.EzisUtil2UtilitiesHolderType
                .GetMethod("FieldByFieldPathExtended", BindingFlags.Static | BindingFlags.Public);

            return Request.Content.IsMimeMultipartContent()
                ? await HandleMultipartPostAsync(veldenValue, fieldByFieldPath)
                : HandleXmlPost(veldenValue, fieldByFieldPath);
        }

        private async Task<IHttpActionResult> HandleMultipartPostAsync(object veldenValue, MethodInfo fieldByFieldPath)
        {
            var streamProvider = new MultipartFormDataStreamProvider(Filesystem.GetTemporaryDirectory());
            await Request.Content.ReadAsMultipartAsync(streamProvider);

            var result = new ObjectFieldValueCollection();
            var formFields = streamProvider.Contents
                .Where(c => c.Headers.ContentDisposition.DispositionType.Equals("form-data")
                         && string.IsNullOrEmpty(c.Headers.ContentDisposition.FileName));

            foreach (var content in formFields)
            {
                string key = content.Headers.ContentDisposition.Name.Trim('"');
                string fieldPath = await content.ReadAsStringAsync();
                result.Add(key, TryGetDisplayValue(fieldByFieldPath, veldenValue, fieldPath));
            }

            return Ok(result);
        }

        private IHttpActionResult HandleXmlPost(object veldenValue, MethodInfo fieldByFieldPath)
        {
            string payload = Request.Content.ReadAsStringAsync().Result;
            var doc = new XmlDocument();
            doc.LoadXml(payload);

            var result = new ObjectFieldValueCollection();
            foreach (XmlElement el in doc.DocumentElement.ChildNodes
                .OfType<XmlElement>()
                .Where(e => e.HasAttribute("name") && e.HasAttribute("fieldPath")))
            {
                result.Add(el.GetAttribute("name"), TryGetDisplayValue(fieldByFieldPath, veldenValue, el.GetAttribute("fieldPath")));
            }

            return Ok(result);
        }

        private IHttpActionResult HandleGet(object veldenValue)
        {
            if (!(veldenValue is IEnumerable veldCollection))
                return NotFound();

            var result = new ObjectFieldValueCollection();
            foreach (var veld in veldCollection)
            {
                var definingVeld = veld.GetType().GetProperty("DefiningVeld").GetValue(veld);
                string name = definingVeld.GetType().GetProperty("Naam").GetValue(definingVeld) as string;
                string value = veld.GetType().GetProperty("DisplayValue").GetValue(veld) as string;
                result[name] = value;
            }

            return Ok(result);
        }

        private static string TryGetDisplayValue(MethodInfo fieldByFieldPath, object veldenValue, string fieldPath)
        {
            try
            {
                var field = fieldByFieldPath.Invoke(veldenValue, new object[] { veldenValue, fieldPath });
                return field?.GetType().GetProperty("DisplayValue").GetValue(field) as string ?? "";
            }
            catch
            {
                return null;
            }
        }
    }
}