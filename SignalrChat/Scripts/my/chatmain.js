/// <reference path="../typings/jquery/jquery.d.ts" />
/// <reference path="../typings/signalr/signalr.d.ts" />
/// <reference path="../typings/underscore/underscore.d.ts" />
var Chat;
(function (Chat) {
    /**
    * タイムラインビュー
    */
    var TimeLineItem = (function () {
        function TimeLineItem(data) {
            this.data = data;
        }
        TimeLineItem.prototype.appendTo = function ($parent) {
            var $ele = this.createItemRoot(this.createTitle(this.data.date, this.data.userName), this.createBody(this.data.message));
            $parent.prepend($ele);
        };

        TimeLineItem.prototype.createItemRoot = function ($title, $body) {
            return $("<li/>").append($("<div/>").addClass("chat-timeline__item").append($title).append($body));
        };

        TimeLineItem.prototype.createTitle = function (date, userName) {
            return $("<div/>").addClass("chat-timeline__item-title").html("[" + Utils.formatDate(date) + "] " + userName);
        };

        TimeLineItem.prototype.createBody = function (msg) {
            return $("<div/>").addClass("chat-timeline__item-body").html(msg);
        };
        return TimeLineItem;
    })();

    /**
    * ユーザーリストビュー
    */
    var UserList = (function () {
        function UserList($root) {
            this.$root = $root;
        }
        /**
        * ユーザーの追加
        */
        UserList.prototype.addUser = function (name) {
            $("<li/>").addClass("chat-userlist__user").append("<p>" + name + "</p>").appendTo($(".chat-userlist ul", this.$root));
        };

        /**
        * ユーザーの削除
        */
        UserList.prototype.removeUser = function (name) {
            $(".chat-userlist__user", this.$root).each(function (idx, ele) {
                var $this = $(ele);
                if ($this.children("p").html() != name)
                    return;
                $this.remove();
            });
        };
        return UserList;
    })();

    /**
    * アプリケーションの初期化
    */
    function initApp($root, client, groupName, userName) {
        $(".chat-submission__send-button", $root).click(function () {
            client.tweet({
                date: new Date(),
                message: $(".chat-submission__message", $root).val(),
                userName: userName
            }).fail(function () {
                console.log("メッセージ送信に失敗しました。。");
            });
        });

        client.onTweetEvent(function (data) {
            var itemView = new TimeLineItem(data);
            itemView.appendTo($(".chat-timeline > ul", $root));
        });

        var userListView = new UserList($root);
        var userListSource = new Chat.UserListSource.UserListSource(function (name) {
            userListView.addUser(name);
        }, function (name) {
            userListView.removeUser(name);
        });

        client.onAddUserEvent(function (name) {
            userListSource.regist(0 /* Add */, name);
        });

        client.onExitUserEvent(function (name) {
            userListSource.regist(1 /* Delete */, name);
        });
    }
    Chat.initApp = initApp;
})(Chat || (Chat = {}));

var Chat;
(function (Chat) {
    (function (Auth) {
        function getItemFromQueryString(qs, key) {
            var result;
            _.map(qs.split("&"), function (s) {
                var k = s.split("=")[0];
                var v = s.split("=")[1];
                if (k === key)
                    result = decodeURIComponent(v);
            });
            return result;
        }

        function getUserName(qs) {
            return getItemFromQueryString(qs, "username");
        }

        function getGroupName(qs) {
            return getItemFromQueryString(qs, "groupname");
        }

        function autuLogin() {
            var createResult = function (uname, gname) {
                if (typeof uname === "undefined") { uname = undefined; }
                if (typeof gname === "undefined") { gname = undefined; }
                return {
                    groupName: gname,
                    userName: uname
                };
            };

            var qs = Utils.getArrayItem(window.location.href.split("?"), 1);

            if (qs === undefined)
                return createResult();

            var userName = getUserName(qs);
            var grpName = getGroupName(qs);

            return createResult(userName, grpName);
        }
        Auth.autuLogin = autuLogin;
    })(Chat.Auth || (Chat.Auth = {}));
    var Auth = Chat.Auth;
})(Chat || (Chat = {}));

$(function () {
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
    connection.start().fail(function () {
        alert("ログインしてください");
    });
});
//# sourceMappingURL=chatmain.js.map
