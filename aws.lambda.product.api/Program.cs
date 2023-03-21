using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Builder;
using Amazon.Extensions.NETCore.Setup;
using aws.lambda.product.api.Implementations;
using aws.lambda.product.api.Interfaces;
using Microsoft.AspNetCore.Hosting;
using aws.lambda.product.api.AWSClient;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using aws.lambda.product.api.DTO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

//JWT Token

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();

//

AWSOptions awsOptions = builder.Configuration.GetAWSOptions();

// Configure AWS service clients to use these credentials
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonDynamoDB>();


//if (!builder.Environment.IsDevelopment())
//{
//    builder.Configuration.AddUserSecrets<Program>();
//}
//else
//{
//builder.Configuration.AddSecretsManager(configurator: config =>
//{
//    config.KeyGenerator = (secret, name) => name.Replace("__", ":");
//});
////}
builder.Services.AddOptions<SecretManager>()
    .BindConfiguration("AWSSDK");
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

//
builder.Services.AddTransient<IAWSS3Client, AWSS3Client>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});



var app = builder.Build();

//jwt
app.UseAuthentication();
app.UseAuthorization();
//

app.UseSwagger();

// This middleware serves the Swagger documentation UI
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee API V1");
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "Welcome to running ASP.NET Core Minimal API on AWS Lambda").RequireAuthorization(); ;


app.MapPost("/authorize/createToken",
[AllowAnonymous] (User user) =>
{
    if (user.UserName == "test" && user.Password == "test")
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var key = Encoding.ASCII.GetBytes
        (builder.Configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
             }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        var stringToken = tokenHandler.WriteToken(token);
        return Results.Ok(stringToken);
    }
    return Results.Unauthorized();
});

app.Run();
