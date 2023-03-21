resource "aws_dynamodb_table" "synchronous_api_table" {
  name = var.table_name
  billing_mode = "PAY_PER_REQUEST"
  hash_key = "id"

  attribute {
    name = "id"
    type = "S"
      }
}

# S3 bucket to store code
resource "aws_s3_bucket" "lambda_bucket" {
  bucket = var.code_bucket_name

  # acl           = "private"
  force_destroy = true
}

resource "aws_s3_bucket_acl" "s3_acl" {
  bucket = aws_s3_bucket.lambda_bucket.id
  acl    = "private"
}

# Initialize module containing IAM policies
module "iam_policies" {
  source     = "./tf-modules/iam-policies"
  table_name = aws_dynamodb_table.synchronous_api_table.name
}

# Create Product Lambda
module "create_product_lambda" {
  source = "./tf-modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir = "${path.module}/aws.lambda.product.api/bin/Release/net6.0/publish"
  zip_file = "aws.lambda.product.api.zip"
  function_name = "aws_lambda_product_api"
  lambda_handler = "aws.lambda.product.api::aws.lambda.product.api.Function::FunctionHandler"
  environment_variables = {
    "PRODUCT_TABLE_NAME" = aws_dynamodb_table.synchronous_api_table.name
  }
}

module "create_product_lambda_api" {
  source = "./tf-modules/api-gateway-lambda-integration"
  api_id = module.api_gateway.api_id
  api_arn= module.api_gateway.api_arn
  function_arn = module.create_product_lambda.function_arn
  function_name = module.create_product_lambda.function_name
  http_method = "POST"
  route = "/"
}

resource "aws_iam_role_policy_attachment" "create_product_lambda_dynamo_db_write" {
  role  = module.create_product_lambda.function_role_name
  policy_arn = module.iam_policies.dynamo_db_write
}

resource "aws_iam_role_policy_attachment" "create_product_lambda_cw_metrics" {
  role  = module.create_product_lambda.function_role_name
  policy_arn = module.iam_policies.cloud_watch_put_metrics
}

module "api_gateway" {
  source = "./tf-modules/api-gateway"
  api_name = "synchronous-api"
  stage_name = "dev"
  stage_auto_deploy = true
}