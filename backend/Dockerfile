# ===========================
# Build stage
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
# Restore + publish
RUN dotnet restore "OOOControlSystem.csproj"  && dotnet publish "OOOControlSystem.csproj" -c Release -o /out

# ===========================
# Runtime stage
# ===========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /out .

# Ensure upload directory exists
RUN mkdir -p /app/wwwroot/uploads/defects

# Expose and run
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OOOControlSystem.dll"]
