using Amazon.Lambda.Core;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.EntityFrameworkCore;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RoadieRating;

public class Function
{

  DatabaseContext dbContext;
  public Function()
  {
    DotNetEnv.Env.Load();
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
    var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
    .UseNpgsql(connectionString)
    .Options;

    dbContext = new DatabaseContext(contextOptions);
  }

  async public Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
  {
    var method = request.RequestContext.Http.Method;
    var pathParameters = request.PathParameters;

    switch (method)
    {
      case "GET":
        if (pathParameters != null && pathParameters.ContainsKey("movieId") && pathParameters.ContainsKey("userId"))
        {
          return await GetUserMovieRating(int.Parse(pathParameters["movieId"]), pathParameters["userId"]);
        }
        break;

      case "POST":
        return await CreateUserMovieRating(request);
      case "PUT":
        return await UpdateUserMovieRating(request);
      case "DELETE":
        return await DeleteUserMovieRating(request);
      default:
        return new APIGatewayHttpApiV2ProxyResponse
        {
          StatusCode = (int)HttpStatusCode.MethodNotAllowed,
          Body = "Method not allowed"
        };
    }

    return new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.BadRequest,
      Body = "Invalid request"
    };
  }


  private async Task<APIGatewayHttpApiV2ProxyResponse> GetUserMovieRating(int movieId, string userId)
  {
    var rating = await dbContext.UserMovieRatings.FindAsync(userId, movieId);

    if (rating == null)
    {
      return new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.NotFound,
        Body = "User movie rating not found",
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
    }

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.OK,
      Body = System.Text.Json.JsonSerializer.Serialize(rating),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }

  private async Task<APIGatewayHttpApiV2ProxyResponse> CreateUserMovieRating(APIGatewayHttpApiV2ProxyRequest request)
  {
    var rating = System.Text.Json.JsonSerializer.Deserialize<UserMovieRating>(request.Body);

    dbContext.UserMovieRatings.Add(rating);
    await dbContext.SaveChangesAsync();

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.Created,
      Body = System.Text.Json.JsonSerializer.Serialize(rating),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }

  private async Task<APIGatewayHttpApiV2ProxyResponse> UpdateUserMovieRating(APIGatewayHttpApiV2ProxyRequest request)
  {
    var rating = System.Text.Json.JsonSerializer.Deserialize<UserMovieRating>(request.Body);
    dbContext.UserMovieRatings.Update(rating);
    await dbContext.SaveChangesAsync();

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.OK,
      Body = System.Text.Json.JsonSerializer.Serialize(rating),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }

  private async Task<APIGatewayHttpApiV2ProxyResponse> DeleteUserMovieRating(APIGatewayHttpApiV2ProxyRequest request)
  {
    var movieId = int.Parse(request.PathParameters["movieId"]);
    var userId = request.PathParameters["userId"];

    var rating = await dbContext.UserMovieRatings.FindAsync(userId, movieId);
    if (rating == null)
    {
      return new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.NotFound,
        Body = "User movie rating not found",
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
    }

    dbContext.UserMovieRatings.Remove(rating);
    await dbContext.SaveChangesAsync();

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.NoContent,
      Body = "User movie rating deleted",
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }
}