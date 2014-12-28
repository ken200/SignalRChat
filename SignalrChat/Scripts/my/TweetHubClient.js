/// <reference path="../typings/jquery/jquery.d.ts" />
/// <reference path="../typings/signalr/signalr.d.ts" />
var TweetHubClient;
(function (TweetHubClient) {
    

    /**
    * クライアント
    */
    var Client = (function () {
        function Client(hub) {
            this.hub = hub;
        }
        /**
        * サーバーへTweetイベントデータ送信
        */
        Client.prototype.tweet = function (data) {
            return this.hub.invoke("Tweet", data);
        };

        /**
        * Tweetイベント受信時処理
        */
        Client.prototype.onTweetEvent = function (handler) {
            this.hub.on("tweet", function (username, date, message) {
                handler({
                    userName: username,
                    date: new Date(date),
                    message: message
                });
            });
        };

        /**
        * 入室イベント受信時処理
        */
        Client.prototype.onAddUserEvent = function (handler) {
            this.hub.on("addUser", function (name) {
                handler(name);
            });
        };

        Client.prototype.onExitUserEvent = function (handler) {
            this.hub.on("exitUser", function (name) {
                handler(name);
            });
        };
        return Client;
    })();
    TweetHubClient.Client = Client;
})(TweetHubClient || (TweetHubClient = {}));
//# sourceMappingURL=TweetHubClient.js.map
