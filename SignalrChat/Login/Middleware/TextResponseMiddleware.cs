using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin;

namespace SignalrChat.Login.Middleware
{
    public static class TextResponseMiddlewareExtentions
    {
        public static IAppBuilder UseText(this IAppBuilder app, TextResponseMiddlewareOptions option)
        {
            return app.Use<TextResponseMiddleware>(option);
        }
    }

    public class TextResponseMiddlewareOptions
    {
        public string Content { get; set; }
        public TextResponseMiddlewareOptions() 
        {
            this.Content = "";
        }
    }

    public class TextResponseMiddleware : OwinMiddleware
    {        
        private TextResponseMiddlewareOptions _options;

        public TextResponseMiddleware(OwinMiddleware next, TextResponseMiddlewareOptions options) : base(next)
        {
            this._options = options;
        }

        public async override Task Invoke(IOwinContext context)
        {
            await context.Response.WriteAsync(_options.Content);
        }
    }
}