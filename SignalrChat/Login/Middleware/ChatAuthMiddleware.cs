using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SignalrChat.Login;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin;
using Microsoft.AspNet.Identity;
using System.Security.Claims;

namespace SignalrChat.Login.Middleware
{
    public static class ChatLoginExtentions
    {
        public static IAppBuilder UseLogin(this IAppBuilder app, LoginOptions loginOption, LogoutOptions logoutOption)
        {
            return app.Use<ChatAuthMiddleware>(loginOption, logoutOption);
        }
    }

    public class LoginOptions
    {
        /// <summary>
        /// ログインユーザー名ゲッター
        /// </summary>
        public Func<IOwinContext, Task<string>> UserNameGetter { get; set; }
        /// <summary>
        /// グループ名ゲッター
        /// </summary>
        public Func<IOwinContext, Task<string>> GroupNameGetter { get; set; }
        /// <summary>
        /// ログインページUrl
        /// </summary>
        /// <remarks>
        /// <para>リクエスト先Urlがこのプロパティ値に一致する場合にログイン認証・リダイレクトを行う。</para>
        /// </remarks>
        public string LoginUrl;
        /// <summary>
        /// ログイン時のHTTPメソッド
        /// </summary>
        public string LoginMethod;
        /// <summary>
        /// ログイン後のリダイレクト先Url
        /// </summary>
        /// <remarks>
        /// <para>URL中の{username}はユーザー名、{groupname}はグループ名に変換される</para>
        /// </remarks>
        public string RedirectUrl;

        public LoginOptions()
        {
            Func<IOwinContext, Task<string>> defaultGetter = (ctx) =>
            {
                return Task.Run<string>(() => { return ""; });
            };
            UserNameGetter = defaultGetter;
            GroupNameGetter = defaultGetter;

            this.LoginUrl = "/login";
            this.LoginMethod = "POST";
            this.RedirectUrl = "/secure";
        }
    }

    public class LogoutOptions
    {
        /// <summary>
        /// ログアウトページUrl
        /// </summary>
        public string LogoutUrl;
        /// <summary>
        /// ログアウト後のリダイレクト先Url
        /// </summary>
        public string RedirectUrl;

        public LogoutOptions()
        {
            this.LogoutUrl = "/logout";
            this.RedirectUrl = "/";
        }
    }

    public class ChatAuthMiddleware : OwinMiddleware
    {
        private LoginOptions _loginOption;
        private LogoutOptions _logoutOption;

        public ChatAuthMiddleware(OwinMiddleware next)
            : this(next, new LoginOptions(), new LogoutOptions()) { }

        public ChatAuthMiddleware(OwinMiddleware next, LoginOptions loginOption, LogoutOptions logoutOption)
            : base(next)
        {
            this._loginOption = loginOption;
            this._logoutOption = logoutOption;
        }

        public async override Task Invoke(IOwinContext context)
        {
            if (context.Request.Path.Value == _logoutOption.LogoutUrl)
            {
                SignOut(context);
                return;
            }

            if (!IsAuthenticated(context)
                && context.Request.Path.Value == _loginOption.LoginUrl
                && context.Request.Method.ToUpper() == _loginOption.LoginMethod)
            {
                await SignIn(context);
                return;
            }

            await Next.Invoke(context);
        }

        private bool IsAuthenticated(IOwinContext context)
        {
            var authUserInfo = context.Authentication.User;
            return authUserInfo != null && !string.IsNullOrEmpty(authUserInfo.Identity.Name) && authUserInfo.Identity.IsAuthenticated;
        }

        private void SignOut(IOwinContext context)
        {
            context.Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            context.Response.Redirect(_logoutOption.RedirectUrl);
        }

        private async Task SignIn(IOwinContext context)
        {
            var userName = await _loginOption.UserNameGetter(context);
            var grpName = await _loginOption.GroupNameGetter(context);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(grpName))
            {
                await WriteResponse(context.Response, 401, "invalid parameter.");
                return;
            }

            var uMng = new ChatUserManager();
            var userIdentity = await uMng.CreateAsync(userName);
            if (userIdentity == null)
            {
                await WriteResponse(context.Response, 401, "invalid user.");
                return;
            }

            context.Authentication.SignIn(userIdentity);
            var redirectPath = _loginOption.RedirectUrl.Replace("{username}", userName).Replace("{groupname}", grpName);
            context.Response.Redirect(redirectPath);
        }

        private async Task WriteResponse(IOwinResponse response, int status, string content)
        {
            response.StatusCode = status;
            await response.WriteAsync(content);
        }
    }
}