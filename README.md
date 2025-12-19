# R3E Server Race Result Application

![.NET 10](https://img.shields.io/badge/.NET-10.0-blue?logo=dotnet&logoColor=white) ![Docker Compatible](https://img.shields.io/badge/Docker-Compatible-2496ED?logo=docker&logoColor=white) ![REST API](https://img.shields.io/badge/API-REST-brightgreen) ![Swagger/OpenAPI](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?logo=swagger&logoColor=white) ![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?logo=sqlite&logoColor=white) ![License GPLv3](https://img.shields.io/badge/license-GPLv3-blue)

A .NET 10 web application that processes RaceRoom Racing Experience (R3E) race results and generates SimResults.net compatible championship summaries. The application receives race result JSON files from R3E dedicated servers, organizes them into championships, and provides a web interface for viewing results through SimResults.net.

## Features

- **Race Result Upload**: Single and batch upload endpoints for R3E race result JSON files
- **Automatic Championship Grouping**: Flexible strategies for organizing races into championships
  - **Monthly Grouping**: Groups races by calendar month
  - **Race Count Grouping**: Groups races by a fixed number of races per championship (uses SQLite database for race counting)
  - **Custom Grouping**: Define championship periods with explicit start/end dates stored in SQLite database
- **Championship Management**: RESTful API for creating, updating, and managing custom championship configurations
- **SQLite Database**: Persistent storage for championship configurations and race count tracking with Entity Framework Core
- **SimResults.net Integration**: Generates compatible summary files for visualization on SimResults.net
- **Docker Support**: Includes Docker Compose setup with NGINX for static file serving
- **Health Monitoring**: Built-in health check endpoint
- **Comprehensive Logging**: Configurable logging with environment variable support

## Architecture

The application is built with:
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM with SQLite provider
- **SQLite** - Lightweight database for championship configurations and race counting
- **Swagger/OpenAPI** - API documentation and testing
- **Docker** - Containerization
- **NGINX** - Static file server for race results

### Project Structure

```
R3EServerRaceResultApplication/
├── R3EServerRaceResult/
│   ├── Controllers/
│   │   ├── R3EResultController.cs       # Race result upload endpoints
│   │   ├── ChampionshipController.cs    # Championship configuration API
│   │   └── SimResultController.cs        # Summary config management
│   ├── Data/
│   │   ├── ChampionshipDbContext.cs     # EF Core DbContext
│   │   └── Repositories/                 # Repository pattern for data access
│   │       ├── IChampionshipRepository.cs
│   │       ├── ChampionshipRepository.cs
│   │       ├── IRaceCountRepository.cs
│   │       └── RaceCountRepository.cs
│   ├── Models/
│   │   ├── ChampionshipConfiguration.cs  # Championship entity
│   │   ├── RaceCountState.cs            # Race count tracking entity
│   │   ├── ChampionshipConfigurationDto.cs # API DTOs
│   │   ├── R3EServerResult/              # R3E result models
│   │   ├── SimResult/                    # SimResults.net models
│   │   └── MultipleUploadResult.cs       # Batch upload response
│   ├── Services/
│   │   ├── ChampionshipConfigurationStore.cs # Championship service layer
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
    "DatabaseConnectionString": "Data Source=/app/data/championships.db"
  }
}
```

**Grouping Strategies:**
- `Monthly`: Groups races by calendar month (default)
- `RaceCount`: Groups races by a fixed number per championship. Race counts and configuration are tracked in SQLite database. Requires `RacesPerChampionship`.
  - **Configuration Changes**: If `RacesPerChampionship` changes on application restart, the race counter is automatically reset to 0 for the affected year(s), starting a new championship sequence. Existing races are not reassigned.
  - **Manual Reset**: Use the REST API endpoint `/api/championships/racecount/reset` to manually start a new championship at any time without restarting the application.
- `Custom`: Allows advanced grouping to specify custom championship boundaries with explicit start/end dates. Championship configurations are stored in SQLite database and managed via REST API.

**RaceCount Strategy Behavior:**

The RaceCount strategy provides predictable championship grouping with built-in configuration validation:

1. **Normal Operation**: Every N races (where N = `RacesPerChampionship`) creates a new championship
   - Example with `RacesPerChampionship=4`: Races 1-4 → Champ 1, Races 5-8 → Champ 2, etc.

2. **Year Boundary**: Each year maintains its own race counter, automatically starting fresh
   - 2025: Races 1-8 → Champ 1-2
   - 2026: Races 1-4 → Champ 1 (new year, new counter)

3. **Configuration Change Detection**: When application starts, configuration is validated against database
   - If `RacesPerChampionship` changes (e.g., 4 → 3), counter resets to 0
   - Logged as WARNING with details about old vs new configuration
   - Existing race results remain in their original championships

4. **Manual Championship Reset**: Use the API to start a new championship at any time
   - POST `/api/championships/racecount/reset` with optional reason
   - Resets counter to 0, next race starts Championship 1
   - Useful for starting mid-season championships or special events
   - Existing races before reset remain in their championships

**Database Configuration:**
- `DatabaseConnectionString`: SQLite connection string for data storage. Required for `RaceCount` and `Custom` grouping strategies.

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

### Championship Configuration (`/api/championships`)

#### Get Current Grouping Strategy
```http
GET /api/championships/strategy
```

Returns the currently active grouping strategy type.

#### Custom Strategy Endpoints

##### Get All Championships
```http
GET /api/championships/configurations?includeExpired=true
```

Returns list of all championship configurations. Set `includeExpired=false` to exclude expired championships.

##### Get Championship by ID
```http
GET /api/championships/configurations/{id}
```

Returns a specific championship configuration by its ID.

##### Create Championship
```http
POST /api/championships/configurations
Content-Type: application/json

{
  "name": "Summer Championship 2025",
  "startDate": "2025-06-01",
  "endDate": "2025-08-31"
}
```

Creates a new championship configuration. The system automatically:
- Generates a unique ID
- Sets creation timestamp
- Computes `isActive` based on current date vs start/end dates
- Validates date ranges and checks for overlaps

##### Update Championship
```http
PUT /api/championships/configurations/{id}
Content-Type: application/json

{
  "name": "Updated Summer Championship 2025",
  "startDate": "2025-06-01",
  "endDate": "2025-09-30"
}
```

Updates an existing championship configuration. The `isActive` property is computed automatically and cannot be manually set.

##### Delete Championship
```http
DELETE /api/championships/configurations/{id}
```

Deletes a championship configuration. Returns 204 No Content on success.

**Championship Response Model:**
```json
{
  "id": "abc-123-def-456",
  "name": "Summer Championship 2025",
  "startDate": "2025-06-01",
  "endDate": "2025-08-31",
  "isActive": true,
  "createdAt": "2025-01-19T10:30:00Z",
  "isExpired": false
}
```

**Computed Properties:**
- `isActive`: Automatically true if current date is between `startDate` and `endDate`
- `isExpired`: Automatically true if current date is after `endDate`

#### RaceCount Strategy Endpoints

##### Get All Race Count States
```http
GET /api/championships/racecount
```

Returns race count states for all years in the database.

**Response:**
```json
{
  "year": 2025,
  "raceCount": 7,
  "racesPerChampionship": 4,
  "currentChampionship": "2025-C02",
  "nextRaceNumber": 4,
  "lastUpdated": "2025-01-19T20:30:00Z"
}
```

##### Get Race Count State for Specific Year
```http
GET /api/championships/racecount/{year}
```

Returns the current race count state for a specific year.

**Response:**
```json
{
  "year": 2025,
  "raceCount": 7,
  "racesPerChampionship": 4,
  "currentChampionship": "2025-C02",
  "nextRaceNumber": 4,
  "lastUpdated": "2025-01-19T20:30:00Z"
}
```

- `raceCount`: Total races processed this year
- `currentChampionship`: Current championship identifier
- `nextRaceNumber`: The race number that will be assigned to the next race
- `racesPerChampionship`: Configuration value

##### Reset Race Counter
```http
POST /api/championships/racecount/reset
Content-Type: application/json

{
  "year": 2025,
  "reason": "Starting summer championship season"
}
```

Resets the race counter for a specific year to start a new championship immediately.

**Request Parameters:**
- `year` (optional): Year to reset (defaults to current year)
- `reason` (optional): Reason for reset (logged for audit trail)

**Response:**
```json
{
  "year": 2025,
  "previousCount": 7,
  "newCount": 0,
  "previousChampionship": "2025-C02",
  "nextChampionship": "2025-C01",
  "message": "Race counter reset for year 2025. Next race will start Championship 1."
}
```

**Use Cases:**
- Starting a mid-season championship
- Beginning a new special event series
- Resetting after configuration testing
- Manually synchronizing with external systems

**Important:** Existing race results before the reset remain in their original championships and folders. Only new races after the reset will be assigned to the new championship sequence.

### Summary Management (`/api/summaries`)

#### Get Summary URLs
```http
GET /api/summaries/urls?year={year}&strategy={strategy}
```

Returns list of SimResults.net URLs for all championships.

**Query Parameters:**
- `year` (optional): Filter by year (e.g., `2025`)
- `strategy` (optional): Filter by grouping strategy (`Monthly`, `RaceCount`, or `Custom`)

**Examples:**
```http
GET /api/summaries/urls
GET /api/summaries/urls?year=2025
GET /api/summaries/urls?strategy=RaceCount
GET /api/summaries/urls?year=2025&strategy=Custom
```

**Response:**
```json
[
  "simresults.net/remote?results=http://your-domain.com:8252/2025/champ1/summary.json",
  "simresults.net/remote?results=http://your-domain.com:8252/2025/champ2/summary.json"
]
```

#### Get Summary File Paths
```http
GET /api/summaries/paths?year={year}&strategy={strategy}
```

Returns list of local file paths for summary files. Use these paths with the config endpoints below.

**Query Parameters:**
- `year` (optional): Filter by year (e.g., `2025`)
- `strategy` (optional): Filter by grouping strategy (`Monthly`, `RaceCount`, or `Custom`)

**Examples:**
```http
GET /api/summaries/paths
GET /api/summaries/paths?year=2025
GET /api/summaries/paths?strategy=RaceCount
```

**Response:**
```json
[
  "2025/champ1/summary.json",
  "2025/champ2/summary.json",
  "2024/12/summary.json"
]
```

**Note:** Summary files are automatically indexed in the SQLite database for fast queries. The index includes:
- File path
- Championship key
- Strategy type
- Year
- Race count
- Creation and update timestamps

**Usage with Config Endpoints:**
```http
# 1. Get file paths
GET /api/summaries/paths?year=2025

# 2. Use path with config endpoints
GET /api/summaries/config?summaryPath=2025/champ1/summary.json
PUT /api/summaries/config?summaryPath=2025/champ1/summary.json
```

## Database Schema Changes

- Removed `ChampionshipStartDate` column from championship tables
- Updated SQL scripts and application code to reflect schema changes

### Updated Tables

- `ChampionshipConfigurations`
  - Removed `ChampionshipStartDate` column

- `RaceCountStates`
  - No change

- `SummaryFiles`
  - Added for indexing summary file metadata

### SQL Script Example

```sql
-- Championship configurations
CREATE TABLE ChampionshipConfigurations (
    Id TEXT PRIMARY KEY,                       -- Unique identifier
    Name TEXT NOT NULL,                       -- Championship name
    StartDate DATETIME NOT NULL,              -- Scheduled start date
    EndDate DATETIME NOT NULL,                -- Scheduled end date
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Creation timestamp
    IsActive BOOLEAN NOT NULL DEFAULT 0,       -- Active status
    -- Removed ChampionshipStartDate column
);

-- Race count tracking (RaceCount strategy)
CREATE TABLE RaceCountStates (
    Year INTEGER PRIMARY KEY,               -- e.g., 2025, 2026
    RaceCount INTEGER NOT NULL,             -- Number of races processed
    RacesPerChampionship INTEGER NOT NULL,  -- Configuration: races per championship
    LastUpdated DATETIME NOT NULL           -- Tracking timestamp
);

-- Summary file index (all strategies)
CREATE TABLE SummaryFiles (
    Id TEXT(36) PRIMARY KEY,
    FilePath TEXT(500) NOT NULL UNIQUE,
    ChampionshipKey TEXT(50) NOT NULL,
    ChampionshipName TEXT(200),
    Strategy TEXT(20) NOT NULL,            -- Monthly, RaceCount, or Custom
    Year INTEGER NOT NULL,
    RaceCount INTEGER NOT NULL,             -- Number of race events in championship
    CreatedAt DATETIME NOT NULL,
    LastUpdated DATETIME NOT NULL
);

-- Indexes for efficient date range queries
CREATE INDEX IX_ChampionshipConfigurations_StartDate ON ChampionshipConfigurations (StartDate);
CREATE INDEX IX_ChampionshipConfigurations_EndDate ON ChampionshipConfigurations (EndDate);
CREATE INDEX IX_ChampionshipConfigurations_StartDate_EndDate ON ChampionshipConfigurations (StartDate, EndDate);

-- Indexes for efficient summary queries
CREATE UNIQUE INDEX IX_SummaryFiles_FilePath ON SummaryFiles (FilePath);
CREATE INDEX IX_SummaryFiles_Year ON SummaryFiles (Year);
CREATE INDEX IX_SummaryFiles_Strategy ON SummaryFiles (Strategy);
CREATE INDEX IX_SummaryFiles_ChampionshipKey ON SummaryFiles (ChampionshipKey);
CREATE INDEX IX_SummaryFiles_CreatedAt ON SummaryFiles (CreatedAt);
CREATE INDEX IX_SummaryFiles_RaceCount ON SummaryFiles (RaceCount);

```

### Data Persistence

The application persists data in multiple ways:

1. **Race Results & Summaries**: Stored as JSON files in the mounted volume (`/app/data`)
2. **Championship Configurations** (Custom strategy): Stored in SQLite database (`/app/data/championships.db`)
3. **Race Count Tracking** (RaceCount strategy): Stored in SQLite database (`/app/data/championships.db`)
4. **Summary File Index** (all strategies): Stored in SQLite database (`/app/data/championships.db`)
   - Automatically indexes summary files when created via API
   - Tracks championship metadata (year, strategy, race count)
   - Only summaries uploaded through the API are indexed

All data is persisted in the Docker volume, ensuring it survives container restarts.
