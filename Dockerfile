# Use the official .NET SDK image to build and publish the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the entire project and build the application
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Use a runtime image for deployment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose the port the app will run on
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "IncidentReportApi.dll"]
