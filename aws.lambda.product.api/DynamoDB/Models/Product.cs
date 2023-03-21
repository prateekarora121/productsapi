using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.DynamoDBv2.DataModel;

namespace aws.lambda.product.api.DynamoDB.Models
{

    [DynamoDBTable("products")]
    public class Product
    {

        [DynamoDBHashKey("id")]
        public string Id { get; set; }

        [DynamoDBRangeKey("category")]
        public string Category { get; set; }

        [DynamoDBProperty("name")]
        public string Name { get; set; }

        [DynamoDBProperty("description")]
        public string Description { get; set; }

        [DynamoDBProperty("price")]
        public decimal Price { get; set; }

        public void setID()
        {
            if (Id == null)
                Id = Guid.NewGuid().ToString();
        }
    }
}
