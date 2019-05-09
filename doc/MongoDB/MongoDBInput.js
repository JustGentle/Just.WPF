
//处理选项
var options = {
    "执行新增": true,
    "执行更新": true,
    "移除重复": true,
    "删除多余": true,
    "显示相同": true
}

//检查结果
var result = {
    "新增": [],
    "更新": {},
    "重复": {},
    "多余": [],
    "相同": []
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
    if(options["执行新增"]){
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
    if(options["移除重复"] && dup._id){
        sysCache.remove(dup);
    }
}
function onEq(item, eq){
	if(options["显示相同"]){
    	result["相同"].push(eq);
	}
}
function onDiff(item, diff){
    delete item._id;
    result["更新"] = {"原": diff, "新": item};
    if(options["执行更新"]){
        //部分属性值不更新
        var copy = JSON.parse(JSON.stringify(item));
        if(copy.ValueType === diff.ValueType) {
            copy.ItemValue = diff.ItemValue;
        }
        sysCache.update(diff, {$set: copy});
    }
}
function onNot(not){
    result["多余"].push(not);
    if(options["删除多余"]){
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

if(!options["执行新增"]){
	result["新增(未执行)"] = result["新增"];
	delete result["新增"];
}
if(!options["执行更新"]){
	result["更新(未执行)"] = result["更新"];
	delete result["更新"];
}
if(!options["移除重复"]){
	result["重复(未移除)"] = result["重复"];
	delete result["重复"];
}
if(!options["删除多余"]){
	result["多余(未删除)"] = result["多余"];
	delete result["多余"];
}
if(!options["显示相同"]){
	delete result["相同"];
}
//输出结果
print(result);