using System.Diagnostics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace eShopModernizedMVC.Filters
{
    public class ActionTracerFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var actionName = (filterContext.ActionDescriptor as ControllerActionDescriptor)?.ActionName ?? filterContext.ActionDescriptor.DisplayName;
            Trace.TraceInformation($"Received request for action {actionName} in controller {filterContext.Controller.GetType().Name}.");
            base.OnActionExecuting(filterContext);
        }
    }
}