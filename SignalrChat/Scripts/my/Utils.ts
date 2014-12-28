module Utils {

    /**
     * タプル(2組)
     */
    export interface Tuple2<T1, T2> {
        item1: T1;
        item2: T2;
    }

    /**
     * 配列から指定要素を取得する
     */
    export function getArrayItem(ary: any[], idx: number, ifFail: any = undefined) {
        if (ary === undefined)
            return ifFail;
        if (ary.length - 1 < idx)
            return ifFail;
        return ary[idx];
    }

    /**
     * 日付整形
     */
    export function formatDate(date: Date, format: string = undefined) {

        var paddingZero = (n: number): string => {
            if (n < 10)
                return "0" + n;
            return n.toString();
        };

        return date.getFullYear()
            + "/"
            + paddingZero(date.getMonth() +1)
            + "/"
            + paddingZero(date.getDate())
            + " "
            + paddingZero(date.getHours())
            + ":"
            + paddingZero(date.getMinutes())
            + ":"
            + paddingZero(date.getSeconds());
    }

    /**
     * ディクショナリー
     */
    export class Dictionary<K, V>{

        private _keys: Array<K>;
        private _values: Array<{ key: K; val : V}>;

        constructor() {
            this._values = [];
            this._keys = [];
        }

        set(key: K, v: V): void {
            var recIdx = this._keys.indexOf(key);
            if (recIdx == -1) {
                this._keys.push(key);
                this._values.push({ key: key, val: v });
            } else {
                this._values[recIdx] = { key: key, val: v };
            }
        }

        get(key: K) : V {
            var recIdx = this._keys.indexOf(key);
            return recIdx == -1 ? undefined : this._values[recIdx].val;
        }

        exist(key: K): boolean {
            return this._keys.indexOf(key) == -1 ? false : true;
        }

        keys(): Array<K> {
            return this._keys;
        }
    }
}