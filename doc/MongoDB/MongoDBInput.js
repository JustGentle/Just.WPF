
//处理选项
var options = {
    all: true, //true: 针对所有数据，false: 仅针对输入数据
    //type: {value: 0, display: "仅检查数据"}
    //type: {value: 1, display: "检查并新增"}
    type: {value: 2, display: "检查并同步"}//新增+更新+删除
}

//检查结果
var result = {
    "新增": [], //新增数据
    "更新": {}, //差异数据
    "重复": {}, //重复数据
    "多余": [], //多余数据
    "相同": [] //相同数据
}

// 输入开关选项数据
var data = [];
/*
******************************************************************************
******************************************************************************
*/
var sysCache = db.getCollection('CacheSysProfileMode');
//数据处理方法
function onAdd(item){
    delete item._id;
    result["新增"].push(item);
    if(options.type.value){
        sysCache.insert(item);
    }
}
function onDup(item, dup){
    var key = item.Mode + ':' + item.Item;
    result["重复"][key] = result["重复"][key] || {
        "保留": item,
        "移除": []
    };
    result["重复"][key]["移除"].push(dup);
    if(options.type.value === 2 && dup._id){
        sysCache.remove(dup);
    }
}
function onEq(item, eq){
    result["相同"].push(eq);
}
function onDiff(item, diff){
    delete item._id;
    result["更新"] = {"原": diff, "新": item};
    if(options.type.value === 2){
        sysCache.update(diff, {$set: item});
    }
}
function onNot(not){
    result["多余"].push(not);
    if(options.type.value === 2){
        sysCache.remove(not);
    }
}
//相同开关数据判断
function same(item, sItem){
    if(item.Mode === sItem.Mode
        && item.Item === sItem.Item
    )
        return true;
    return false;
}
//完全相同数据判断
function eq(item, eqItem){
    if(item.Mode !== eqItem.Mode
        || item.Item !== eqItem.Item
        || item.Display !== eqItem.Display
        || item.Hints !== eqItem.Hints
        || item.DefaultValue !== eqItem.DefaultValue
        || item.ValueType !== eqItem.ValueType
        || item.Category !== eqItem.Category
        || item.Visible !== eqItem.Visible
        || tojson(item.EnumValue || null) !== tojson(eqItem.EnumValue || null)
    )
        return false;
    return true;
}
//检查
if(options.all){
    //全部数据
    var collection = sysCache.find({});
    while(collection.hasNext()){
        var next = collection.next();
        var found = null;
        for(i in data){
            var item = data[i];
            if(same(item, next)){
                if(!found){
                    if(item._same){
                        onDup(item, next);
                    } else {
                        if(eq(item, next)){
                            onEq(item, next);
                        } else {
                            onDiff(item, next);
                        }
                        item._same = next;
                    }
                    found = item;
                } else {
                    item._dup = next;
                    delete item._id;
                    onDup(found, item);
                }
            }
        }
        if(!found)
            onNot(next);
    }
    for(i in data){
        var item = data[i];
        if(item._same){
            delete item._same;
        } else if(item._dup) {
            delete item._dup;
        } else {
            var dup = null;
            for(j in result["新增"]){
                if(same(item, result["新增"][j])){
                    dup = result["新增"][j];
                    break;
                }
            }
            if(dup){
                onDup(dup, item);
            } else {
                onAdd(item);
            }
        }
    }
} else {
    //指定数据
    for(i in data){
        var item = data[i];
        var found = null;
        var founds = sysCache.find({"Mode": item.Mode, "Item": item.Item});
        while(founds.hasNext()){
            var next = founds.next();
            if(found){
                onDup(item, next);
            }
            else if(eq(item, next)){
                onEq(item, next);
            } else {
                onDiff(item, next);
            }
            found = next;
        }
        if(!found)
            onAdd(item);
    }
}
//输出结果
print(result);