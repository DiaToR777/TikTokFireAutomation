FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["TikTokFireAutomation/TikTokFireAutomation.csproj", "TikTokFireAutomation/"]
RUN dotnet restore "TikTokFireAutomation/TikTokFireAutomation.csproj"

COPY . .
WORKDIR "/src/TikTokFireAutomation"

RUN dotnet publish "./TikTokFireAutomation.csproj" \
    -c $BUILD_CONFIGURATION \
    -r linux-x64 \
    --self-contained true \
    -o /app/publish \
    /p:UseAppHost=true

FROM mcr.microsoft.com/playwright/dotnet:v1.61.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/logs /app/user_profile

ENTRYPOINT ["./TikTokFireAutomation"]
