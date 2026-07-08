using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Data
{
    public class ApplicationDbInitializer
    {
        public static void SeedGovernoratesFromJson(ApplicationDbContext context)
        {
            if (!context.Governorates.Any())
            {
                var json = File.ReadAllText("SeedData/Governorates.json");
                var governorates = JsonSerializer.Deserialize<List<Governorate>>(json);
                context.Governorates.AddRange(governorates);
                context.SaveChanges();
            }
        }
        public static void SeedDistrictsFromJson(ApplicationDbContext context)
        {
            if (!context.Districts.Any())
            {
                var json = File.ReadAllText("SeedData/Districts.json");
                var districts = JsonSerializer.Deserialize<List<District>>(json);
                context.Districts.AddRange(districts);
                context.SaveChanges();
            }
        }

        public static void SeedSubDistrictsFromJson(ApplicationDbContext context)
        {
            if (!context.SubDistricts.Any())
            {
                var json = File.ReadAllText("SeedData/SubDistricts.json");
                var subDistricts = JsonSerializer.Deserialize<List<SubDistrict>>(json);
                context.SubDistricts.AddRange(subDistricts);
                context.SaveChanges();
            }
        }

        public static void SeedCommunitiesFromJson(ApplicationDbContext context)
        {
            if (!context.Communities.Any())
            {
                var json = File.ReadAllText("SeedData/Communities.json");
                var communities = JsonSerializer.Deserialize<List<Community>>(json);

                // Get all existing SubDistrict codes
                var validSubDistrictCodes = context.SubDistricts.Select(sd => sd.Code).ToHashSet();

                var validCommunities = new List<Community>();

                foreach (var community in communities)
                {
                    if (validSubDistrictCodes.Contains(community.SubDistrictCode))
                    {
                        validCommunities.Add(community);
                    }
                    else
                    {
                        Console.WriteLine($"⚠ Skipping Community with SubDistrictCode {community.SubDistrictCode} — not found in SubDistricts table.");
                    }
                }

                context.Communities.AddRange(validCommunities);
                context.SaveChanges();
            }
        }

        // Ensures every governorate has a same-named District → SubDistrict → Community chain,
        // so a project location can be recorded at the "entire governorate" level.
        // Idempotent: each level is looked up by name within its parent before creating.
        public static void EnsureGovernorateLocationChains(ApplicationDbContext context)
        {
            var governorates = context.Governorates.ToList();
            foreach (var gov in governorates)
            {
                var district = context.Districts
                    .FirstOrDefault(d => d.GovernorateCode == gov.Code && d.AR_Name == gov.AR_Name);
                if (district == null)
                {
                    district = new District
                    {
                        Code = GenerateUnusedChildCode(gov.Code, c => context.Districts.Any(d => d.Code == c)),
                        GovernorateCode = gov.Code,
                        AR_Name = gov.AR_Name,
                        EN_Name = gov.EN_Name
                    };
                    context.Districts.Add(district);
                    context.SaveChanges();
                }

                var subDistrict = context.SubDistricts
                    .FirstOrDefault(s => s.DistrictCode == district.Code && s.AR_Name == gov.AR_Name);
                if (subDistrict == null)
                {
                    subDistrict = new SubDistrict
                    {
                        Code = GenerateUnusedChildCode(district.Code, c => context.SubDistricts.Any(s => s.Code == c)),
                        DistrictCode = district.Code,
                        AR_Name = gov.AR_Name,
                        EN_Name = gov.EN_Name
                    };
                    context.SubDistricts.Add(subDistrict);
                    context.SaveChanges();
                }

                var community = context.Communities
                    .FirstOrDefault(c => c.SubDistrictCode == subDistrict.Code && c.AR_Name == gov.AR_Name);
                if (community == null)
                {
                    var commCode = "CG" + gov.Code; // e.g. CGSY02 — distinct from the seeded C#### scheme
                    while (context.Communities.Any(c => c.Code == commCode))
                        commCode += "X";
                    context.Communities.Add(new Community
                    {
                        Code = commCode,
                        SubDistrictCode = subDistrict.Code,
                        AR_Name = gov.AR_Name,
                        EN_Name = gov.EN_Name
                    });
                    context.SaveChanges();
                }
            }
        }

        // Appends a 2-digit suffix, probing from "99" downward until an unused code is found.
        private static string GenerateUnusedChildCode(string parentCode, Func<string, bool> exists)
        {
            for (int suffix = 99; suffix >= 0; suffix--)
            {
                var candidate = parentCode + suffix.ToString("D2");
                if (!exists(candidate)) return candidate;
            }
            throw new InvalidOperationException($"No free child location code available under {parentCode}");
        }
    }
}
