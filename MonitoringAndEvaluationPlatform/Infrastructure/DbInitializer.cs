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
                }

                // Create users for all ministries (runs independently of ministry seeding)
                if (context.Ministries.Any())
                {
                    var ministries = context.Ministries.ToList();

                    // Ensure MinistriesUser role exists
                    string ministriesRoleName = "MinistriesUser";
                    if (!await roleManager.RoleExistsAsync(ministriesRoleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(ministriesRoleName));
                    }

                    // Ensure DataEntry role exists
                    string dataEntryRoleName = "DataEntry";
                    if (!await roleManager.RoleExistsAsync(dataEntryRoleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(dataEntryRoleName));
                    }

                    foreach (var ministry in ministries)
                    {
                        string userName = ministry.MinistryUserName;
                        string email = $"{userName.ToLower()}@example.com";
                        string defaultPassword = "Ministry@123";

                        // Create Ministry user if it doesn't exist
                        var existingUser = await userManager.FindByNameAsync(userName);
                        if (existingUser == null)
                        {
                            var user = new ApplicationUser
                            {
                                UserName = userName,
                                Email = email,
                                EmailConfirmed = true,
                                MinistryName = ministry.MinistryDisplayName
                            };

                            var result = await userManager.CreateAsync(user, defaultPassword);
                            if (result.Succeeded)
                            {
                                await userManager.AddToRoleAsync(user, ministriesRoleName);
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Failed to create user {userName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            }
                        }
                        else
                        {
                            // Ensure existing user has the correct role
                            if (!await userManager.IsInRoleAsync(existingUser, ministriesRoleName))
                            {
                                await userManager.AddToRoleAsync(existingUser, ministriesRoleName);
                            }
                        }

                        // Create Data Entry user for each ministry
                        string dataEntryUserName = $"{ministry.MinistryUserName}_DE";
                        string dataEntryEmail = $"{dataEntryUserName.ToLower()}@example.com";

                        var existingDataEntryUser = await userManager.FindByNameAsync(dataEntryUserName);
                        if (existingDataEntryUser == null)
                        {
                            var dataEntryUser = new ApplicationUser
                            {
                                UserName = dataEntryUserName,
                                Email = dataEntryEmail,
                                EmailConfirmed = true,
                                MinistryName = ministry.MinistryDisplayName
                            };

                            var result = await userManager.CreateAsync(dataEntryUser, defaultPassword);
                            if (result.Succeeded)
                            {
                                await userManager.AddToRoleAsync(dataEntryUser, dataEntryRoleName);
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Failed to create data entry user {dataEntryUserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            }
                        }
                        else
                        {
                            // Ensure existing data entry user has the correct role
                            if (!await userManager.IsInRoleAsync(existingDataEntryUser, dataEntryRoleName))
                            {
                                await userManager.AddToRoleAsync(existingDataEntryUser, dataEntryRoleName);
                            }
                        }
                    }
                }
                if (!context.Goals.Any())
                {
                    var goals = new List<Goal>

                        {
                            new Goal
                            {
                                EN_Name = "No Poverty",
                                AR_Name = "القضاء على الفقر",
                                Icon = "/img/E-WEB-Goal-01.png",
                                Targets = new List<Target>
                                {
                                    new Target
                                    {
                                        EN_Name = "Eradicate extreme poverty",
                                        AR_Name = "القضاء على الفقر المدقع",
                                        SDGsIndicators = new List<SDGIndicator>
                                        {
                                            new SDGIndicator
                                            {
                                                EN_Name = "Proportion of population below $1.90 a day",
                                                AR_Name = "نسبة السكان تحت خط الفقر (1.90 دولار يومياً)",
                                                Source = "UN",
                                                IsCommon = true,
                                                Active = true
                                            },
                                            new SDGIndicator
                                            {
                                                EN_Name = "Proportion of men, women and children living in poverty",
                                                AR_Name = "نسبة الرجال والنساء والأطفال الذين يعيشون في فقر",
                                                Source = "World Bank",
                                                IsCommon = false,
                                                Active = true
                                            }
                                        }
                                    },
                                    new Target
                                    {
                                        EN_Name = "Implement social protection systems",
                                        AR_Name = "تنفيذ نظم الحماية الاجتماعية",
                                        SDGsIndicators = new List<SDGIndicator>
                                        {
                                            new SDGIndicator
                                            {
                                                EN_Name = "Coverage of social protection systems",
                                                AR_Name = "نسبة التغطية بأنظمة الحماية الاجتماعية",
                                                Source = "ILO",
                                                IsCommon = true,
                                                Active = true
                                            }
                                        }
                                    }
                                }
                            },

                            new Goal
                            {
                                EN_Name = "Zero Hunger",
                                AR_Name = "القضاء التام على الجوع",
                                Icon = "/img/E-WEB-Goal-02.png",
                                Targets = new List<Target>
                                {
                                    new Target
                                    {
                                        EN_Name = "End hunger and ensure access to safe food",
                                        AR_Name = "القضاء على الجوع وضمان الحصول على غذاء آمن",
                                        SDGsIndicators = new List<SDGIndicator>
                                        {
                                            new SDGIndicator
                                            {
                                                EN_Name = "Prevalence of undernourishment",
                                                AR_Name = "معدل انتشار سوء التغذية",
                                                Source = "FAO",
                                                IsCommon = true,
                                                Active = true
                                            }
                                        }
                                    }
                                }
                            },

                        new Goal { EN_Name = "Good Health and Well‑being", AR_Name = "الصحة الجيدة والرفاه", Icon = "/img/E-WEB-Goal-03.png" },
                        new Goal { EN_Name = "Quality Education", AR_Name = "التعليم الجيد", Icon = "/img/E-WEB-Goal-04.png" },
                        new Goal { EN_Name = "Gender Equality", AR_Name = "المساواة بين الجنسين", Icon = "/img/E-WEB-Goal-05.png" },
                        new Goal { EN_Name = "Clean Water and Sanitation", AR_Name = "المياه النظيفة والنظافة الصحية", Icon = "/img/E-WEB-Goal-06.png" },
                        new Goal { EN_Name = "Affordable and Clean Energy", AR_Name = "طاقة نظيفة وبأسعار معقولة", Icon = "/img/E-WEB-Goal-07.png" },
                        new Goal { EN_Name = "Decent Work and Economic Growth", AR_Name = "العمل اللائق ونمو الاقتصاد", Icon = "/img/E-WEB-Goal-08.png" },
                        new Goal { EN_Name = "Industry, Innovation and Infrastructure", AR_Name = "الصناعة والابتكار والهياكل الأساسية", Icon = "/img/E-WEB-Goal-09.png" },
                        new Goal { EN_Name = "Reduced Inequalities", AR_Name = "الحد من أوجه عدم المساواة", Icon = "/img/E-WEB-Goal-10.png" },
                        new Goal { EN_Name = "Sustainable Cities and Communities", AR_Name = "مدن ومجتمعات محلية مستدامة", Icon = "/img/E-WEB-Goal-11.png" },
                        new Goal { EN_Name = "Responsible Consumption and Production", AR_Name = "الاستهلاك والإنتاج المسؤولان", Icon = "/img/E-WEB-Goal-12.png" },
                        new Goal { EN_Name = "Climate Action", AR_Name = "العمل المناخي", Icon = "/img/E-WEB-Goal-13.png" },
                        new Goal { EN_Name = "Life Below Water", AR_Name = "الحياة تحت الماء", Icon = "/img/E-WEB-Goal-14.png" },
                        new Goal { EN_Name = "Life on Land", AR_Name = "الحياة في البر", Icon = "/img/E-WEB-Goal-15.png" },
                        new Goal { EN_Name = "Peace, Justice and Strong Institutions", AR_Name = "السلام والعدل والمؤسسات القوية", Icon = "/img/E-WEB-Goal-16.png" },
                        new Goal { EN_Name = "Partnerships for the Goals", AR_Name = "عقد الشراكات لتحقيق الأهداف", Icon = "/img/E-WEB-Goal-17.png" },
                    };
                    context.Goals.AddRange(goals);
                    context.SaveChanges();
                }


                if (!context.Sectors.Any())
                {
                    var sectors = new List<Sector>
                    {
                        new Sector { EN_Name = "Public",AR_Name = "عام" },
                        new Sector { EN_Name = "Private",AR_Name = "خاص" },
                        new Sector { EN_Name = "Common",AR_Name = "مشترك" },
                        new Sector { EN_Name = "Domestic",AR_Name = "أهلي" }
                    };
                    context.Sectors.AddRange(sectors);
                    context.SaveChanges();
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
                    context.SaveChanges();
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

                // Seed Governorates (Syria regions) if they don't exist
                if (!context.Governorates.Any())
                {
                    var governorates = new List<Governorate>
                    {
                        new Governorate { Code = "01", AR_Name = "دمشق", EN_Name = "Damascus" },
                        new Governorate { Code = "02", AR_Name = "ريف دمشق", EN_Name = "Damascus Countryside" },
                        new Governorate { Code = "03", AR_Name = "حلب", EN_Name = "Aleppo" },
                        new Governorate { Code = "04", AR_Name = "حمص", EN_Name = "Homs" },
                        new Governorate { Code = "05", AR_Name = "حماة", EN_Name = "Hama" },
                        new Governorate { Code = "06", AR_Name = "اللاذقية", EN_Name = "Latakia" },
                        new Governorate { Code = "07", AR_Name = "طرطوس", EN_Name = "Tartous" },
                        new Governorate { Code = "08", AR_Name = "إدلب", EN_Name = "Idlib" },
                        new Governorate { Code = "09", AR_Name = "درعا", EN_Name = "Daraa" },
                        new Governorate { Code = "10", AR_Name = "السويداء", EN_Name = "As-Suwayda" },
                        new Governorate { Code = "11", AR_Name = "القنيطرة", EN_Name = "Quneitra" },
                        new Governorate { Code = "12", AR_Name = "دير الزور", EN_Name = "Deir ez-Zor" },
                        new Governorate { Code = "13", AR_Name = "الرقة", EN_Name = "Ar-Raqqah" },
                        new Governorate { Code = "14", AR_Name = "الحسكة", EN_Name = "Al-Hasakah" }
                    };

                    context.Governorates.AddRange(governorates);
                    context.SaveChanges();
                }

                context.SaveChanges();

            } // Context is disposed here when the scope ends
        }
    }
}

