/// <reference path="../typings/jquery/jquery.d.ts" />
/// <reference path="../typings/signalr/signalr.d.ts" />
/// <reference path="../typings/underscore/underscore.d.ts" />

module Chat {

    /**
     * タイムラインビュー
     */
    class TimeLineItem {

        constructor(public data: TweetHubClient.TweetData) {
        }

        public appendTo($parent: JQuery) {
            var $ele = this.createItemRoot(
                this.createTitle(this.data.date, this.data.userName),
                this.createBody(this.data.message));
            $parent.prepend($ele);
        }

        private createItemRoot($title: JQuery, $body: JQuery) {
            return $("<li/>").append(
                    $("<div/>")
                    .addClass("chat-timeline__item")
                    .append($title)
                    .append($body));
        }

        private createTitle(date: Date, userName: string) {
            return $("<div/>")
                .addClass("chat-timeline__item-title")
                .html("[" + Utils.formatDate(date) + "] " + userName);
        }

        private createBody(msg: string) {
            return $("<div/>")
                .addClass("chat-timeline__item-body")
                .html(msg);
        }
    }

    /**
     * ユーザーリストビュー
     */
    class UserList {

        private $root: JQuery;

        constructor($root: JQuery) {
            this.$root = $root;
        }

        /**
         * ユーザーの追加
         */
        public addUser(name: string) {
            $("<li/>")
                .addClass("chat-userlist__user")
                .append("<p>" + name + "</p>")
                .appendTo($(".chat-userlist ul", this.$root));
        }

        /**
         * ユーザーの削除
         */
        public removeUser(name: string) {
            $(".chat-userlist__user", this.$root).each((idx, ele) => {
                var $this = $(ele);
                if ($this.children("p").html() != name)
                    return;
                $this.remove();
            });
        }
    }


    /**
     * アプリケーションの初期化
     */
    export function initApp($root: JQuery, client: TweetHubClient.Client, groupName : string, userName : string) {

        $(".chat-submission__send-button", $root).click(() => {
            client.tweet({
                date: new Date(),
                message: $(".chat-submission__message", $root).val(),
                userName : userName
            }).fail(() => {
                console.log("メッセージ送信に失敗しました。。");
            });
        });

        client.onTweetEvent((data) => {
            var itemView = new TimeLineItem(data);
            itemView.appendTo($(".chat-timeline > ul", $root));
        });

        var userListView = new UserList($root);
        var userListSource = new Chat.UserListSource.UserListSource(
            (name) => { userListView.addUser(name); }
            , (name) => { userListView.removeUser(name); });

        client.onAddUserEvent((name) => {
            userListSource.regist(Chat.UserListSource.UserListSourceRegistType.Add, name);
        });

        client.onExitUserEvent((name) => {
            userListSource.regist(Chat.UserListSource.UserListSourceRegistType.Delete, name);
        });

    }
}

module Chat.Auth {

    function getItemFromQueryString(qs: string, key: string) {
        var result: string;
        _.map(qs.split("&"), (s) => {
            var k = s.split("=")[0];
            var v = s.split("=")[1];
            if (k === key)
                result = decodeURIComponent(v);
        });
        return result;
    }

    function getUserName(qs: string) {
        return getItemFromQueryString(qs, "username");
    }

    function getGroupName(qs: string) {
        return getItemFromQueryString(qs, "groupname");
    }

    export function autuLogin() {

        var createResult = (uname: string = undefined, gname: string = undefined) => {
            return {
                    groupName: gname,
                    userName: uname
                }
        };

        var qs = Utils.getArrayItem(window.location.href.split("?"), 1);

        if (qs === undefined)
            return createResult();

        var userName = getUserName(qs);
        var grpName = getGroupName(qs);

        return createResult(userName, grpName)
    }
}



$(() => {

    var loginResult = Chat.Auth.autuLogin();

    //サーバーコネクション設定
    var connection = $.hubConnection("/signalr/signalr", true);
    connection.qs = loginResult;

    //サーバー内ハブの取得
    var hub = connection.createHubProxy("tweetHub");

    //ハブ初期化
    var client = new TweetHubClient.Client(hub);
    Chat.initApp($("#chat-app"), client, loginResult.groupName, loginResult.userName);

    //開始
    connection.start().fail(() => {
        alert("ログインしてください");
    });
});
