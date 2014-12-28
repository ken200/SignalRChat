using System;
using Owin;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.FileSystems;
using SignalrChat.Login.Middleware;
using System.Threading.Tasks;

namespace SignalrChat
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);

            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie
            });

            app.UseLogin(
                new LoginOptions()
                {
                    LoginUrl = "/chatlogin",
                    LoginMethod = "POST",
                    UserNameGetter = (ctx) =>
                    {
                        return Task.Run<string>(
                            async () =>
                            {
                                var form = await ctx.Request.ReadFormAsync();
                                return form["username"];
                            });
                    },
                    GroupNameGetter = (ctx) => 
                    {
                        return Task.Run<string>(
                            async () =>
                            {
                                var form = await ctx.Request.ReadFormAsync();
                                return form["groupname"];
                            });
                    },
                    RedirectUrl = "/Content/chat.html?username={username}&groupname={groupname}"
                },
                new LogoutOptions()
                {
                    LogoutUrl = "/logout",
                    RedirectUrl = "/Content/index.html"
                });

            app.Map("/Content", (ap) => { ap.UseStaticFiles(); });
            app.Map("/Scripts", (ap) => { ap.UseStaticFiles(); });
            app.Map("/Style", (ap) => { ap.UseStaticFiles(); }); 

            app.Map("/signalr", (map) =>
            {
                var resolver = new DefaultDependencyResolver();
                resolver.Register(typeof(TweetHub), () =>
                {
                    return new TweetHub(
                        new DefaultTweetLogService(), 
                        new Microsoft.AspNet.SignalR.Infrastructure.PrincipalUserIdProvider(),
                        new DefaultUserNameStore(),
                        new DefaultMessageCacheService());
                });
                map.MapSignalR(new HubConfiguration() { Resolver = resolver, EnableJavaScriptProxies = true, EnableDetailedErrors = true });
            });
        }
    }
}