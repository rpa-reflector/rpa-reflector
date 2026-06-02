namespace RPAReflector
{
    using Microsoft.Owin;
    using System;
    using System.Configuration;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;

    public class BasicAuthenticationMiddleware : OwinMiddleware
    {
        public BasicAuthenticationMiddleware(OwinMiddleware next) : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            string[] credentials = ParseCredentials(context.Request.Headers["Authorization"]);
            if (credentials == null)
            {
                context.Response.StatusCode = 401;
                return;
            }

            string username = credentials[0];
            string password = credentials[1];

            if (IsAuthorized(username, password))
            {
                var claims = new[] {
                        new Claim(ClaimTypes.Name, username)
                    };
                var identity = new ClaimsIdentity(claims, "Basic");
                context.Request.User = new ClaimsPrincipal(identity);
            }
            else
            {
                context.Response.StatusCode = 401;
                return;
            }

            await Next.Invoke(context);
        }

        private string[] ParseCredentials(string authHeader)
        {
            try
            {
                if (authHeader != null && authHeader.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
                {
                    string token = authHeader.Substring("Basic".Length).Trim();
                    string credentialString = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                    string[] credentials = credentialString.Split(':');                    
                    if (credentials.Length == 2)
                    {
                        return credentials;
                    }
                }
            }
            catch
            {
                // Ignore parsing errors and return null to indicate invalid credentials
            }
            
            return null;
        }

        private bool IsAuthorized(string username, string password)
        {
            string apiUsername = ConfigurationManager.AppSettings["ApiUsername"];
            if (apiUsername == null || apiUsername.Length == 0)
            {
                throw new Exception("ApiUsername cannot be empty!");
            }
            
            string apiPassword = ConfigurationManager.AppSettings["ApiPassword"];
            if (apiPassword == null || apiPassword.Length == 0)
            {
                throw new Exception("ApiPassword cannot be empty!");
            }

            return username == apiUsername && password == apiPassword;
        }
    }
}
