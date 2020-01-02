using System;
using System.Collections.Generic;
using System.Text;

namespace Snowing.AspNetCore.Http
{
    public class ServiceOption
    {
        public string BaseAddress { get; set; }

        public string Controller { get; set; }

        public HttpContentType DefaultContentType { get; set; }


        public ServiceOption()
        {
            this.DefaultContentType = HttpContentType.ApplicationJson;
        }

    }
}
