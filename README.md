# Round-Batman

## Requirements

- .NET 10 SDK
- Python 3, if you want to run the web client

## Run the API

From the repository root, run:

```bash
dotnet restore
dotnet run --project src/RoundBatman.Api/RoundBatman.Api.csproj --launch-profile http
```

The API will be available at `http://localhost:5227`.

To check that it is running, open `http://localhost:5227/health`.

## Run the web client

With the API running, open another terminal and run:

```bash
cd client
python3 -m http.server 5500
```

Open `http://localhost:5500` in your browser. The client is configured to use
the API at `http://localhost:5227`.

## Run the tests

From the repository root, run:

```bash
dotnet test RoundBatman.slnx
```
