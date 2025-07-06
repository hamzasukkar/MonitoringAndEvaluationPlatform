using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Infrastructure
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            // Create a scope to manage the context's lifetime
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

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
                        new Ministry { MinistryDisplayName = "وزارة الخارجية والمغتربين", MinistryUserName = "MoFA" },
                        new Ministry { MinistryDisplayName = "وزارة الدفاع", MinistryUserName = "MoD" },
                        new Ministry { MinistryDisplayName = "وزارة الداخلية", MinistryUserName = "MoI" },
                        new Ministry { MinistryDisplayName = "وزارة العدل", MinistryUserName = "MoJ" },
                        new Ministry { MinistryDisplayName = "وزارة الأوقاف", MinistryUserName = "MoAwaqf" },
                        new Ministry { MinistryDisplayName = "وزارة التعليم العالي والبحث العلمي", MinistryUserName = "MoHESR" },
                        new Ministry { MinistryDisplayName = "وزارة التربية", MinistryUserName = "MoE" },
                        new Ministry { MinistryDisplayName = "وزارة الشؤون الاجتماعية والعمل", MinistryUserName = "MoSAL" },
                        new Ministry { MinistryDisplayName = "وزارة الاقتصاد والتجارة الخارجية", MinistryUserName = "MoECT" },
                        new Ministry { MinistryDisplayName = "وزارة المالية", MinistryUserName = "MoF" },
                        new Ministry { MinistryDisplayName = "وزارة الصحة", MinistryUserName = "MoH" },
                        new Ministry { MinistryDisplayName = "وزارة الإدارة المحلية والبيئة", MinistryUserName = "MoLAE" },
                        new Ministry { MinistryDisplayName = "وزارة الأشغال العامة والإسكان", MinistryUserName = "MoPWH" },
                        new Ministry { MinistryDisplayName = "وزارة النقل", MinistryUserName = "MoT" },
                        new Ministry { MinistryDisplayName = "وزارة الاتصالات وتقانة المعلومات", MinistryUserName = "MoCIT" },
                        new Ministry { MinistryDisplayName = "وزارة الزراعة والإصلاح الزراعي", MinistryUserName = "MoAAR" },
                        new Ministry { MinistryDisplayName = "وزارة السياحة", MinistryUserName = "MoTourism" },
                        new Ministry { MinistryDisplayName = "وزارة الصناعة", MinistryUserName = "MoIndustry" },
                        new Ministry { MinistryDisplayName = "وزارة الكهرباء", MinistryUserName = "MoElectricity" },
                        new Ministry { MinistryDisplayName = "وزارة النفط والثروة المعدنية", MinistryUserName = "MoOMR" },
                        new Ministry { MinistryDisplayName = "وزارة الموارد المائية", MinistryUserName = "MoWR" },
                        new Ministry { MinistryDisplayName = "وزارة الإعلام", MinistryUserName = "MoInfo" },
                        new Ministry { MinistryDisplayName = "وزارة الثقافة", MinistryUserName = "MoCulture" },
                        new Ministry { MinistryDisplayName = "وزارة التنمية الإدارية", MinistryUserName = "MoAD" },
                        new Ministry { MinistryDisplayName = "وزارة الرياضة والشباب", MinistryUserName = "MoSY" },
                        new Ministry { MinistryDisplayName = "وزارة الطاقة", MinistryUserName = "MoEnergy" },
                        new Ministry { MinistryDisplayName = "وزارة الطوارئ والكوارث", MinistryUserName = "MoEDM" },
                    };
                    context.Ministries.AddRange(ministries);
                    await context.SaveChangesAsync(); // Ensure ministries are saved before creating users

                    foreach (var ministry in ministries)
                    {
                        string roleName = ministry.MinistryUserName;
                        string userName = ministry.MinistryUserName;
                        string email = $"{userName.ToLower()}@example.com";
                        string defaultPassword = "Ministry@123";

                        // Create role if it doesn’t exist
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            await roleManager.CreateAsync(new IdentityRole(roleName));
                        }

                        // Create user if it doesn’t exist
                        var existingUser = await userManager.FindByNameAsync(userName);
                        if (existingUser == null)
                        {
                            var user = new ApplicationUser
                            {
                                UserName = userName,
                                Email = email,
                                EmailConfirmed = true,
                                MinistryName = userName // Or MinistryDisplayName if preferred
                            };

                            var result = await userManager.CreateAsync(user, defaultPassword);
                            if (result.Succeeded)
                            {
                                await userManager.AddToRoleAsync(user, roleName);
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Failed to create user {userName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            }
                        }
                    }
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
