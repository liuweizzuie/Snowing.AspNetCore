using System;
using System.Collections.Generic;
using System.Text;

namespace Snowing.AspNetCore.Http
{
    public interface IServiceOptionProvider
    {
        ServiceOption Option { get; }
    }
}
