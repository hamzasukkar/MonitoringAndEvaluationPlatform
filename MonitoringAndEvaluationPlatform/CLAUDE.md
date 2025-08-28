# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build and Run
- `dotnet run` - Run the application in development mode (https://localhost:7173)
- `dotnet build` - Build the project
- `dotnet publish` - Publish for deployment

### Database Management
- `dotnet ef database update` - Apply pending migrations to the database
- `dotnet ef migrations add <MigrationName>` - Create a new database migration
- `dotnet ef database drop` - Drop the database (use with caution)

### Configuration
- Update connection string in `appsettings.json` for your SQL Server instance
- Default admin credentials: admin@example.com / Admin@123

## Architecture Overview

This is an ASP.NET Core 8 MVC web application for monitoring and evaluating development projects, with a hierarchical performance tracking system.

### Core Domain Model
The application follows a hierarchical structure for monitoring and evaluation:

**Framework → Outcome → Output → SubOutput → Indicator → Project → ActionPlan → Activity → Plan**

Key entities:
- **Framework**: Top-level monitoring framework (e.g., SDGs)
- **Outcome/Output/SubOutput**: Hierarchical results structure
- **Indicator**: Measurable performance metrics with weights
- **Project**: Implementation units with location, sector, donor, ministry associations
- **Measure**: Links projects to indicators with planned/realized values
- **Plan**: Detailed activity tracking (Financial, Physical, DisbursementPerformance, FieldMonitoring, ImpactAssessment)

### Services Architecture
- **PlanService**: Handles plan updates and cascades performance calculations up the hierarchy
- **MonitoringService**: Manages monitoring data and calculations
- **PerformanceService**: Aggregates performance metrics across the hierarchy
- **ActivityService**: Manages activity data and relationships

### Performance Calculation System
Performance metrics flow upward through the hierarchy:
1. Plans are updated with planned vs realized values
2. Project performance is calculated from activity plans by type
3. Performance propagates up: Project → Indicator → SubOutput → Output → Outcome → Framework
4. Currently uses direct propagation (simplified); designed for weighted averages

### Data Context
`ApplicationDbContext` manages:
- Identity (users, roles, authentication)
- Core entities with complex many-to-many relationships
- Location hierarchy (Governorate → District → SubDistrict → Community)
- Localization support (Arabic/English/French)

### Localization
- Supports Arabic (default), English, and French
- Resource files in `Resources/` directory
- Uses suffix-based view localization

### Key Patterns
- Entity Framework Core with SQL Server
- ASP.NET Core Identity for authentication
- Razor Views with multiple layout types (Dashboard, Monitoring, Projects, etc.)
- Service layer pattern with dependency injection
- Seed data initialization on startup