using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Plan
    {
        [Key]
        public int Code { get; set; }

        public string Name { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM yyyy}")]
        public DateTime Date { get; set; }

        public int Realised { get; set; }

        public int ActionPlanCode { get; set; }

        virtual public ActionPlan ActionPlan { get; set; }
    }
}
