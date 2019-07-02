using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Just.WPF.Views.MongoDBTool
{
    public class MongoDBHelper
    {
        #region prop
        private string _connectionString;
        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                _connectionString = value;
                _client = null;
                _database = null;
            }
        }
        private string _databaseName;
        public string DatabaseName
        {
            get => _databaseName;
            set
            {
                _databaseName = value;
                _database = null;
            }
        }
        public string CollectionName { get; set; }

        private IMongoClient _client;
        private IMongoClient Client
        {
            get
            {
                _client = _client ?? new MongoClient(ConnectionString);
                return _client;
            }
        }
        private IMongoDatabase _database;
        public IMongoDatabase Database
        {
            get
            {
                _database = _database ?? Client?.GetDatabase(DatabaseName);
                return _database;
            }
        }
        public IMongoCollection<T> GetCollection<T>()
        {
            return Database.GetCollection<T>(CollectionName);
        }

        #endregion

        #region ctor
        public MongoDBHelper(string connectionString, string database, string collection)
        {
            ConnectionString = connectionString;
            DatabaseName = database;
            CollectionName = collection;
        }

        #endregion
        
        #region crud
        public void InsertOne<T>(T doc)
        {
            GetCollection<T>().InsertOne(doc);
        }
        public void InsertMany<T>(IEnumerable<T> docs)
        {
            GetCollection<T>().InsertMany(docs);
        }

        public List<T> Find<T>(Expression<Func<T, bool>> filter)
        {
            return GetCollection<T>().Find(filter).ToList();
        }
        public List<T> Find<T>()
        {
            return GetCollection<T>().Find(_ => true).ToList();
        }
        public T FindOneAndUpdate<T>(Expression<Func<T, bool>> filter, UpdateDefinition<T> update)
        {
            return GetCollection<T>().FindOneAndUpdate(filter, update);
        }
        public T FindOneAndReplace<T>(Expression<Func<T, bool>> filter, T replace)
        {
            return GetCollection<T>().FindOneAndReplace(filter, replace);
        }
        public T FindOneAndDelete<T>(Expression<Func<T, bool>> filter)
        {
            return GetCollection<T>().FindOneAndDelete(filter);
        }

        public bool UpdateOne<T>(Expression<Func<T, bool>> filter, UpdateDefinition<T> update)
        {
            var result = GetCollection<T>().UpdateOne<T>(filter, update);
            return result.IsAcknowledged;
        }
        public bool UpdateMany<T>(Expression<Func<T, bool>> filter, UpdateDefinition<T> update)
        {
            var result = GetCollection<T>().UpdateMany(filter, update);
            return result.IsAcknowledged;
        }

        public bool ReplaceOne<T>(Expression<Func<T, bool>> filter, T replace)
        {
            var result = GetCollection<T>().ReplaceOne(filter, replace);
            return result.IsAcknowledged;
        }

        public bool DeleteOne<T>(Expression<Func<T, bool>> filter)
        {
            var result = GetCollection<T>().DeleteOne(filter);
            return result.IsAcknowledged;
        }
        public bool DeleteMany<T>(Expression<Func<T, bool>> filter)
        {
            var result = GetCollection<T>().DeleteMany(filter);
            return result.IsAcknowledged;
        }

        #endregion

        #region json
        public static List<T> FromManyJson<T>(string json)
        {
            var result = new List<T>();
            var pattern = @"\{((?<Open>\{)|(?<-Open>\})|[^{}])*(?(Open)(?!))\}";
            var matches = Regex.Matches(json, pattern);
            foreach (Match item in matches)
            {
                try
                {
                    result.Add(FromJson<T>(item.Value));
                }
                catch (Exception)
                {
                    //throw;
                }
            }
            return result;
        }
        public static T FromJson<T>(string json)
        {
            return BsonSerializer.Deserialize<T>(json);
        }
        public static string ToJson<T>(T doc)
        {
            return doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true });
        }
        #endregion
    }
}
