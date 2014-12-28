using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using StackExchange.Redis;

namespace SignalrChat.Login
{
    public class ChatUser : IUser
    {
        public string Id { get; set; }

        public string UserName { get; set; }

        public ChatUser()
        {
            Id = Guid.NewGuid().ToString();
        }
    }

    public class ChatUserStore : IUserStore<ChatUser>
    {
        public class StoreSetting
        {
            public string Host { get; set; }
            public int SyncTimeout { get; set; }
            public int Retry { get; set; }
            public int Port { get; set; }
            public int DbNo { get; set; }
            /// <summary>
            /// ログイン済みユーザー名の集合を表すセット名
            /// </summary>
            public string ActiveUserSetKey { get; set; }

            public StoreSetting()
            {
                this.Host = "localhost";
                this.SyncTimeout = 1000;
                this.Retry = 3;
                this.Port = 6379;
                this.DbNo = 0;
                this.ActiveUserSetKey = "ActiveUserSet";
            }
        }

        private StoreSetting _setting;

        public ChatUserStore(StoreSetting setting)
        {
            this._setting = setting;
        }

        public ChatUserStore()
            : this(new StoreSetting()) { }

        public Task CreateAsync(ChatUser user)
        {
            return Task.Delay(0);
        }

        public Task DeleteAsync(ChatUser user)
        {
            return Task.Delay(0);
        }

        public Task<ChatUser> FindByIdAsync(string userId)
        {
            throw new NotSupportedException("UserId is not supported");
        }

        private bool ExistUser(string userName)
        {
            if (userName.ToLower() == "admin" || userName.ToLower() == "root")
                return false;
            return true;
        }

        public Task<ChatUser> FindByNameAsync(string userName)
        {
            //todo: ActiveUserセットに対する存在有無確認の実装

            return Task.Run<ChatUser>(() =>
            {
                return !ExistUser(userName) ? new ChatUser() { UserName = userName } : null;
            });
        }

        public Task UpdateAsync(ChatUser user)
        {
            return Task.Delay(0);
        }

        public void Dispose()
        {

        }
    }

    public class ChatUserManager
    {
        private UserManager<ChatUser> _uMng;

        public ChatUserManager(UserManager<ChatUser> uMng)
        {
            this._uMng = uMng;
        }

        public ChatUserManager() 
            : this(new UserManager<ChatUser>(new ChatUserStore())) { }


        public async Task<ClaimsIdentity> CreateAsync(string username)
        {
            var  user = new ChatUser(){ UserName = username};
            var activeUser = await _uMng.FindByNameAsync(user.UserName);

            if (activeUser == null)
            {
                return await _uMng.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            }else
            {
                return null;
            }
        }
    }
}