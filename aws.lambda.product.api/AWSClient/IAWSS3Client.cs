using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.S3.Model;

namespace aws.lambda.product.api.AWSClient
{
    public interface IAWSS3Client
    {
        string GetBucketName();
        AmazonS3Client GetAWSS3Client();
        PutObjectRequest GetBucketObject();
        AmazonDynamoDBClient GetDynamoDbClient();
    }
}
