using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Snowing.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Snowing.AspNetCore.Http
{
    /// <summary>
    /// http 服务的基类；一个 Service 承载一类业务，相当于DDD的聚合根Repository
    /// </summary>
    public class HttpServiceBase
    {
        protected  ServiceOption Option { get; set; }
        protected ILogger<HttpServiceBase> logger { get; set; }

        public HttpServiceBase(IServiceOptionProvider provider, ILogger<HttpServiceBase> logger)
        {
            this.Option = provider.Option;
            this.logger = logger;
        }

        /// <summary>
        /// 直接序列化结果
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="type"></param>
        public TResult HttpPost<TResult>(string actionName, 
            IDictionary<string, object> headers = null,
            object body = null,
            HttpContentType type = HttpContentType.ApplicationJson,
             params Tuple<string, object>[] parames)
        {
            TResult result = default(TResult);
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.Option.BaseAddress);

            if(headers == null)
            {
                headers = new Dictionary<string, object>();
            }

            if (!headers.ContainsKey("Content-Type"))
            {
                switch (type)
                {
                    case HttpContentType.Unknown:
                        break;
                    case HttpContentType.ApplicationJson:
                        headers.Add("Content-Type", "application/json");
                        break;
                    case HttpContentType.FormUrlEncoded:
                        headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        break;
                    default:
                        break;
                }
            }

            string relativeUrl = BuildRelativeUrl(actionName, parames);
            HttpContent content = BuildContent(headers, body);
            HttpResponseMessage msg = client.PostAsync(relativeUrl, content).Result;
            if (msg.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string str = msg.Content.ReadAsStringAsync().Result;
                result = JsonConvert.DeserializeObject<TResult>(str);
                if(result == null)
                {
                    this.logger.LogError("convert error:  " + str);
                }
            }
            else
            {
                if (msg.StatusCode == System.Net.HttpStatusCode.BadRequest && logger != null)
                {
                    this.logger.LogInformation(actionName);
                    if (body != null)
                    {
                        this.logger.LogInformation(JsonConvert.SerializeObject(body));
                    }
                    string _log = msg.Content.ReadAsStringAsync().Result;
                    this.logger.LogError(_log);
                }
                throw new HttpRequestException(string.Format("status = {0}:{1}", (int)msg.StatusCode, msg.StatusCode));
            }

            return result;
        }

        protected HttpContent BuildContent(IDictionary<string, object> headers = null, object body = null)
        {
            string content = string.Empty;
            HttpContent hc = null;
            if (body != null && !string.IsNullOrEmpty(body.ToString()) && !(body is string))
            {
                content = JsonConvert.SerializeObject(body);
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                hc = new StreamContent(stream);
            }
            else if (body is string) //发  string 类型的 form
            {
                hc = new StringContent(body.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
            }
            else // body is null
            {
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(content)); //content is empty
                hc = new StreamContent(stream);
            }

            if (headers != null)
            {
                foreach (var item in headers)
                {
                    hc.Headers.Add(item.Key, item.Value.ToString());
                }
            }
            return hc;
        }


        protected string BuildRelativeUrl(string actionName, params Tuple<string, object>[] parames)
        {
            StringBuilder relativeUrlBuilder = new StringBuilder();
            relativeUrlBuilder.AppendFormat("{0}/{1}", this.Option.Controller, actionName);
            if (parames != null && parames.Length > 0)
            {
                relativeUrlBuilder.Append("?");
                relativeUrlBuilder.Append(parames);
            }
            return relativeUrlBuilder.ToString();
        }

        protected string ParaPartUrl(params Tuple<string, object>[] parames)
        {
            IList<string> ps = new List<string>();
            parames.ActionForeach(p => ps.Add(string.Format("{0}={1}", p.Item1, p.Item2)));
            return ps.Concat('&');
        }

        public TResult HttpGet<TResult>(string serviceName,
            IDictionary<string, object> headers = null, params Tuple<string, object>[] paras)
        {
            string relativeUrl = BuildRelativeUrl(serviceName, paras);

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.Option.BaseAddress);
            Uri request = new Uri(relativeUrl, UriKind.Relative);
            string http_result = client.GetAsync(request).Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<TResult>(http_result);
        }

        protected Stream HttpDownload(string serviceName, params Tuple<string, object>[] parames)
        {
            string relativeUrl = BuildRelativeUrl(serviceName, parames);
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.Option.BaseAddress);
            Uri request = new Uri(relativeUrl, UriKind.Relative);

            HttpResponseMessage response = client.GetAsync(request).Result;

            response.Headers.ActionForeach(h =>
            {
                object v = h.Value;
                if (v is IEnumerable<string>)
                {
                    v = (v as IEnumerable<string>).ToList().Concat(' ');
                }
                logger.LogInformation("{0} : {1}", h.Key, v);
            });

            return response.Content.ReadAsStreamAsync().Result;
        }

        public Stream DownloadFile(string url)
        {
            HttpClient client = new HttpClient();
            Uri request = new Uri(url, UriKind.Absolute);

            HttpResponseMessage response = client.GetAsync(request).Result;
            return response.Content.ReadAsStreamAsync().Result;
        }

        protected async Task<string> UploadFileAsync(string url, string contentType, Stream stream, string fileExt)
        {
            using (var client = new HttpClient())
            {
                string fileName = DateTime.Now.Ticks.ToString("x");
                string boundary = "------------" + fileName;
                using (var content = new MultipartFormDataContent(boundary))
                {
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=" + boundary);

                    StreamContent sc = new StreamContent(stream);
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        sc.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                    }
                    sc.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(
                        string.Format("form-data; name=\"file\"; filename=\"somename.{0}\"", fileExt));

                    content.Add(sc, "file", "somename." + fileExt);

                    using (var httpResponseMessage = await client.PostAsync(url, content))
                    {
                        var responseContent = "";
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
                        }
                        return responseContent;
                    }
                }
            }
        }

    }
}
