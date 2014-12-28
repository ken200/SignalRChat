var Utils;
(function (Utils) {
    

    /**
    * 配列から指定要素を取得する
    */
    function getArrayItem(ary, idx, ifFail) {
        if (typeof ifFail === "undefined") { ifFail = undefined; }
        if (ary === undefined)
            return ifFail;
        if (ary.length - 1 < idx)
            return ifFail;
        return ary[idx];
    }
    Utils.getArrayItem = getArrayItem;

    /**
    * 日付整形
    */
    function formatDate(date, format) {
        if (typeof format === "undefined") { format = undefined; }
        var paddingZero = function (n) {
            if (n < 10)
                return "0" + n;
            return n.toString();
        };

        return date.getFullYear() + "/" + paddingZero(date.getMonth() + 1) + "/" + paddingZero(date.getDate()) + " " + paddingZero(date.getHours()) + ":" + paddingZero(date.getMinutes()) + ":" + paddingZero(date.getSeconds());
    }
    Utils.formatDate = formatDate;

    /**
    * ディクショナリー
    */
    var Dictionary = (function () {
        function Dictionary() {
            this._values = [];
            this._keys = [];
        }
        Dictionary.prototype.set = function (key, v) {
            var recIdx = this._keys.indexOf(key);
            if (recIdx == -1) {
                this._keys.push(key);
                this._values.push({ key: key, val: v });
            } else {
                this._values[recIdx] = { key: key, val: v };
            }
        };

        Dictionary.prototype.get = function (key) {
            var recIdx = this._keys.indexOf(key);
            return recIdx == -1 ? undefined : this._values[recIdx].val;
        };

        Dictionary.prototype.exist = function (key) {
            return this._keys.indexOf(key) == -1 ? false : true;
        };

        Dictionary.prototype.keys = function () {
            return this._keys;
        };
        return Dictionary;
    })();
    Utils.Dictionary = Dictionary;
})(Utils || (Utils = {}));
//# sourceMappingURL=Utils.js.map
