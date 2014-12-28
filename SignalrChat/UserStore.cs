using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace SignalrChat
{
    /// <summary>
    /// ユーザーストアインターフェイス
    /// </summary>
    /// <remarks>
    /// 非同期なインターフェイスとする
    /// </remarks>
    public interface IUserStore
    {
        /// <summary>
        /// 追加
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task Add(string groupName, string userName);
        /// <summary>
        /// 削除
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task Remove(string groupName, string userName);
        /// <summary>
        /// 全アイテムの取得
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetAll(string groupName);
    }

    /// <summary>
    /// Redisを使ったユーザー名ストア
    /// </summary>
    public class DefaultUserNameStore : IUserStore
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
            /// <summary>
            /// グループ所属ユーザーの集合を表すセット名テンプレート
            /// </summary>
            public string GroupMemberSetKeyTemplate { get; set; }


            public StoreSetting()
            {
                this.Host = "localhost";
                this.SyncTimeout = 1000;
                this.Retry = 3;
                this.Port = 6379;
                this.DbNo = 0;
                this.ActiveUserSetKey = "ActiveUserSet";
                this.GroupMemberSetKeyTemplate = "{0}GroupMemberSet";
            }
        }

        private StoreSetting _setting;
        private ConnectionMultiplexer _muxer;


        public DefaultUserNameStore(StoreSetting s)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("[DefaultUserNameStore][コンストラクタ]"));
            this._setting = s;
            this._muxer = CreateConnection();
        }

        public DefaultUserNameStore()
            : this(new StoreSetting()) {}

        private ConnectionMultiplexer CreateConnection()
        {
            ConfigurationOptions option = ConfigurationOptions.Parse(_setting.Host);
            var con = ConnectionMultiplexer.Connect(option, Console.Out);
            con.InternalError += con_InternalError;
            return con;
        }

        void con_InternalError(object sender, InternalErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("[ConnectionMultiplexer][InternalError]{0}", e.Exception.Message));
        }

        public Task Add(string groupName, string userName)
        {
            Func<bool> AddInternal = () =>
            {
                var db = _muxer.GetDatabase(_setting.DbNo);
                var tran = db.CreateTransaction();
                tran.SetAddAsync(_setting.ActiveUserSetKey, userName);
                tran.SetAddAsync(string.Format(_setting.GroupMemberSetKeyTemplate, groupName), userName);
                return tran.Execute();
            };

            return Task.Run(() =>
            {
                for (var i = 1; i <= _setting.Retry; i++)
                {
                    if (AddInternal())
                        return;
                    System.Threading.Thread.Sleep(100);
                }

                throw new Exception("Addメソッドのリトライ上限を超えました。");
            });
        }

        public Task Remove(string groupName, string userName)
        {
            Func<bool> RemoveInternal = () => 
            {
                var db = _muxer.GetDatabase(_setting.DbNo);
                var tran = db.CreateTransaction();
                tran.SetRemoveAsync(_setting.ActiveUserSetKey, userName);
                tran.SetRemoveAsync(string.Format(_setting.GroupMemberSetKeyTemplate, groupName), userName);
                return tran.Execute();
            };

            return Task.Run(() =>
            {
                for (var i = 1; i <= _setting.Retry; i++)
                {
                    if (RemoveInternal())
                        return;
                    System.Threading.Thread.Sleep(100);
                }

                throw new Exception("Removeメソッドのリトライ上限を超えました。");
            });
        }

        public Task<IEnumerable<string>> GetAll(string groupName)
        {
            return Task.Run(async () =>
            {
                var db = _muxer.GetDatabase(_setting.DbNo);
                var members = await db.SetCombineAsync(
                                        SetOperation.Intersect, 
                                        _setting.ActiveUserSetKey, 
                                        string.Format(_setting.GroupMemberSetKeyTemplate, groupName));
                return members.Select<RedisValue, string>((v) => { return v.ToString(); });
            });
        }
    }
}