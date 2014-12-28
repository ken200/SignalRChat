var Chat;
(function (Chat) {
    (function (_UserListSource) {
        /**
        * ユーザーリスト登録種類
        */
        (function (UserListSourceRegistType) {
            /**
            * 追加
            */
            UserListSourceRegistType[UserListSourceRegistType["Add"] = 0] = "Add";

            /**
            * 削除
            */
            UserListSourceRegistType[UserListSourceRegistType["Delete"] = 1] = "Delete";
        })(_UserListSource.UserListSourceRegistType || (_UserListSource.UserListSourceRegistType = {}));
        var UserListSourceRegistType = _UserListSource.UserListSourceRegistType;

        /**
        * ユーザーリストレコード
        */
        var UserListRecord = (function () {
            function UserListRecord(type, userName, date) {
                if (typeof date === "undefined") { date = new Date(); }
                this.type = type;
                this.userName = userName;
                this.date = date;
            }
            return UserListRecord;
        })();
        _UserListSource.UserListRecord = UserListRecord;

        /**
        * ユーザーリスト
        */
        var UserListSource = (function () {
            function UserListSource(onAdded, onDeleted, fireInterval, cullInterval, delOnlyRetryMax) {
                if (typeof fireInterval === "undefined") { fireInterval = 1500; }
                if (typeof cullInterval === "undefined") { cullInterval = 1500; }
                if (typeof delOnlyRetryMax === "undefined") { delOnlyRetryMax = 2; }
                var _this = this;
                this.onAdded = onAdded;
                this.onDeleted = onDeleted;
                this.fireInterval = fireInterval;
                this.cullInterval = cullInterval;
                this.delOnlyRetryMax = delOnlyRetryMax;
                this._list = new Utils.Dictionary();
                this._delOnlyRetryInfo = [];

                var loop = function () {
                    setTimeout(function () {
                        _this.cutListAndFire();
                        loop();
                    }, fireInterval);
                };
                loop();
            }
            /**
            * リトライ情報の更新
            * 更新できなかった場合は、ゼロリセットする
            *
            * @returns 更新結果(true:更新できた,false:リトライ数上限オーバーのため、更新できなかった)
            *
            */
            UserListSource.prototype.updateDelOnlyRetryInfo = function (targetKey) {
                console.log("[updateDelOnlyRetryInfo] start");

                for (var i = 0; i < this._delOnlyRetryInfo.length; i++) {
                    var retryInfo = this._delOnlyRetryInfo[i];
                    if (retryInfo.key != targetKey)
                        continue;
                    retryInfo.count += 1;
                    retryInfo.count = retryInfo.count > this.delOnlyRetryMax ? 0 : retryInfo.count;
                    this._delOnlyRetryInfo[i] = retryInfo;

                    console.log("[updateDelOnlyRetryInfo] key:" + retryInfo.key + ", count:" + retryInfo.count);

                    return retryInfo.count != 0;
                }
                this._delOnlyRetryInfo.push({ key: targetKey, count: 1 });

                console.log("[updateDelOnlyRetryInfo] ### NEW ### key:" + targetKey + ", count:1");

                return true;
            };

            /**
            * リトライ情報のリセット
            */
            UserListSource.prototype.resetDelOnlyRetryInfo = function (targetKey) {
                for (var i = 0; i < this._delOnlyRetryInfo.length; i++) {
                    var retryInfo = this._delOnlyRetryInfo[i];
                    if (retryInfo.key != targetKey)
                        continue;
                    retryInfo.count = 0;
                    this._delOnlyRetryInfo[i] = retryInfo;
                }
            };

            UserListSource.prototype.cutListAndFire = function () {
                var _this = this;
                var keys = this._list.keys();
                var keyCount = keys.length;

                for (var i = 0; i < keyCount; i++) {
                    var key = keys[i];
                    var item = this._list.get(key);

                    if (item.length == 0)
                        continue;

                    if (item.length == 1 && item[0].type == 1 /* Delete */ && this.updateDelOnlyRetryInfo(key)) {
                        continue;
                    }

                    var head = _.head(item);
                    var last = _.last(item);

                    console.log("[HeadItem] type:" + head.type + ", user:" + head.userName + ", date:" + head.date.getTime());
                    console.log("[LastItem] type:" + last.type + ", user:" + last.userName + ", date:" + last.date.getTime());

                    if (head.type == last.type) {
                        var event = head.type == 0 /* Add */ ? this.onAdded : this.onDeleted;
                        event(last.userName);
                    } else if (head.type != last.type && last.type == 0 /* Add */) {
                        var opeinval = last.date.getTime() - head.date.getTime();

                        console.log("opeinval:" + opeinval + ", mabiki:" + this.cullInterval);

                        var event = opeinval < this.cullInterval ? function () {
                        } : function (u) {
                            _this.onDeleted(u);
                            _this.onAdded(u);
                        };
                        event(last.userName);
                    }

                    //else if (head.type != last.type && last.type == UserListSourceRegistType.Delete) {
                    //
                    //}
                    this.resetDelOnlyRetryInfo(key);
                    this._list.set(key, []);
                }
            };

            /**
            * ユーザーリストへ登録
            */
            UserListSource.prototype.regist = function (type, userName) {
                var newrec = new UserListRecord(type, userName);
                var setData = [];
                if (!this._list.exist(userName)) {
                    setData = [newrec];
                } else {
                    setData = this._list.get(userName);
                    setData.push(newrec);
                }

                this._list.set(userName, setData);
            };
            return UserListSource;
        })();
        _UserListSource.UserListSource = UserListSource;
    })(Chat.UserListSource || (Chat.UserListSource = {}));
    var UserListSource = Chat.UserListSource;
})(Chat || (Chat = {}));
//# sourceMappingURL=UserListSource.js.map
