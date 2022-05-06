﻿using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Nop.Plugin.Api.JSON.ActionResults
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.WebUtilities;
    using Nop.Plugin.Api.Enumeration;
    using Nop.Web.Infrastructure;

    public class ErrorActionResult : IActionResult 
    {
        private readonly string _jsonString;
        private readonly HttpStatusCode _statusCode;

        public ErrorActionResult(string jsonString, HttpStatusCode statusCode)
        {
            _jsonString = jsonString;
            _statusCode = statusCode;
        }
        
        public Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var HttpContext = context.HttpContext;
            ApiResponseHeader apiresponse = new ApiResponseHeader();
            apiresponse.SetResponseHeader(HttpContext);

            using (TextWriter writer = new HttpResponseStreamWriter(HttpContext.Response.Body, Encoding.UTF8))
            {
                writer.Write(_jsonString);
            }

            return Task.CompletedTask;
        }
    }
}