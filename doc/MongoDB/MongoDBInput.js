
//����ѡ��
var options = {
    "ִ������": true,
    "ִ�и���": true,
    "�Ƴ��ظ�": true,
    "ɾ������": true,
    "��ʾ��ͬ": true
}

//�����
var result = {
    "����": [],
    "����": {},
    "�ظ�": {},
    "����": [],
    "��ͬ": []
}

// ���뿪��ѡ������
var data = [];
/*
******************************************************************************
******************************************************************************
*/
var sysCache = db.getCollection('CacheSysProfileMode');
//���ݴ�����
function onAdd(item){
    delete item._id;
    result["����"].push(item);
    if(options["ִ������"]){
        sysCache.insert(item);
    }
}
function onDup(item, dup){
    var key = item.Mode + ':' + item.Item;
    result["�ظ�"][key] = result["�ظ�"][key] || {
        "����": item,
        "�Ƴ�": []
    };
    result["�ظ�"][key]["�Ƴ�"].push(dup);
    if(options["�Ƴ��ظ�"] && dup._id){
        sysCache.remove(dup);
    }
}
function onEq(item, eq){
	if(options["��ʾ��ͬ"]){
    	result["��ͬ"].push(eq);
	}
}
function onDiff(item, diff){
    delete item._id;
    result["����"] = {"ԭ": diff, "��": item};
    if(options["ִ�и���"]){
        //��������ֵ������
        var copy = JSON.parse(JSON.stringify(item));
        if(copy.ValueType === diff.ValueType) {
            copy.ItemValue = diff.ItemValue;
        }
        sysCache.update(diff, {$set: copy});
    }
}
function onNot(not){
    result["����"].push(not);
    if(options["ɾ������"]){
        sysCache.remove(not);
    }
}
//��ͬ���������ж�
function same(item, sItem){
    if(item.Mode === sItem.Mode
        && item.Item === sItem.Item
    )
        return true;
    return false;
}
//��ȫ��ͬ�����ж�
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
//���
if(options.all){
    //ȫ������
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
            for(j in result["����"]){
                if(same(item, result["����"][j])){
                    dup = result["����"][j];
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
    //ָ������
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

if(!options["ִ������"]){
	result["����(δִ��)"] = result["����"];
	delete result["����"];
}
if(!options["ִ�и���"]){
	result["����(δִ��)"] = result["����"];
	delete result["����"];
}
if(!options["�Ƴ��ظ�"]){
	result["�ظ�(δ�Ƴ�)"] = result["�ظ�"];
	delete result["�ظ�"];
}
if(!options["ɾ������"]){
	result["����(δɾ��)"] = result["����"];
	delete result["����"];
}
if(!options["��ʾ��ͬ"]){
	delete result["��ͬ"];
}
//������
print(result);