using aws.lambda.product.api.AWSClient;
using aws.lambda.product.api.DTO;
using aws.lambda.product.api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aws.lambda.product.api.Controllers
{
    [Route("api/products")]
    [ApiController,Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;
        public ProductsController(IProductService productsService,ILogger<ProductsController> logger)
        {
            _productService = productsService;
            _logger = logger;
        }


        [Route("get")]
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Api is Working!");
        }

        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var response = await _productService.GetAllProducts();
                if (response.IsSuccess)
                {
                    if (response.Data is null)
                        return NotFound();
                    return Ok(response.Data);
                }
                else
                {
                    return NotFound(response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        [Route("")]
        [HttpGet]
        public async Task<IActionResult> Get( string category, string id)
        {
            try
            {
                var response = await _productService.GetProductsById(id, category);
                if (response.IsSuccess)
                {
                    if (response.Data is null)
                        return NotFound();
                    return Ok(response.Data);
                }
                else
                    return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }


        [Route("")]
        [HttpPost]
        public async Task<IActionResult> Save(Product product)
        {
            // SaveAsync is used to put an item in DynamoDB, it will overwite if an item with the same primary key already exists
            try
            {
                var response = await _productService.Save(product);
                if (response.IsSuccess)
                    return StatusCode(201);
                else
                    return StatusCode(500);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        [Route("")]
        [HttpDelete]
        public async Task<IActionResult> Delete(string id,string category)
        {
            // DeleteAsync is used to delete an item from DynamoDB

            try
            {
                var response = await _productService.Delete(id,category);
                if (response.IsSuccess)
                    return StatusCode(204);
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }


        [Route("file")]
        [RequestSizeLimit(737280000)]
        [HttpPost]
        public async Task<IActionResult> UploadFile() 
        {
            if (Request.ContentLength==0)
            {
                return BadRequest();
            }
            IFormFile file = Request.Form.Files[0];
            var bucket = await this._productService.UploadFile(file);
            if (bucket.IsSuccess)
            {
                var res= await this._productService.DownloadFile(file.FileName);
                return Ok(res);
            }
            else
            {
                return BadRequest(bucket.ErrorMessage);
            }
        }

        [Route("file")]
        [HttpGet]
        public async Task<IActionResult> DownloadFile(string filename)
        {
            var response = await this._productService.DownloadFile(filename);
            if (response.IsSuccess)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response.ErrorMessage);
            }
        }

        [Route("file")]
        [HttpDelete]
        public async Task<IActionResult> DeleteFile(string filename)
        {
            var response = await this._productService.DeleteFile(filename);
            if (response.IsSuccess)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response.ErrorMessage);
            }
        }


    }
}
