using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Snowing.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Snowing.AspNetCore.Filteres
{
    public class NonEmptyBodyAttribute : ActionFilterAttribute, IActionFilter
    {
        public int Code { get; set; }

        public string Message { get; set; }

        public NonEmptyBodyAttribute(int code = 500, string message = "empty body")
        {
            this.Code = code;
            this.Message = message;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IDictionary<string, object> para = context.ActionArguments;
            if (para.Count == 0)
            {
                JsonResult<object> result = new JsonResult<object>() { Data = new object() };
                result.Status = this.Code;
                result.Message = this.Message;

                context.Result = new ContentResult()
                {
                    Content = JsonConvert.SerializeObject(result)
                };
            }
        }
    }
}
