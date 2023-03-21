using Amazon.DynamoDBv2.DataModel;

namespace aws.lambda.product.api.DTO
{
    public class Product
    {
        public string? Id { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
    }
}
