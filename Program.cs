using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RedirectCompare
{
    class Program
    {
        private const string InputPath = @"redirects.csv";
        private const string OutputPath = @"results.csv";

        static void Main(string[] args)
        {
            // Read in file
            var redirects = new List<SourceRedirect>();
            using (var reader = new StreamReader(InputPath))
            {
                // Skip header line
                reader.ReadLine();

                string line;
                string[] parts;
                while ((line = reader.ReadLine()) != null)
                {
                    parts = line.Split(',');
                    redirects.Add(new SourceRedirect { Origin = parts[0].Trim(), Destination = parts[1].Trim() });
                }
            }

            // Check redirects
            Parallel.ForEach(redirects, redirect =>
            {
                redirect.CurrentDestinationStatus = FindRedirectDestination("http://" + redirect.Origin);
                redirect.OriginalDestinationStatus = FindRedirectDestination(redirect.Destination);
                Console.WriteLine($"{redirect.Origin},{redirect.Destination}");
            });

            // Output to file
            using (var writer = new StreamWriter(OutputPath))
            {
                writer.WriteLine("Source,File Destination,Original Destination,Current Destination,Match");
                foreach (var redirect in redirects)
                {
                    var current = redirect.CurrentDestinationStatus;
                    var original = redirect.OriginalDestinationStatus;
                    writer.WriteLine($"{redirect.Origin},{redirect.Destination},{original.Error ?? original.Uri.ToString()},{current.Error ?? current.Uri.ToString()},{(((current.Error ?? current.Uri.ToString()) == (original.Error ?? original.Uri.ToString())) ? "TRUE" : "FALSE")}");
                }
            }
        }

        static ResponseStatus FindRedirectDestination(string source)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(source);
                request.Method = "HEAD";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return new ResponseStatus { StatusCode = response.StatusCode, Uri = response.ResponseUri };
                }
            }
            catch (Exception ex)
            {
                return new ResponseStatus { Error = ex.Message };
            }
        }
    }

    class SourceRedirect
    {
        public string Origin { get; set; }

        public string Destination { get; set; }

        public ResponseStatus OriginalDestinationStatus { get; set; }

        public ResponseStatus CurrentDestinationStatus { get; set; }
    }

    class ResponseStatus
    {
        public HttpStatusCode? StatusCode { get; set; }

        public Uri Uri { get; set; }

        public string Error { get; set; }
    }
}
