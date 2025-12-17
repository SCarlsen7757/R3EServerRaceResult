# R3E Server Race Result Application

![.NET 10](https://img.shields.io/badge/.NET-10.0-blue?logo=dotnet&logoColor=white) ![Docker Compatible](https://img.shields.io/badge/Docker-Compatible-2496ED?logo=docker&logoColor=white) ![REST API](https://img.shields.io/badge/API-REST-brightgreen) ![Swagger/OpenAPI](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?logo=swagger&logoColor=white) ![License GPLv3](https://img.shields.io/badge/license-GPLv3-blue)

A .NET 10 web application that processes RaceRoom Racing Experience (R3E) race results and generates SimResults.net compatible championship summaries. The application receives race result JSON files from R3E dedicated servers, organizes them into championships, and provides a web interface for viewing results through SimResults.net.

## Features

- **Race Result Upload**: Single and batch upload endpoints for R3E race result JSON files
- **Automatic Championship Grouping**: Flexible strategies for organizing races into championships
- **Monthly Grouping**: Groups races by calendar month
- **Race Count Grouping**: Groups races by a fixed number of races per championship
- **SimResults.net Integration**: Generates compatible summary files for visualization on SimResults.net
- **Configuration Management**: RESTful API for managing championship and event configurations
- **Docker Support**: Includes Docker Compose setup with NGINX for static file serving
- **Health Monitoring**: Built-in health check endpoint
- **Comprehensive Logging**: Configurable logging with environment variable support

## Architecture

The application is built with:
- **ASP.NET Core 10** - Web API framework
- **Swagger/OpenAPI** - API documentation and testing
- **Docker** - Containerization
- **NGINX** - Static file server for race results

### Project Structure

```
R3EServerRaceResultApplication/
├── R3EServerRaceResult/
│   ├── Controllers/
│   │   ├── R3EResultController.cs       # Race result upload endpoints
│   │   └── SimResultController.cs        # Summary config management
│   ├── Models/
│   │   ├── R3EServerResult/              # R3E result models
│   │   ├── SimResult/                    # SimResults.net models
│   │   └── MultipleUploadResult.cs       # Batch upload response
│   ├── Services/
│   │   └── ChampionshipGrouping/         # Championship organization strategies
│   ├── Settings/
│   │   ├── ChampionshipAppSettings.cs
│   │   ├── FileStorageAppSettings.cs
│   │   ├── PointSystem.cs
│   │   └── GroupingStrategyType.cs
│   └── appsettings.json
├── docker-compose.yml
└── README.md
```

## Getting Started

### Prerequisites

- .NET 10 SDK (for local development)
- Docker and Docker Compose (for containerized deployment)

### Configuration

Configuration can be set via `appsettings.json` or environment variables.

#### Championship Settings

```json
{
  "Championship": {
    "WebServer": "http://your-domain.com:8251",
    "EventName": "Championship",
    "EventUrl": "",
    "LogoUrl": "",
    "LeagueName": "R3E <3",
    "LeagueUrl": "",
    "PointSystem": {
      "RacePoints": [25, 18, 15, 12, 10, 8, 6, 4, 2, 1],
      "QualifyingPoints": [3, 2, 1],
      "FastestLapPoints": 1
    }
  }
}
```

#### File Storage Settings

```json
{
  "FileStorage": {
    "MountedVolumePath": "/app/data",
    "ResultFileName": "summary",
    "GroupingStrategy": "Monthly",
    "RacesPerChampionship": 4,
    "ChampionshipStartDate": null
  }
}
```

**Grouping Strategies:**
- `Monthly`: Groups races by calendar month (default)
- `RaceCount`: Groups races by a fixed number per championship (requires `RacesPerChampionship` and optionally `ChampionshipStartDate`)
- `Custom`: Allows advanced grouping by specifying custom championship boundaries, such as explicit start/end dates or other criteria. Requires additional configuration (see `CustomChampionshipGroupingStrategy.cs` and relevant settings).

### Running with Docker Compose

1. Clone the repository
2. Update environment variables in `docker-compose.yml`
3. Run:

```bash
docker-compose up -d
```

The application will be available at:
- **API**: http://localhost:8251
- **Swagger UI**: http://localhost:8251/swagger
- **Static Results**: http://localhost:8252
- **Health Check**: http://localhost:8251/health

### Running Locally

1. Clone the repository
2. Configure `appsettings.json`
3. Run:

```bash
cd R3EServerRaceResult
dotnet run
```

## API Endpoints

### Race Results (`/api/results`)

#### Upload Single Result
```http
POST /api/results
Content-Type: multipart/form-data

file: <R3E result JSON file>
```

#### Upload Multiple Results (Batch)
```http
POST /api/results/batch
Content-Type: multipart/form-data

files: <List of R3E result JSON files>
```

Returns a `MultipleUploadResult` with:
- `TotalReceived`: Number of files received
- `TotalProcessed`: Successfully processed files
- `TotalSkipped`: Skipped (duplicate) files
- `TotalFailed`: Failed files with error details
- File lists: `ProcessedFiles`, `SkippedFiles`, `FailedFiles`

#### Delete Result
```http
DELETE /api/results/{resultPath}
```

### Summary Management (`/api/summaries`)

#### Get Summary URLs
```http
GET /api/summaries/urls
```

Returns list of SimResults.net URLs for all championships.

#### Get Global Config
```http
GET /api/summaries/config?summaryPath={path}
```

#### Update Global Config
```http
PUT /api/summaries/config?summaryPath={path}
Content-Type: application/json

{
  "league": "My League",
  "points": "25,18,15,12,10,8,6,4,2,1",
  ...
}
```

#### Patch Global Config
```http
PATCH /api/summaries/config?summaryPath={path}
Content-Type: application/json

{
  "league": "Updated League Name"
}
```

#### Get Event Config
```http
GET /api/summaries/events/{eventName}/config?summaryPath={path}
```

#### Update Event Config
```http
PUT /api/summaries/events/{eventName}/config?summaryPath={path}
PATCH /api/summaries/events/{eventName}/config?summaryPath={path}
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `LOG_LEVEL` | Logging level (Trace, Debug, Information, Warning, Error, Critical) | `Warning` |
| `LOG_INCLUDE_SCOPES` | Include logging scopes | `true` |
| `LOG_TIMESTAMP_FORMAT` | Timestamp format string | `[yyyy-MM-dd HH:mm:ss]` |
| `LOG_USE_UTC_TIMESTAMP` | Use UTC timestamps | `false` |
| `LOG_SINGLE_LINE` | Single-line log format | `true` |
| `Championship__*` | Championship settings (see Configuration) | - |
| `FileStorage__*` | File storage settings (see Configuration) | - |

## Docker Volumes

The application uses a named volume `data` to persist race results and summaries. Both the API container and NGINX container mount this volume.

## SimResults.net Integration

The application generates JSON files compatible with SimResults.net remote result feature. Access results via:

```
https://simresults.net/remote?results=http://your-domain.com:8252/YYYY/summary.json
```

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE.txt](LICENSE.txt) file for details.

## Development

Built with:
- C# 14.0
- .NET 10
- ASP.NET Core Web API
- Swashbuckle (Swagger/OpenAPI)
- Docker
