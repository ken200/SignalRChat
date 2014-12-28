using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace SignalrChat
{
    /// <summary>
    /// クエリ文字列からユーザー名を取得する
    /// </summary>
    public class QueryParameterUserIdProvider : IUserIdProvider
    {
        public string GetUserId(IRequest request)
        {
            return request.QueryString.Get("username");
        }
    }
}