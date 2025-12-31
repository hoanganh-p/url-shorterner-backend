# UrlShortener.Api

Backend for the URL Shortener service.

Quick run (development):

- Backend (dotnet):

```powershell
cd UrlShortener.Api
dotnet restore
dotnet run --project UrlShortener/UrlShortener.csproj
```

<!-- - Frontend (React):

```powershell
cd ..\..\reactjs\url-shortener-frontend
npm install
npm start
``` -->

Tests:

```powershell
cd UrlShortener.Api
dotnet test UrlShortener.Tests/UrlShortener.Tests.csproj
```

Notes:
- Keep secrets out of appsettings.json; use user-secrets or environment variables.
- Consider adding Docker and CI workflows (there is a sample in .github/workflows).
