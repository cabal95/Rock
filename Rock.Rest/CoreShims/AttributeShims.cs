namespace System.Web.Http
{
    public class HttpGetAttribute : Microsoft.AspNetCore.Mvc.HttpGetAttribute { }
    public class HttpPostAttribute : Microsoft.AspNetCore.Mvc.HttpPostAttribute { }
    public class HttpPutAttribute : Microsoft.AspNetCore.Mvc.HttpPutAttribute { }
    public class HttpDeleteAttribute : Microsoft.AspNetCore.Mvc.HttpDeleteAttribute { }

    public class FromBodyAttribute : Microsoft.AspNetCore.Mvc.FromBodyAttribute { }
    public class FromUriAttribute : Microsoft.AspNetCore.Mvc.FromQueryAttribute { }
}
