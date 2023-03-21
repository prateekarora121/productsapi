using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AutoMapper;
using aws.lambda.product.api.AWSClient;
using aws.lambda.product.api.DTO;
using aws.lambda.product.api.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Net.Sockets;

namespace aws.lambda.product.api.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IDynamoDBContext _dynamoDBContext;
        private readonly IMapper _mapper;
        private readonly IAWSS3Client _awsS3Client;
        private readonly ILogger<ProductService> logger;
        private readonly string folderName = "products/";
        private readonly string tableName = "products";
        private readonly int MB = 5 * 1048576;
        public ProductService(IDynamoDBContext dynamoDBContext, IMapper mapper
            , IAWSS3Client awsS3Client, ILogger<ProductService> logger)
        {
            this._dynamoDBContext = dynamoDBContext;
            this._mapper = mapper;
            this._awsS3Client = awsS3Client;
            this.logger = logger;
        }


        public async Task<Response> GetAllProducts()
        {
            var response = new Response();
            try
            {

                var request = new ScanRequest
                {
                    TableName = tableName,
                };
                var products = await _awsS3Client.GetDynamoDbClient().ScanAsync(request);

                //var config = new DynamoDBOperationConfig();
                if (products.Count ==0)
                {
                    response.Data = null;
                    response.IsSuccess = false;
                    response.ErrorMessage = "No data found!";
                }
                else
                {
                    var productList =new List<Product>();
                    foreach (var product in products.Items) {
                        product.TryGetValue("id", out var id);
                        product.TryGetValue("name", out var name);
                        product.TryGetValue("category", out var category);
                        product.TryGetValue("description", out var description);
                        product.TryGetValue("price", out var price);

                        productList.Add(new Product() 
                        { 
                            Id= id?.S,
                            Name=name?.S,
                            Category=category?.S,
                            Description=description?.S,
                            Price=Convert.ToDecimal(price?.N)
                        });
                    }
                    response.Data = productList;
                    response.IsSuccess = true;
                    response.ErrorMessage = string.Empty;
                }
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error at ProductsService -> GetAllProducts {ex.Message}");
                throw;
            }
        }
        public async Task<Response> GetProductsById(string id, string category)
        {
            var response = new Response();
            try
            {
                //var config = new DynamoDBOperationConfig();
                var product = await _dynamoDBContext.LoadAsync<DynamoDB.Models.Product>(id, category);

                response.Data = product;
                response.IsSuccess = true;
                response.ErrorMessage = string.Empty;

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error at ProductsService -> GetProductsById {ex.Message}");
                throw;
            }
        }

        public async Task<Response> Save(Product product)
        {
            try
            {
                DynamoDB.Models.Product prod = this._mapper.Map<DynamoDB.Models.Product>(product);
                prod.setID();
                await _dynamoDBContext.SaveAsync(prod);
                return new Response(true, null, null);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error at ProductsService -> Save {ex.Message}");
                throw;
            }
        }

        public async Task<Response> Delete(string id, string category)
        {
            try
            {
                var res = await GetProductsById(id, category);
                if (res.Data != null)
                {
                    await _dynamoDBContext.DeleteAsync<DynamoDB.Models.Product>(id, category);
                }
                else
                {
                    logger.LogError($"No Record Found with id: {id} and category: {category} at ProductsService -> Delete");
                    return new Response(false, null, $"No Record Found with id: {id} and category: {category}");
                }
                return new Response(true, null, $"Record Deleted with id: {id} and category: {category}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error at ProductsService -> Delete {ex.Message}");
                throw;
            }
            // DeleteAsync is used to delete an item from DynamoDB
        }

        public Task<Response> DownloadFile(string filename)
        {
            var response = new Response();
            try
            {

                var request = new GetPreSignedUrlRequest()
                {
                    BucketName = this._awsS3Client.GetBucketName(),
                    Key = folderName + filename,
                    Expires = DateTime.Now.AddHours(24),
                    Protocol = Protocol.HTTPS
                };
                var downloadLink = this._awsS3Client.GetAWSS3Client().GetPreSignedURL(request);

                response.Data = downloadLink;
                response.IsSuccess = true;
                response.ErrorMessage = string.Empty;

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error at ProductsService -> Delete {ex.Message}");
                throw;
            }
        }

        private dynamic ToStream(IFormFile file)
        {
            dynamic stream=null;
            if (file.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    stream= new MemoryStream(fileBytes);

                }

            }
            return stream;
        }
        public async Task<Response> UploadFile(IFormFile file)
        {
            var response = new Response();
            try
            {
                //var config = new DynamoDBOperationConfig();
                var bucket = _awsS3Client.GetBucketObject();
                bucket.Key = folderName + file.FileName;
                bucket.ContentType = file.Headers.ContentType;

                if (file.Length > 0)
                {
                    bucket.InputStream = ToStream(file);

                    if (bucket.InputStream.Length <= 0)
                    {
                        return (new Response(false, null, $"No File Found"));
                    }
                    response.Data = null;
                    response.IsSuccess = true;
                    response.ErrorMessage = string.Empty;
                }
                else
                {
                    response.Data = null;
                    response.IsSuccess = false;
                    response.ErrorMessage = "file not Found";
                }


                if (bucket.InputStream.Length >= MB)
                {
                    await UploadLargeFile(file);
                }
                else if (bucket.ContentType.Contains("image"))
                { 
                await uploadMediaFile(file);
                }
                else
                {
                    await _awsS3Client.GetAWSS3Client().PutObjectAsync(bucket);
                }
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error at ProductsService -> uploadFile {ex.Message}");
                throw;
            }
        }


        private async Task uploadMediaFile(IFormFile file)
        {
            TransferUtility utility = new TransferUtility(_awsS3Client.GetAWSS3Client());
            // making a TransferUtilityUploadRequest instance
            TransferUtilityUploadRequest request = new TransferUtilityUploadRequest();
          
            request.BucketName = _awsS3Client.GetBucketName();
            request.Key = folderName + file.FileName; //file name up in S3
            request.InputStream = ToStream(file); //local file name
            utility.Upload(request);
            return;

        }

            private async Task UploadLargeFile(IFormFile file)
        {
            MemoryStream stream = ToStream(file);

            var initiateRequest = new InitiateMultipartUploadRequest()
            {
                BucketName=_awsS3Client.GetBucketName(),
                Key= folderName + file.FileName

        };

           var response= await _awsS3Client.GetAWSS3Client().InitiateMultipartUploadAsync(initiateRequest);

            var contentLength = stream.Length;
            //int chunkSize_ = 5 * (int)Math.Pow(2, 10);
            int chunkSize= MB;
            var chunkList =new List<PartETag>();

            try {

                int filePosition = 0;
                for (int i = 1; filePosition < contentLength; i++) {
                    var uploadRequest = new UploadPartRequest() {
                        BucketName = _awsS3Client.GetBucketName(),
                        Key = folderName + file.FileName,
                        UploadId=response.UploadId,
                        PartNumber=i,
                        PartSize=chunkSize,
                        InputStream=stream

                    };

                    var uploadPartResponse = await _awsS3Client.GetAWSS3Client().UploadPartAsync(uploadRequest);
                    chunkList.Add(new PartETag() {
                        ETag = uploadPartResponse.ETag,
                        PartNumber= i,
                    });
                    filePosition += chunkSize;
                }

                var completeRequest = new CompleteMultipartUploadRequest()
                {
                    BucketName = _awsS3Client.GetBucketName(),
                    Key = folderName + file.FileName,
                    UploadId = response.UploadId,
                    PartETags = chunkList
                };
                await _awsS3Client.GetAWSS3Client().CompleteMultipartUploadAsync(completeRequest);


            }
            catch (Exception ex)
            {
                logger.LogError($"Error at ProductsService -> UploadLargeFile {ex.Message}");
                throw;
            }

        }
           


        public async Task<Response> DeleteFile(string filename)
        {
            var response = new Response();
            try
            {

                var request = new DeleteObjectRequest()
                {
                    BucketName = this._awsS3Client.GetBucketName(),
                    Key = folderName + filename,

                };
                await this._awsS3Client.GetAWSS3Client().DeleteObjectAsync(request);

                response.Data = "File deleted!";
                response.IsSuccess = true;
                response.ErrorMessage = string.Empty;

                return (response);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error at ProductsService -> DeleteFile {ex.Message}");
                throw;
            }
        }
    }
}
