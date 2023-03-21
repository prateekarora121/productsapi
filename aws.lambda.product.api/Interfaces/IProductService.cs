using aws.lambda.product.api.DTO;

namespace aws.lambda.product.api.Interfaces
{
    public interface IProductService
    {
        Task<Response> GetProductsById(string id,string category);
        Task<Response> Save(Product product);
        Task<Response> Delete(string id,string category);
        Task<Response> UploadFile(IFormFile file);
        Task<Response> DownloadFile(string filename);
        Task<Response> DeleteFile(string filename);
        Task<Response> GetAllProducts();
    }
}
