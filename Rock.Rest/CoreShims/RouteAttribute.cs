namespace System.Web.Http
{
    public class RouteAttribute : Microsoft.AspNetCore.Mvc.RouteAttribute
    {
        public RouteAttribute( string template )
            : base( template )
        {
        }
    }
}
