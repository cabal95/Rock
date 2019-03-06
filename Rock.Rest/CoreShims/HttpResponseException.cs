using System;

namespace Rock.Rest
{
    public class HttpResponseException : Exception
    {
        public System.Net.Http.HttpResponseMessage Response { get; set; }

        public HttpResponseException( System.Net.HttpStatusCode code )
            : base( $"Error { code }" )
        {
        }

        public HttpResponseException( System.Net.Http.HttpResponseMessage response )
        {
            Response = response;
        }
    }
}
