module Chat.UserListSource {

    /**
     * ユーザーリスト登録種類
     */
    export enum UserListSourceRegistType {
        /**
         * 追加
         */
        Add = 0,
        /**
         * 削除
         */
        Delete = 1
    }

    /**
     * ユーザーリストレコード
     */
    export class UserListRecord {
        constructor(
            public type: UserListSourceRegistType,
            public userName: string,
            public date: Date = new Date()) { }
    }

    /**
     * ユーザーリスト
     */
    export class UserListSource {

        private _list: Utils.Dictionary<string, Array<UserListRecord>>;
        private _delOnlyRetryInfo: Array<{ key: string; count: number }>;

        constructor(
            public onAdded: (username: string) => void,
            public onDeleted: (username: string) => void,
            public fireInterval: number = 1500,
            public cullInterval :number = 1500,
            public delOnlyRetryMax : number = 2) {

            this._list = new Utils.Dictionary<string, Array<UserListRecord>>();
            this._delOnlyRetryInfo = [];

            var loop = () => {
                setTimeout(() => {
                    this.cutListAndFire();
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
        private updateDelOnlyRetryInfo(targetKey: string) {

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
        }

        /**
         * リトライ情報のリセット
         */
        private resetDelOnlyRetryInfo(targetKey: string) {
            for (var i = 0; i < this._delOnlyRetryInfo.length; i++) {
                var retryInfo = this._delOnlyRetryInfo[i];
                if (retryInfo.key != targetKey)
                    continue;
                retryInfo.count = 0;
                this._delOnlyRetryInfo[i] = retryInfo;
            }
        }

        private cutListAndFire() {

            var keys = this._list.keys();
            var keyCount = keys.length;

            for (var i = 0; i < keyCount; i++) {

                var key = keys[i];
                var item = this._list.get(key);

                if (item.length == 0)
                    continue;

                if (item.length == 1
                    && item[0].type == UserListSourceRegistType.Delete
                    && this.updateDelOnlyRetryInfo(key)) {
                        continue;
                }

                var head = _.head(item);
                var last = _.last(item);

                console.log("[HeadItem] type:" + head.type + ", user:" + head.userName + ", date:" + head.date.getTime());
                console.log("[LastItem] type:" + last.type + ", user:" + last.userName + ", date:" + last.date.getTime());

                if (head.type == last.type) {
                    var event = head.type == UserListSourceRegistType.Add ? this.onAdded : this.onDeleted;
                    event(last.userName);
                }

                else if (head.type != last.type && last.type == UserListSourceRegistType.Add) {
                    var opeinval = last.date.getTime() - head.date.getTime();

                    console.log("opeinval:" + opeinval + ", mabiki:" + this.cullInterval);

                    var event = opeinval < this.cullInterval
                        ? () => { }
                        : (u: string) => { this.onDeleted(u); this.onAdded(u); };
                    event(last.userName);
                }

                //else if (head.type != last.type && last.type == UserListSourceRegistType.Delete) {
                //
                //}

                this.resetDelOnlyRetryInfo(key);
                this._list.set(key, []);
            }
        }

        /**
         * ユーザーリストへ登録
         */
        regist(type: UserListSourceRegistType, userName: string) {
            var newrec = new UserListRecord(type, userName);
            var setData = [];
            if (!this._list.exist(userName)) {
                setData = [newrec];
            } else {
                setData = this._list.get(userName);
                setData.push(newrec);
            }

            this._list.set(userName, setData);
        }
    }
} 