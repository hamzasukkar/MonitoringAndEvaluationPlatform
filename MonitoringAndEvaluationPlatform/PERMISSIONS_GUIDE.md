# Monitoring and Evaluation Platform - Permissions System

## Overview
This document describes the role-based permission system implemented for the Monitoring and Evaluation Platform based on the requirements in Roles.pdf.

## User Roles

### 1. System Administrator (`SystemAdministrator`)
- **Full access** to all system features
- Can manage strategies, policies, programs, and subprograms
- Can create and modify ministries
- Can view all reports and analytics
- **Login**: admin@example.com / Admin@123

### 2. Ministries User (`MinistriesUser`)
- **Read-only access** to strategies, policies, programs, and projects
- Can view reports and performance analytics
- Can export reports and analyze indicators
- Cannot create or modify strategic elements
- **Login**: ministry@example.com / Ministry@123

### 3. Data Entry (`DataEntry`)
- **Read access** to strategies, policies, programs, and projects
- **Full control** over project management (add, edit, delete projects)
- **Full control** over project forms and metrics
- Can modify action plans and project data
- Cannot modify strategic frameworks
- **Login**: dataentry@example.com / DataEntry@123

### 4. Reading User (`ReadingUser`)
- **View-only access** to most system features
- Can view control panels and basic reports
- Cannot modify any data
- Limited reporting capabilities
- **Login**: reader@example.com / Reader@123

## Permission Matrix by Interface

### Interface 1 - Login (All Users)
- ✅ Login
- ✅ Password Recovery

### Interface 2 - Strategy Management
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Read Strategies | ✅ | ✅ | ✅ | ✅ |
| Add Strategy | ✅ | ❌ | ❌ | ❌ |
| Modify Strategy | ✅ | ❌ | ❌ | ❌ |
| Delete Strategy | ✅ | ❌ | ❌ | ❌ |

### Interface 3 - Policy Management
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Read Policies | ✅ | ✅ | ✅ | ✅ |
| Add Policy | ✅ | ❌ | ❌ | ❌ |
| Modify Policy | ✅ | ❌ | ❌ | ❌ |
| Delete Policy | ✅ | ❌ | ❌ | ❌ |

### Interface 4 - Program Management
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Read Programs | ✅ | ✅ | ✅ | ✅ |
| Add Program | ✅ | ❌ | ❌ | ❌ |
| Edit Program | ✅ | ❌ | ❌ | ❌ |
| Delete Program | ✅ | ❌ | ❌ | ❌ |

### Interface 5 - Subprogram Management
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Read Subprograms | ✅ | ✅ | ✅ | ✅ |
| Add Subprogram | ✅ | ❌ | ❌ | ❌ |
| Edit Subprogram | ✅ | ❌ | ❌ | ❌ |
| Delete Subprogram | ✅ | ❌ | ❌ | ❌ |

### Interface 6 - Project Management
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Read Projects | ✅ | ✅ | ✅ | ❌ |
| Add Project | ❌ | ❌ | ✅ | ❌ |
| Edit Project | ❌ | ❌ | ✅ | ❌ |
| Delete Project | ❌ | ❌ | ✅ | ❌ |

### Interface 7 - Project Forms
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Read Forms | ✅ | ✅ | ✅ | ❌ |
| Fill Form | ❌ | ❌ | ✅ | ❌ |
| Edit Form | ❌ | ❌ | ✅ | ❌ |
| Delete Form | ❌ | ❌ | ✅ | ❌ |

### Interface 8 - Project Metrics
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Read Metrics | ✅ | ✅ | ✅ | ❌ |
| Add Values | ❌ | ❌ | ✅ | ❌ |
| Edit Values | ❌ | ❌ | ✅ | ❌ |
| Delete Values | ❌ | ❌ | ✅ | ❌ |

### Interface 9 - Action Plans
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Read Plans | ✅ | ✅ | ✅ | ❌ |
| Modify Status | ❌ | ❌ | ✅ | ❌ |
| Delete Plan | ❌ | ❌ | ✅ | ❌ |

### Interface 10 - General Control Panel
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| View Control Panel | ✅ | ✅ | ✅ | ✅ |

### Interface 11 - Ministries Management
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Read Ministries | ✅ | ✅ | ✅ | ✅ |
| Create Ministry | ✅ | ❌ | ❌ | ❌ |
| Modify Ministry | ✅ | ❌ | ❌ | ❌ |
| Delete Ministry | ✅ | ❌ | ❌ | ❌ |
| Display Indicators | ✅ | ❌ | ❌ | ❌ |

### Interface 12 - Project Dashboard
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Browse Projects | ✅ | ✅ | ✅ | ✅ |
| Monitor Performance | ✅ | ✅ | ❌ | ❌ |
| Classify Projects | ✅ | ✅ | ✅ | ❌ |

### Interface 13 - Strategic Indicators Dashboard
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| Display Indicators | ✅ | ✅ | ✅ | ✅ |
| Compare Performance | ✅ | ✅ | ✅ | ❌ |
| Edit Strategy Data | ✅ | ❌ | ❌ | ❌ |

### Interface 15 - Comprehensive Performance Reporting
| Permission | System Admin | Ministries User | Data Entry | Reading User |
|------------|--------------|-----------------|------------|--------------|
| View Reports | ✅ | ✅ | ❌ | ✅ |
| Analyze Performance | ✅ | ✅ | ❌ | ❌ |
| Export Reports | ✅ | ✅ | ❌ | ❌ |

## Implementation Details

### Files Created/Modified

1. **Models/UserRoles.cs** - Role constants
2. **Models/Permissions.cs** - Permission constants
3. **Infrastructure/PermissionAuthorizationHandler.cs** - Authorization logic
4. **Attributes/PermissionAttribute.cs** - Controller decoration
5. **Program.cs** - Updated to register policies and create users
6. **Controllers/** - Updated with permission attributes

### Usage in Controllers

```csharp
[Authorize]
[Permission(Permissions.ReadStrategies)]
public async Task<IActionResult> Index()
{
    // Only users with ReadStrategies permission can access
}

[Permission(Permissions.AddStrategy)]
public IActionResult Create()
{
    // Only System Administrators can access
}
```

### Testing the System

1. **Build the application**: `dotnet build`
2. **Run the application**: `dotnet run`
3. **Test different user roles**:
   - Login with different credentials
   - Try accessing different features
   - Verify permission restrictions

### Default Users Created

The system automatically creates these users on startup:

| Role | Username | Email | Password | Ministry |
|------|----------|-------|----------|----------|
| System Administrator | admin | admin@example.com | Admin@123 | System Administration |
| Ministries User | ministry_user | ministry@example.com | Ministry@123 | Ministry of Planning |
| Data Entry | data_entry | dataentry@example.com | DataEntry@123 | Data Entry Department |
| Reading User | reading_user | reader@example.com | Reader@123 | External Observer |

### Security Features

1. **Role-based access control** - Users can only access features their role permits
2. **Attribute-based authorization** - Controllers decorated with permission attributes
3. **Automatic policy registration** - All permissions automatically registered as policies
4. **Secure user seeding** - Default users created with strong passwords
5. **Access denied handling** - Unauthorized users redirected to access denied page

This implementation provides a comprehensive, secure, and maintainable permission system that matches the requirements specified in the Roles.pdf document.