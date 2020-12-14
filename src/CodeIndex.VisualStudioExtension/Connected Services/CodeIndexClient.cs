namespace CodeIndex.VisualStudioExtension
{
    public partial class CodeIndexClient
    {
        public CodeIndexClient(System.Net.Http.HttpClient httpClient, string baseUrl) : this(httpClient)
        {
            BaseUrl = baseUrl;
        }

        public string BaseUrl { get; set; }
    }
}
