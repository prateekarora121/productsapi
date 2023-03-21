using AutoMapper;
using aws.lambda.product.api.DTO;

namespace aws.lambda.product.api.Mapper
{
    public class ProductMapper:Profile
    {
        public ProductMapper()
        {
            //source mapping to destination
            CreateMap<Product, DynamoDB.Models.Product>();
        }
    }
}
