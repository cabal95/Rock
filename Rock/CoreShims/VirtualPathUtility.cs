namespace System.Web
{
    public static class VirtualPathUtility
    {
        public static string ToAbsolute( string relativeUrl )
        {
            var httpContext = HttpContext.Current;
            return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}" + relativeUrl.Replace( "~/", "/" );
        }
    }
}
