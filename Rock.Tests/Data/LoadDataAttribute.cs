namespace Rock.Tests.Data
{
    [System.AttributeUsage( System.AttributeTargets.Class, AllowMultiple = false )]
    public class LoadDataAttribute : System.Attribute
    {
        public string Source { get; set; }

        public LoadDataAttribute( string source )
        {
            Source = source;
        }
    }
}
