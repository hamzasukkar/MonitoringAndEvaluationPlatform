using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ProjectDonor
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public int DonorCode { get; set; }
        public Donor Donor { get; set; }

        [Range(0, 100, ErrorMessage = "Funding percentage must be between 0 and 100")]
        public decimal FundingPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Funding amount cannot be negative")]
        public decimal FundingAmount { get; set; }
    }
}