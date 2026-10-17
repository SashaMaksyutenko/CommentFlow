# The API image. Built from the repository root:
#   docker build -f src/Comments.Api/Dockerfile .

# 1. Build in the SDK image: it has the compiler and NuGet
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Project files first: Docker caches the restore step and repeats it only when dependencies change
COPY src/Comments.Domain/Comments.Domain.csproj src/Comments.Domain/
COPY src/Comments.Application/Comments.Application.csproj src/Comments.Application/
COPY src/Comments.Infrastructure/Comments.Infrastructure.csproj src/Comments.Infrastructure/
COPY src/Comments.Api/Comments.Api.csproj src/Comments.Api/
RUN dotnet restore src/Comments.Api/Comments.Api.csproj

COPY src/ src/
RUN dotnet publish src/Comments.Api/Comments.Api.csproj --configuration Release --output /app --no-restore

# 2. Run in the much smaller ASP.NET Core runtime image: no SDK and no source code inside
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# The folder for uploaded files belongs to the "app" user of the image,
# so the API can write there without running as root
RUN mkdir /app/uploads && chown $APP_UID /app/uploads
USER $APP_UID

# The image listens on port 8080 inside the container
EXPOSE 8080
ENTRYPOINT ["dotnet", "Comments.Api.dll"]
