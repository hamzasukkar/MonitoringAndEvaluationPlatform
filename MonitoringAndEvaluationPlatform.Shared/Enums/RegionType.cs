using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Enums
{
    public enum RegionType
    {
        [Display(Name = "Region")]
        Region = 1,

        [Display(Name = "Province")]
        Province = 2,

        [Display(Name = "District")]
        District = 3,

        [Display(Name = "Community")]
        Community = 4
    }
}
