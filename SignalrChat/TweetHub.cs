using System;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;


namespace SignalrChat
{
    /// <summary>
    /// ツイートハブ
    /// </summary>
    [Authorize]
    public class TweetHub : Hub
    {
        private ITweetLogService _log;
        private IUserIdProvider _userIdProvider;
        private IUserStore _userStore;
        private IMessageCacheService _msgCache;



        public TweetHub(ITweetLogService log, IUserIdProvider userIdProvider, IUserStore userStore, IMessageCacheService msgCache) 
        {
            Debug.WriteLine("[コンストラクタ] TweetHub");

            this._log = log;
            this._userIdProvider = userIdProvider;
            this._userStore = userStore;
            this._msgCache = msgCache;
        }

        public async Task Tweet(Tweet data)
        {
            _log.Write(data);
            var grpName = GetGroupName();
            await _msgCache.Add(grpName, data);
            Clients.Group(grpName).Tweet(data.UserName, data.Date, data.Message);
        }

        private string GetGroupName()
        {
            return this.Context.QueryString["groupname"];
        }

        private string GetUserName()
        {
            return _userIdProvider.GetUserId(this.Context.Request);
        }

        public override async Task OnConnected()
        {
            try
            {
                if (Context.User != null)
                {
                    Debug.WriteLine(string.Format("[認証状態] IsAuthenticated:{0}、Name:{1}", Context.User.Identity.IsAuthenticated, Context.User.Identity.Name));
                }
                else
                {
                    
                    Debug.WriteLine("[認証状態] Context.User is null");
                }

                var id = Context.ConnectionId;
                var userName = GetUserName();
                var grpName = GetGroupName();
                Debug.WriteLine(string.Format("[接続] id:{0}, username:{1}, group:{2}", id, userName, grpName));

                await Groups.Add(id, grpName);

                var userList = await _userStore.GetAll(grpName);
                foreach (var l in userList)
                {
                    Clients.Caller.addUser(l);
                }

                Console.WriteLine("groupname:{0}", grpName);
                Clients.OthersInGroup(grpName).addUser(userName);
                await _userStore.Add(grpName, userName);

                var msgList = await _msgCache.GetAll(grpName);
                foreach (var m in msgList)
                {
                    Clients.Caller.Tweet(m.UserName, m.Date, m.Message);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(string.Format("[OnConnected][Error]{0}", e.Message));
            }
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            var id = Context.ConnectionId;
            var userName = GetUserName();
            var grpName = GetGroupName();
            Debug.WriteLine(string.Format("[切断] id:{0}, username:{1}, groupname:{2}", id, userName, grpName));
            await _userStore.Remove(grpName, userName);
            Clients.OthersInGroup(grpName).exitUser(userName);
            await Groups.Remove(id, grpName);
        }

        public override Task OnReconnected()
        {
            var userName = GetUserName();
            Debug.WriteLine(string.Format("[再接続] id:{0}, username:{1}", Context.ConnectionId, userName));
            //Clients.Others.addUser(userName);

            return base.OnReconnected();
        }
    }

    /// <summary>
    /// ログサービス
    /// </summary>
    public interface ITweetLogService
    {
        void Write(Tweet data);
    }

    /// <summary>
    /// ツイート情報
    /// </summary>
    public class Tweet 
    {
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public int Id { get; set; }
        public string UserName { get; set; }

        public Tweet()
        {
            //memo: 定義はしたものの使い道が未定のため、デフォルト値として-1をセットしておく。
            Id = -1;
        }

        public override string ToString()
        {
            return string.Format("dt:\"{0}\",un:\"{1}\",m:\"{2}\",i:\"{3}\""
                , Date.Ticks
                , UserName
                , Message.Replace("\"", "\"\"")
                , Id);
        }

        public static Tweet Parse(string src)
        {
            var splited = src.Split(',');
            var tObj = new Tweet();
            tObj.Date = new DateTime(Convert.ToInt64(splited[0].Split(':')[1].Replace("\"", "")));
            tObj.UserName = splited[1].Split(':')[1].Replace("\"", "");
            tObj.Message = splited[2].Split(':')[1].Replace("\"", "");
            return tObj;
        }
    }

    /// <summary>
    /// デフォルトログサービス
    /// </summary>
    public class DefaultTweetLogService : ITweetLogService
    {
        public DefaultTweetLogService() { }

        public void Write(Tweet data)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("[ログ] Date:{0}, Id:{1}, Message:{2}", data.Date.ToString("yyyy/MM/dd HH:mm:ss"), data.Id, data.Message));
        }
    }
}