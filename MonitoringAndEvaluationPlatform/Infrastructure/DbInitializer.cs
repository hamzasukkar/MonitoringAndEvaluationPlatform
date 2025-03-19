using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Infrastructure
{
    public static class DbInitializer
    {
        public static void Seed(IServiceProvider serviceProvider)
        {
            // Create a scope to manage the context's lifetime
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (!context.Sectors.Any())
                {
                    var sectors = new List<Sector>
                    {
                        new Sector { Partner = "Sector 1" }, // Remove explicit Code if using auto-increment
                        new Sector { Partner = "Sector 2" },
                        new Sector { Partner = "Sector 3" }
                    };
                    context.Sectors.AddRange(sectors);
                }

                if (!context.Donors.Any())
                {
                    var donors = new List<Donor>
                    {
                        new Donor { Partner = "Donor 1" }, // Remove explicit Code if using auto-increment
                        new Donor { Partner = "Donor 2" },
                        new Donor { Partner = "Donor 3" }
                    };
                    context.Donors.AddRange(donors);
                }

                if (!context.Regions.Any())
                {
                    var regions = new List<Region>
                    {
                        new Region { Name = "Region 1" }, // Remove explicit Code if using auto-increment
                        new Region { Name = "Region 2" },
                        new Region { Name = "Region 3" }
                    };

                    context.Regions.AddRange(regions);
                }
                if (!context.SuperVisors.Any())
                {
                    var superVisors = new List<SuperVisor>
                    {
                        new SuperVisor { Name = "SuperVisor 1" }, // Remove explicit Code if using auto-increment
                        new SuperVisor { Name = "SuperVisor 2" },
                        new SuperVisor { Name = "SuperVisor 3" }
                    };

                    context.SuperVisors.AddRange(superVisors);
                }

                if (!context.ProjectManagers.Any())
                {
                    var projectManagers = new List<ProjectManager>
                    {
                        new ProjectManager { Name = "ProjectManager 1" }, // Remove explicit Code if using auto-increment
                        new ProjectManager { Name = "ProjectManager 2" },
                        new ProjectManager { Name = "ProjectManager 3" }
                    };

                    context.ProjectManagers.AddRange(projectManagers);
                }

                if (!context.Ministry.Any())
                {
                    var ministries = new List<Ministry>
                    {
                        new Ministry { MinistryName = "Ministry 1" }, // Remove explicit Code if using auto-increment
                        new Ministry { MinistryName = "Ministry 2" },
                        new Ministry { MinistryName = "Ministry 3" }
                    };

                    context.Ministry.AddRange(ministries);
                }

                context.SaveChanges();

            } // Context is disposed here when the scope ends
        }
    }
}
