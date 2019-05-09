var collection = db.getCollection('CacheSysProfileMode').find();
while(collection.hasNext()){
    var item = collection.next();
    delete item._id;
    print(tojson(item));
}