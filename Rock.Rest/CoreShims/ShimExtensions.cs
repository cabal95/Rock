using System.Linq;

namespace Rock.Rest
{
    public static class HttpRequestExtensions
    {
        public static string GetUserHostAddress( this Microsoft.AspNetCore.Http.HttpRequest request )
        {
            string ipAddress = null;

            if ( request.Headers.ContainsKey( "X-Forwarded-For" ) )
            {
                ipAddress = request.Headers["X-Forwarded-For"].First();
            }

            if ( !string.IsNullOrEmpty( ipAddress ) )
            {
                string[] addresses = ipAddress.Split( ',' );
                if ( addresses.Length != 0 )
                {
                    return addresses[0];
                }
            }
            else
            {
                return request.HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return string.Empty;
        }

        public static string GetHeader( this Microsoft.AspNetCore.Http.HttpRequest request, string header )
        {
            if ( request.Headers.ContainsKey( header ) )
            {
                return request.Headers[header].First();
            }

            return string.Empty;
        }
    }
}
