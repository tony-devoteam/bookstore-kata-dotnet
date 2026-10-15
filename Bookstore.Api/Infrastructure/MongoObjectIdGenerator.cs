using Bookstore.Api.Application;
using MongoDB.Bson;

namespace Bookstore.Api.Infrastructure;

public sealed class MongoObjectIdGenerator : IBookIdGenerator
{
    public string NewId() => ObjectId.GenerateNewId().ToString();
}
