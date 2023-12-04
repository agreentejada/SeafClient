using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.IO;

namespace SeafClient.Utils
{
    /// <summary>
    ///     Helper class for the <see cref="HttpClient"/> from System.Net.Http
    /// </summary>
    internal static class HttpUtils
    {
        /// <summary>
        ///     Creates an HTTP GET request to the given URI
        /// </summary>
        /// <param name="method">The HTTP method</param>
        /// <param name="uri">The uri to get</param>
        /// <param name="headerInfo">Additional headers to be added to the HTTP GET request</param>
        /// <returns>The http response</returns>
        public static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, IEnumerable<KeyValuePair<string, string>> headerInfo, IEnumerable<KeyValuePair<string, string>> bodyParams)
        {
            var message = new HttpRequestMessage(method, uri);
            message.Headers.Referrer = uri;

            foreach (var hi in headerInfo)
                message.Headers.Add(hi.Key, hi.Value);

            if (bodyParams.Any())
            {
                message.Content = new FormUrlEncodedContent(bodyParams);
            }

            return message;
        }

        /// <summary>
        /// Copied over from https://www.jordanbrown.dev/2021/02/06/2021/http-to-raw-string-csharp/ and modified to fit HttpRequestMessage.
        /// Writes out the HTTP Message being sent over the wire as a string.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<string> ToRawString(this HttpRequestMessage request)
        {
            var sb = new StringBuilder();

            var line1 = $"{request.Method} {request.RequestUri}";
            sb.AppendLine(line1);

            foreach (var (key, values) in request.Headers)
            {
                foreach (var value in values)
                {
                    var header = $"{key}: {value}";
                    sb.AppendLine(header);
                }
            }


            if (request.Content != null)
            {
                foreach (var (key, values) in request.Content.Headers)
                {
                    foreach (var value in values)
                    {
                        var header = $"{key}: {value}";
                        sb.AppendLine(header);
                    }
                }

                sb.AppendLine();
                sb.Append(await request.Content.ReadAsStringAsync());
            }

            return sb.ToString();
        }
    }
}