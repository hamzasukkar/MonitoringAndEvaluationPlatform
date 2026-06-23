using System.Collections.Generic;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    /// <summary>
    /// Matrix of ministry projects × budget categories ("الفقرات").
    /// Each project has a Budget (الميزانية) and Disbursement (الصرف) value per category.
    /// </summary>
    public class BudgetMatrixViewModel
    {
        public List<string> Categories { get; set; } = new();
        public List<BudgetMatrixRow> Projects { get; set; } = new();
    }

    public class BudgetMatrixRow
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        // category name -> phase budget
        public Dictionary<string, double> Budget { get; set; } = new();

        // category name -> sum of realised (disbursed) values
        public Dictionary<string, double> Disbursement { get; set; } = new();
    }
}
