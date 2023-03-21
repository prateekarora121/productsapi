using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.S3.Model;
using aws.lambda.product.api.DTO;
using Microsoft.Extensions.Options;

namespace aws.lambda.product.api.AWSClient
{
    public class AWSS3Client:IAWSS3Client
    {
        private string accessKey { get; set; }
        private string secretKey { get; set; }
        private AmazonS3Client client { get; set; }
        private PutObjectRequest bucketObject { get; set; }
        private AmazonDynamoDBClient dynamoDBClient { get; set; }

        //private readonly string  BucketName = "awsproductdemo";
        private readonly string BucketName = "demolambda01234";

        //public string BucketName = "awsproductdemo"; 

        public AWSS3Client(IOptions<SecretManager> secretManager,IConfiguration config)
        {
            accessKey = secretManager.Value.AccessKey.ToString();
            secretKey = secretManager.Value.SecretKey.ToString();
        }
        public AmazonS3Client GetAWSS3Client()
        {
            if (client == null) {

                this.client = new AmazonS3Client(this.accessKey, this.secretKey,Amazon.RegionEndpoint.APSoutheast1);
            }

            return client;
        }

        public AmazonDynamoDBClient GetDynamoDbClient()
        {
            if (dynamoDBClient == null)
            {
                this.dynamoDBClient = new AmazonDynamoDBClient(accessKey, secretKey);
            }

            return dynamoDBClient;
        }

        public string GetBucketName()
        { return BucketName; }
        public PutObjectRequest GetBucketObject()
        {
            if (bucketObject == null) {
                this.bucketObject = new PutObjectRequest() {
                    BucketName = this.BucketName
                };
            }
            return bucketObject;
        
        }
    }
}
