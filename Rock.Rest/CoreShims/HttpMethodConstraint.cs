using Microsoft.AspNetCore.Routing.Constraints;

namespace Rock.Rest
{
    public class HttpMethodConstraint : HttpMethodRouteConstraint
    {
        public HttpMethodConstraint( string[] allowedMethods )
            : base( allowedMethods )
        {
        }
    }
}
