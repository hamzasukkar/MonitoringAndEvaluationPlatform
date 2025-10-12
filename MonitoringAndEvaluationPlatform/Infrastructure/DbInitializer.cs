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
                        new Ministry { MinistryDisplayName_AR = "وزارة الخارجية والمغتربين", MinistryDisplayName_EN = "Ministry of Foreign Affairs and Expatriates", MinistryUserName = "MoFA", Logo = "🌐" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الدفاع", MinistryDisplayName_EN = "Ministry of Defense", MinistryUserName = "MoD", Logo = "🛡️" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الداخلية", MinistryDisplayName_EN = "Ministry of Interior", MinistryUserName = "MoI", Logo = "🏛️" },
                        new Ministry { MinistryDisplayName_AR = "وزارة العدل", MinistryDisplayName_EN = "Ministry of Justice", MinistryUserName = "MoJ", Logo = "⚖️" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الأوقاف", MinistryDisplayName_EN = "Ministry of Endowments", MinistryUserName = "MoAwaqf", Logo = "🕌" },
                        new Ministry { MinistryDisplayName_AR = "وزارة التعليم العالي والبحث العلمي", MinistryDisplayName_EN = "Ministry of Higher Education and Scientific Research", MinistryUserName = "MoHESR", Logo = "🎓" },
                        new Ministry { MinistryDisplayName_AR = "وزارة التربية", MinistryDisplayName_EN = "Ministry of Education", MinistryUserName = "MoE", Logo = "📚" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الشؤون الاجتماعية والعمل", MinistryDisplayName_EN = "Ministry of Social Affairs and Labor", MinistryUserName = "MoSAL", Logo = "👥" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الاقتصاد والتجارة الخارجية", MinistryDisplayName_EN = "Ministry of Economy and Foreign Trade", MinistryUserName = "MoECT", Logo = "💼" },
                        new Ministry { MinistryDisplayName_AR = "وزارة المالية", MinistryDisplayName_EN = "Ministry of Finance", MinistryUserName = "MoF", Logo = "💰" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الصحة", MinistryDisplayName_EN = "Ministry of Health", MinistryUserName = "MoH", Logo = "🏥" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الإدارة المحلية والبيئة", MinistryDisplayName_EN = "Ministry of Local Administration and Environment", MinistryUserName = "MoLAE", Logo = "🌳" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الأشغال العامة والإسكان", MinistryDisplayName_EN = "Ministry of Public Works and Housing", MinistryUserName = "MoPWH", Logo = "🏗️" },
                        new Ministry { MinistryDisplayName_AR = "وزارة النقل", MinistryDisplayName_EN = "Ministry of Transport", MinistryUserName = "MoT", Logo = "🚗" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الاتصالات وتقانة المعلومات", MinistryDisplayName_EN = "Ministry of Communications and Information Technology", MinistryUserName = "MoCIT", Logo = "📡" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الزراعة والإصلاح الزراعي", MinistryDisplayName_EN = "Ministry of Agriculture and Agrarian Reform", MinistryUserName = "MoAAR", Logo = "🌾" },
                        new Ministry { MinistryDisplayName_AR = "وزارة السياحة", MinistryDisplayName_EN = "Ministry of Tourism", MinistryUserName = "MoTourism", Logo = "✈️" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الصناعة", MinistryDisplayName_EN = "Ministry of Industry", MinistryUserName = "MoIndustry", Logo = "🏭" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الكهرباء", MinistryDisplayName_EN = "Ministry of Electricity", MinistryUserName = "MoElectricity", Logo = "⚡" },
                        new Ministry { MinistryDisplayName_AR = "وزارة النفط والثروة المعدنية", MinistryDisplayName_EN = "Ministry of Oil and Mineral Resources", MinistryUserName = "MoOMR", Logo = "🛢️" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الموارد المائية", MinistryDisplayName_EN = "Ministry of Water Resources", MinistryUserName = "MoWR", Logo = "💧" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الإعلام", MinistryDisplayName_EN = "Ministry of Information", MinistryUserName = "MoInfo", Logo = "📰" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الثقافة", MinistryDisplayName_EN = "Ministry of Culture", MinistryUserName = "MoCulture", Logo = "🎭" },
                        new Ministry { MinistryDisplayName_AR = "وزارة التنمية الإدارية", MinistryDisplayName_EN = "Ministry of Administrative Development", MinistryUserName = "MoAD", Logo = "📊" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الرياضة والشباب", MinistryDisplayName_EN = "Ministry of Sports and Youth", MinistryUserName = "MoSY", Logo = "⚽" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الطاقة", MinistryDisplayName_EN = "Ministry of Energy", MinistryUserName = "MoEnergy", Logo = "🔋" },
                        new Ministry { MinistryDisplayName_AR = "وزارة الطوارئ والكوارث", MinistryDisplayName_EN = "Ministry of Emergency and Disaster Management", MinistryUserName = "MoEDM", Logo = "🚨" },
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
                                MinistryName = ministry.MinistryDisplayName_EN
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
                                MinistryName = ministry.MinistryDisplayName_EN
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
                        new SuperVisor
                        {
                            Name = "أحمد محمود السيد",
                            PhoneNumber = "+963-11-2345678",
                            Email = "ahmed.mahmoud@supervision.gov.sy"
                        },
                        new SuperVisor
                        {
                            Name = "فاطمة علي الحسن",
                            PhoneNumber = "+963-21-3456789",
                            Email = "fatima.ali@supervision.gov.sy"
                        },
                        new SuperVisor
                        {
                            Name = "محمد خالد الأحمد",
                            PhoneNumber = "+963-31-4567890",
                            Email = "mohammed.khaled@supervision.gov.sy"
                        }
                    };

                    context.SuperVisors.AddRange(superVisors);
                    await context.SaveChangesAsync();
                }
                if (!context.ProjectManagers.Any())
                {
                    var projectManagers = new List<ProjectManager>
                    {
                        new ProjectManager
                        {
                            Name = "سامر يوسف العمر",
                            PhoneNumber = "+963-11-9876543",
                            Email = "samer.yousef@projects.gov.sy"
                        },
                        new ProjectManager
                        {
                            Name = "ليلى حسن الخطيب",
                            PhoneNumber = "+963-21-8765432",
                            Email = "layla.hassan@projects.gov.sy"
                        },
                        new ProjectManager
                        {
                            Name = "عمر صالح الديري",
                            PhoneNumber = "+963-31-7654321",
                            Email = "omar.saleh@projects.gov.sy"
                        }
                    };

                    context.ProjectManagers.AddRange(projectManagers);
                    await context.SaveChangesAsync();
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
                    await context.SaveChangesAsync();
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

