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


    }
}
