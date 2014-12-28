/// <reference path="../typings/jquery/jquery.d.ts" />
/// <reference path="../typings/signalr/signalr.d.ts" />

module TweetHubClient {

    /**
     * ツイートデータ
     */
    export interface TweetData {
        date: Date;
        message: string;
        userName: string;
    }


    /**
     * クライアント
     */
    export class Client {

        private hub: HubProxy;

        constructor(hub: HubProxy) {
            this.hub = hub;
        }

        /**
         * サーバーへTweetイベントデータ送信
         */
        public tweet(data: TweetData) {
            return this.hub.invoke("Tweet", data);
        }

        /**
         * Tweetイベント受信時処理
         */
        public onTweetEvent(handler: (data: TweetData) => void) {
            this.hub.on("tweet", (username : string, date: string, message: string) => {
                handler({
                    userName: username,
                    date: new Date(date),
                    message: message
                });
            });
        }

        /**
         * 入室イベント受信時処理
         */
        public onAddUserEvent(handler: (name : string) => void) {
            this.hub.on("addUser", (name: string) => {
                handler(name);
            });
        }

        public onExitUserEvent(handler : (name : string) => void) {
            this.hub.on("exitUser", (name: string) => {
                handler(name);
            });
        }
    }

}