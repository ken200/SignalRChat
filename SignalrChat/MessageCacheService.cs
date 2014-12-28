using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using StackExchange.Redis;


namespace SignalrChat
{
    /// <summary>
    /// メッセージキャッシュサービスのインターフェイス
    /// </summary>
    public interface IMessageCacheService
    {
        /// <summary>
        /// キャッシュへ追加
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="msg"></param>
        Task Add(string groupName, Tweet msg);
        /// <summary>
        /// キャッシュから全メッセージを取得
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        Task<IEnumerable<Tweet>> GetAll(string groupName);
    }

    /// <summary>
    /// デフォルトメッセージキャッシュサービス
    /// </summary>
    public class DefaultMessageCacheService : IMessageCacheService
    {
        public class StoreSetting
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public int DbNo { get; set; }
            public string GroupName { get; set; }
            /// <summary>
            /// メッセージキャッシュのリストのキー名称テンプレート
            /// </summary>
            public string MessageCacheKeyTemplate { get; set; }
            /// <summary>
            /// キャッシュの最大個数
            /// </summary>
            public int MaxCacheCount { get; set; }
            /// <summary>
            /// 操作のリトライ回数
            /// </summary>
            /// <remarks>
            /// <para>トランザクション失敗時のリトライ回数</para>
            /// </remarks>
            public int Retry { get; set; }

            public StoreSetting()
            {
                this.Host = "localhost";
                this.Port = 6379;
                this.MessageCacheKeyTemplate = "{0}GroupMessageCacheList";
                this.DbNo = 0;
                this.MaxCacheCount = 30;
                this.Retry = 3;
            }
        }

        private StoreSetting _setting;
        private const string TRAN_KEY = "DefaultMessageCacheService:TranKey";
        private ConnectionMultiplexer _muxer;

        public DefaultMessageCacheService(StoreSetting setting)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("[DefaultMessageCacheService][コンストラクタ]"));
            this._setting = setting;
            this._muxer = CreateConnection();
        }

        public DefaultMessageCacheService() 
            : this(new StoreSetting()) { }

        private ConnectionMultiplexer CreateConnection()
        {
            ConfigurationOptions option = ConfigurationOptions.Parse(_setting.Host);
            return ConnectionMultiplexer.Connect(option);
        }

        public Task Add(string groupName, Tweet msg)
        {
            Func<bool> AddInternal = () => {
                bool ret = false;
                var db = _muxer.GetDatabase(_setting.DbNo);

                try
                {
                    //トランザクション
                    var tran = db.CreateTransaction();
                    tran.AddCondition(Condition.KeyNotExists(TRAN_KEY));
                    tran.StringSetAsync(TRAN_KEY, DateTime.Now.Ticks);

                    //あぶれ分の削除
                    var msgCacheKey = string.Format(_setting.MessageCacheKeyTemplate, groupName);
                    var len = db.ListLength(msgCacheKey);
                    for (var i = db.ListLength(msgCacheKey); i >= _setting.MaxCacheCount; i--)
                    {
                        tran.ListLeftPopAsync(msgCacheKey);
                    }
                    //今回分の追加
                    tran.ListRightPushAsync(msgCacheKey, msg.ToString());

                    ret = tran.Execute();
                }
                finally
                {
                    db.KeyDelete(TRAN_KEY);
                }


                return ret;
            };

            return Task.Run(() =>
            {
                for(var i=0;i<_setting.Retry;i++)
                {
                    if (AddInternal())
                        return;
                    System.Threading.Thread.Sleep(100);
                }

                throw new Exception("Addメソッドのリトライ上限を超えました。");
            });
        }

        public Task<IEnumerable<Tweet>> GetAll(string groupName)
        {
            return Task.Run(async () =>
            {
                var db = _muxer.GetDatabase(_setting.DbNo);
                var msgs = await db.ListRangeAsync(string.Format(_setting.MessageCacheKeyTemplate, groupName));

                return msgs.Select<RedisValue, Tweet>((v) =>
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("[DefaultMessageCacheService][Data]:{0}", v.ToString()));

                    Tweet tweet = new Tweet();
                    try
                    {
                        tweet = Tweet.Parse(v.ToString());
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("[DefaultMessageCacheService][Error]{0}", e.Message));
                    }

                    return tweet;
                });
            });
        }
    }
}