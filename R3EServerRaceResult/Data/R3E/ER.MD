R3E Content
```mermaid
erDiagram
    Car ||--|{ Livery : "One or more liveries"
    Manufacturer ||--|{ Car : "One or more cars"
    CarClass ||--|{ Car : "One or more cars"
    Track ||--|{ Layout : "One or more layouts"

    Livery {
        int id
        string name
    }
    Car {
        int id
        string name
        Livery liveries
    }
    Manufacturer {
        int id
        string name
        string country
        Car car
    }
    CarClass {
        int id
        string name
        Car car
    }

    Layout {
        int id
        string name
        int maxVehicles
    }
    Track {
        int id
        string name
        string location
    }
```

Race stats
```mermaid
erDiagram

    Driver ||--|{ Result : "One or more results"
    Driver ||--|{ Laps : "One or more laps"
    Driver ||--|{ Incident : "One or more incidents"
    Event ||--|{ Session : "One or more sessions"
    Session ||--|{ Result : "One or more results"
    Session ||--|{ Laps : "One or more laps"
    Session ||--|{ Incident : "One or more incidents"
    Car ||--|{ Result : "Used in one or more results"
    Track ||--|{ Event : "Host one or more events"
    Layout ||--|{ Event : "Used in one or more events"
    Driver ||--|{ Incident : "Involved in incidents" 

    Driver {
        int id
        string name
    }
    Event {
        int id
        int trackId FK
        int layoutId FK
        date eventDate
    }
    Session {
        int id
        int eventId FK
        string sessionType
    }
    Result {
        int id
        int driverId FK
        int sessionId FK
        int carId FK
        int startPosition
        int position
        int classStartPosition
        int classPosition
        time totalRaceTime
        string finishStatus
        int totalLaps 
    }
    Laps {
        int id
        int driverId FK
        int sessionId FK
        time lapTime
        time sector1Time
        time sector2Time
        time sector3Time
        bool isValid
    }
    Incident {
        int id
        int driverId FK
        int sessionId FK  
        string incidentType
        int incidentPoints
        int lapNumber
        int involvedDriverId FK 
    }