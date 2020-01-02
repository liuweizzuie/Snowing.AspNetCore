using System;
using System.Collections.Generic;
using System.Text;

namespace Snowing.AspNetCore.Http
{
    public enum HttpContentType
    {
        /// <summary>
        /// Default
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// application/json
        /// </summary>
        ApplicationJson = 0,

        /// <summary>
        /// application/x-www-form-urlencoded
        /// </summary>
        FormUrlEncoded = 1,
    }
}
