# Usa l'SDK .NET per buildare l'app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia csproj contentente i pacchetti e li scarica con dotnet restore(nella cache di Docker)
COPY *.csproj ./
RUN dotnet restore

# Copia il resto e pubblica
COPY . ./
RUN dotnet publish WebAppEF.csproj -c Release -o out

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "WebAppEF.dll"] 
# quando si avvia, esegue dotnet WebAppEF.dll
