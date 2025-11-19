# Wheel - Computer Vision Platform
It provides a comprehensive solution for managing datasets, annotating images, training AI models, and evaluating model performance.

## Overview
Wheel enables teams to efficiently manage the complete lifecycle of computer vision projects - from dataset creation and image annotation to model training and evaluation. Built with Blazor Server and .NET 8, it offers a responsive and intuitive user interface for annotation workflows.

## Key Features

### Dataset Management
- Create and organize multiple datasets
- Bulk image upload with drag-and-drop support
- Image preview and metadata management
- Dataset statistics and analytics

### Image Annotation
- Interactive canvas-based annotation tools
- Support for multiple annotation types:
  - Bounding boxes (Object Detection)
  - Polygons (Semantic Segmentation)
- Real-time annotation preview
- Class-based color coding
- Keyboard shortcuts for efficient workflow

### Project Management
- Multiple project types support:
  - Object Detection
  - Semantic Segmentation
  - Classification
- Project-specific class definitions
- Role-based dataset splitting (Train/Val/Test)
- Project workspace for annotation

### Model Training
- Integrated training workflow
- Real-time training progress tracking
- Training status monitoring
- Model versioning and management

### Model Evaluation
- Prediction visualization
- Ground truth vs prediction comparison
- Evaluation metrics and statistics
- Interactive evaluation canvas

## Architecture

WheelApp follows **Clean Architecture** principles with clear separation of concerns:

```
WheelApp/
├── WheelApp.Domain/          # Core business logic and entities
│   ├── Entities/             # Domain entities (Dataset, Project, Annotation, etc.)
│   ├── ValueObjects/         # Immutable value objects
│   ├── Repositories/         # Repository interfaces
│   ├── Services/             # Domain services
│   ├── Specifications/       # Query specifications
│   └── Events/               # Domain events
│
├── WheelApp.Application/     # Application business rules
│   ├── UseCases/             # Use case handlers (CQRS)
│   │   ├── Commands/         # Command handlers (Create, Update, Delete)
│   │   └── Queries/          # Query handlers (Get, List)
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Mappings/             # AutoMapper profiles
│   ├── Behaviors/            # MediatR pipeline behaviors
│   └── Common/               # Shared application logic
│
├── WheelApp.Infrastructure/  # External concerns
│   ├── Persistence/          # EF Core implementation
│   │   ├── Repositories/     # Repository implementations
│   │   ├── Configurations/   # Entity configurations
│   │   └── Interceptors/     # Audit and domain event interceptors
│   ├── Services/             # Infrastructure services
│   └── Storage/              # File storage implementation
│
└── WheelApp/                 # Blazor Server UI
    ├── Pages/                # Blazor pages
    ├── Components/           # Reusable components
    ├── Services/             # UI services and coordinators
    └── wwwroot/              # Static assets
```

## Technology Stack

### Backend
- **.NET 8** - Latest LTS version
- **Entity Framework Core** - ORM for data access
- **SQL Server** - Primary database
- **MediatR** - CQRS and mediator pattern
- **AutoMapper** - Object-to-object mapping
- **FluentValidation** - Input validation
- **Serilog** - Structured logging

### Frontend
- **Blazor Server** - Interactive web UI
- **JavaScript Interop** - Canvas manipulation
- **HTML5 Canvas** - Annotation rendering
- **Bootstrap 5** - UI framework
- **CSS Grid/Flexbox** - Responsive layouts

### Architecture & Patterns
- **Clean Architecture** - Separation of concerns
- **Domain-Driven Design** - Rich domain models
- **CQRS** - Command/Query separation
- **Repository Pattern** - Data access abstraction
- **Specification Pattern** - Reusable queries
- **Unit of Work** - Transaction management

## Database Schema

### Core Entities
- **Dataset** - Container for images
- **Image** - Individual image with metadata
- **Project** - Annotation project with specific type
- **ProjectClass** - Class definitions for projects
- **Annotation** - Image annotations (boxes, polygons, points)
- **Training** - Model training records
- **Evaluation** - Model evaluation results
- **Role** - Dataset split roles (Train/Val/Test)

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server 2019 or later (or SQL Server LocalDB)
- Visual Studio 2022 or VS Code

### Installation

1. Clone the repository
```bash
git clone https://github.com/yourusername/WheelApp.git
cd WheelApp
```

2. Update connection string in `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WheelApp;Trusted_Connection=True;MultipleActiveResultSets=true;"
  }
}
```

3. Apply database migrations
```bash
dotnet ef database update --project WheelApp.Infrastructure --startup-project WheelApp
```

4. Run the application
```bash
cd WheelApp
dotnet run
```

5. Open browser and navigate to `https://localhost:7150`

## Project Structure

### Domain Layer (WheelApp.Domain)
Pure business logic with no external dependencies. Contains:
- Entity models with business rules
- Value objects for type-safety
- Repository contracts
- Domain services
- Specifications for queries
- Domain events

### Application Layer (WheelApp.Application)
Orchestrates domain logic and use cases:
- CQRS commands and queries
- DTOs for data transfer
- Validation rules
- Application services
- AutoMapper configurations

### Infrastructure Layer (WheelApp.Infrastructure)
Implements external concerns:
- EF Core repository implementations
- Database configurations
- File storage services
- External API integrations
- Audit interceptors

### Presentation Layer (WheelApp)
Blazor Server UI:
- Interactive annotation canvas
- Real-time updates
- Responsive design
- Service coordinators
- UI components

## Key Features Implementation

### Annotation Canvas
- HTML5 Canvas with JavaScript interop
- Real-time drawing and editing
- Zoom and pan functionality
- Multi-class support with color coding
- Undo/Redo functionality

### MARS Configuration
Multiple Active Result Sets (MARS) enabled to handle concurrent database queries in Blazor Server environment, preventing DbContext concurrency issues.

### File Upload
- Large file support (up to 1GB)
- Concurrent upload handling
- Progress tracking
- File validation

### Validation
- FluentValidation for input validation
- Domain-level validation rules
- Client-side and server-side validation

## Development Guidelines

### Clean Architecture Principles
1. **Dependency Rule**: Dependencies point inward (Domain ← Application ← Infrastructure/UI)
2. **Separation of Concerns**: Each layer has distinct responsibilities
3. **Testability**: Core logic isolated from frameworks
4. **Independence**: Business logic independent of UI and database

### Code Organization
- Use feature-based organization in Application layer
- Follow CQRS pattern for all operations
- Implement specification pattern for complex queries
- Use value objects for type-safety
- Emit domain events for cross-aggregate communication

### Best Practices
- Always use repository abstractions
- Implement validation in Application layer
- Keep domain logic in entities
- Use AutoMapper for DTO mapping
- Log using Serilog with structured logging
- Handle exceptions with Result pattern

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### Third-Party Dependencies

| Library | Version | License |
|---------|---------|---------|
| .NET | 8.0 | MIT |
| Entity Framework Core | 9.0 | MIT |
| MediatR | 12.4 | MIT |
| AutoMapper | 13.0 | RPL-1.5 / Community |
| FluentValidation | 11.11 | Apache 2.0 |
| Serilog | Latest | Apache 2.0 |
| Bootstrap | 5.x | MIT |

**Note:** AutoMapper 13.0+ uses a dual-license model. This project qualifies for the free Community License.

## Support

For issues and questions, please create an issue in the GitHub repository.
