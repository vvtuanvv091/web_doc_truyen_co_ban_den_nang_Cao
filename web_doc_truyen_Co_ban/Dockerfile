FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY web_doc_truyen_Co_ban/*.csproj web_doc_truyen_Co_ban/
RUN dotnet restore web_doc_truyen_Co_ban/web_doc_truyen_Co_ban.csproj
COPY . .
RUN dotnet publish web_doc_truyen_Co_ban/web_doc_truyen_Co_ban.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "web_doc_truyen_Co_ban.dll"]
