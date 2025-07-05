using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
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
                        new Sector { Name = "Sector 1" }, // Remove explicit Code if using auto-increment
                        new Sector { Name = "Sector 2" },
                        new Sector { Name = "Sector 3" }
                    };
                    context.Sectors.AddRange(sectors);
                }

                if (!context.Donors.Any())
                {
                    var donors = new List<Donor>
                    {
                        new Donor { Partner = "OCHA" }, // Remove explicit Code if using auto-increment
                        new Donor { Partner = "UNHCR 2" },
                        new Donor { Partner = "WFP" },
                        new Donor { Partner = "UNICEF" },
                        new Donor { Partner = "WHO" },
                        new Donor { Partner = "UNDP" },
                        new Donor { Partner = "FAO" },
                        new Donor { Partner = "OHCHR" },
                        new Donor { Partner = "UNRWA" },
                        new Donor { Partner = "INGO's" },
                        new Donor { Partner = "ICRC" },
                        new Donor { Partner = "MSF" },
                        new Donor { Partner = "NRC" },
                        new Donor { Partner = "UN-OCHA" },
                    };
                    context.Donors.AddRange(donors);
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

                if (!context.Ministries.Any())
                {
                    var ministries = new List<Ministry>
                    {
                        new Ministry { MinistryName = "Ministry of Public Works and Housing" },
                        new Ministry { MinistryName = "Ministry of Endowments" },
                        new Ministry { MinistryName = "Ministry of Local Administration and Environment" },
                        new Ministry { MinistryName = "Ministry of Information" },
                        new Ministry { MinistryName = "Ministry of Communications and Information Technology" },
                        new Ministry { MinistryName = "Ministry of Economy and Foreign Trade" },
                        new Ministry { MinistryName = "Ministry of Internal Trade and Consumer Protection" },
                        new Ministry { MinistryName = "Ministry of Education" },
                        new Ministry { MinistryName = "Ministry of Higher Education and Scientific Research" },
                        new Ministry { MinistryName = "Ministry of Administrative Development" },
                        new Ministry { MinistryName = "Ministry of Foreign Affairs and Expatriates" },
                        new Ministry { MinistryName = "Ministry of Interior" },
                        new Ministry { MinistryName = "Ministry of Defense" },
                        new Ministry { MinistryName = "Ministry of Irrigation" },
                        new Ministry { MinistryName = "Ministry of Youth and Sports" },
                        new Ministry { MinistryName = "Ministry of Agriculture and Agrarian Reform" },
                        new Ministry { MinistryName = "Ministry of Tourism" },
                        new Ministry { MinistryName = "Ministry of Social Affairs and Labor" },
                        new Ministry { MinistryName = "Ministry of Health" },
                        new Ministry { MinistryName = "Ministry of Industry" },
                        new Ministry { MinistryName = "Ministry of Energy" },
                        new Ministry { MinistryName = "Ministry of Emergency and Disaster Management" },
                        new Ministry { MinistryName = "Ministry of Justice" },
                        new Ministry { MinistryName = "Ministry of Electricity" },
                        new Ministry { MinistryName = "Ministry of Finance" },
                        new Ministry { MinistryName = "Ministry of Expatriates" },
                        new Ministry { MinistryName = "Ministry of Water Resources" },
                        new Ministry { MinistryName = "Ministry of Oil and Mineral Resources" },
                        new Ministry { MinistryName = "Ministry of Transport" },
                        new Ministry { MinistryName = "Ministry of Presidential Affairs" },
                    };

                    context.Ministries.AddRange(ministries);
                }


                // ✅ Check if Frameworks already exist
                if (!context.Frameworks.Include(f => f.Outcomes).Any())
                {
                    var frameworks = new List<Framework>
                {
                    new Framework
                    {
                        Name = "Health Framework",
                        IndicatorsPerformance = 80,
                        DisbursementPerformance = 90,
                        FieldMonitoring = 85,
                        ImpactAssessment = 75,
                        Outcomes = new List<Outcome>
                        {
                            new Outcome
                            {
                                Name = "Reduced Child Mortality",
                                IndicatorsPerformance = 78,
                                DisbursementPerformance = 88,
                                FieldMonitoring = 82,
                                ImpactAssessment = 70,
                                Weight = 0.4,
                                Outputs = new List<Output>
                                {
                                    new Output
                                    {
                                        Name = "Improved Vaccination Coverage",
                                        IndicatorsPerformance = 75,
                                        DisbursementPerformance = 85,
                                        FieldMonitoring = 80,
                                        ImpactAssessment = 65,
                                        Weight = 0.6,
                                        SubOutputs = new List<SubOutput>
                                        {
                                            new SubOutput
                                            {
                                                Name = "Increase in Immunization Rates",
                                                IndicatorsPerformance = 77,
                                                DisbursementPerformance = 87,
                                                FieldMonitoring = 81,
                                                ImpactAssessment = 68,
                                                Weight = 0.7,
                                                Indicators = new List<Indicator>
                                                {
                                                    new Indicator
                                                    {
                                                        Name = "Percentage of Fully Immunized Children",
                                                        IndicatorsPerformance = 80,
                                                        Weight = 0.5,
                                                        Target = 95,
                                                        GAGRA = 5.0,
                                                        GAGRR = 4.5,
                                                        Concept = "Increase immunization",
                                                        Description = "Measure of vaccination rates",
                                                        MethodOfComputation = "Survey data analysis",
                                                        Comment = "Expected to improve with awareness campaigns"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                    context.Frameworks.AddRange(frameworks);
                    context.SaveChanges();
                }




                context.SaveChanges();

            } // Context is disposed here when the scope ends
        }
    }
}
