dotnet publish .\aws.lambda.product.api\aws.lambda.product.api.sln --configuration "Release" --framework "net6.0" /p:GenerateRuntimeConfigurationFiles=true --runtime linux-x64 --self-contained false
terraform apply