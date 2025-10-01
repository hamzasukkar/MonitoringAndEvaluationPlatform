# GEMINI.md - Monitoring and Evaluation Platform

## Project Overview

This project is a web application for monitoring and evaluating the performance of various entities such as frameworks, projects, and indicators. It is built with ASP.NET Core 8 and Entity Framework Core, using a SQL Server database. The application features a dashboard for visualizing performance data and supports localization for English, Arabic, and French.

### Key Technologies

*   **Backend:** ASP.NET Core 8, C#
*   **Database:** SQL Server, Entity Framework Core
*   **Frontend:** ASP.NET Core MVC (Razor Views), likely with JavaScript for dynamic UI updates.
*   **Authentication:** ASP.NET Core Identity

### Architecture

The application follows a standard ASP.NET Core MVC architecture:

*   **Models:** Define the data structures for entities like `Framework`, `Project`, `Indicator`, etc.
*   **Views:** Render the user interface using Razor templates.
*   **Controllers:** Handle user requests, interact with services, and return views or data.
*   **Services:** Contain the business logic for performance calculations and data manipulation.
*   **Data:** Includes the `ApplicationDbContext` for database interactions.

## Building and Running

### Prerequisites

*   .NET 8 SDK
*   SQL Server (or SQL Server Express)

### Configuration

1.  **Database Connection:** The connection string is located in `appsettings.json`. Update the `DefaultConnection` string to point to your SQL Server instance.

    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=.\\sqlexpress;Database=MRE3;User Id=sa;Password=123;MultipleActiveResultSets=true;TrustServerCertificate=True"
      }
    }
    ```

2.  **Database Migrations:** The project uses Entity Framework Core migrations to manage the database schema. To apply the migrations, run the following command in the project's root directory:

    ```bash
    dotnet ef database update
    ```

### Running the Application

To run the application, use the following command:

```bash
dotnet run
```

The application will be available at `https://localhost:5001` (or a similar port).

## Development Conventions

### Coding Style

The codebase follows standard C# and ASP.NET Core conventions.

### Testing

There are no dedicated test projects in the provided file structure.

### Contribution Guidelines

TODO: Add contribution guidelines if applicable.
