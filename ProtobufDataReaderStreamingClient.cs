using ProtoBuf.Data;

namespace ConsoleApp1
{
    public class BufferConsumer
    {
        // HttpClient lifecycle management best practices:
        // https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines#recommended-use

        private static HttpClient sharedClient = new HttpClient(new HttpClientHandler()
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback =
            (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            }
        })
        {
            BaseAddress = new Uri("https://localhost:7293")
        };

        public static async Task GetAsync()
        {
            try
            {
                var message = new HttpRequestMessage(HttpMethod.Get, "api/HubSync/GetProtoStreaming");

                using HttpResponseMessage response = await sharedClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode()
                    .WriteRequestToConsole();

                if (response.Content.Headers?.ContentType?.MediaType == "application/x-protobuf")
                {
                    using Stream contentStream = await response.Content.ReadAsStreamAsync();

                    using (var dataReader = DataSerializer.Deserialize(contentStream))
                    {
                        while (dataReader.Read())
                        {
                            object[] values = new object[dataReader.FieldCount];
                            dataReader.GetValues(values);
                            Console.WriteLine(string.Join(", ", values));
                        }
                    }

                    Console.WriteLine("Hemos terminado.");
                }
                else
                {
                    Console.WriteLine("Contenido no es x-protobuf");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }

    static class HttpResponseMessageExtensions
    {
        internal static void WriteRequestToConsole(this HttpResponseMessage response)
        {
            if (response is null)
            {
                return;
            }

            var request = response.RequestMessage;
            Console.Write($"{request?.Method} ");
            Console.Write($"{request?.RequestUri} ");
            Console.WriteLine($"HTTP/{request?.Version}");
        }
    }
}
