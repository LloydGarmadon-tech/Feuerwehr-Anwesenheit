FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish src/FireDepartmentMvp/FireDepartmentMvp.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:5000
ENV ConnectionStrings__Default="Data Source=/app/data/firedepartment.db"

EXPOSE 5000

ENTRYPOINT ["dotnet", "FireDepartmentMvp.dll"]
