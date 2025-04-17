using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace MonitoringAndEvaluationPlatform.ViewModels
{
    public class DataSuggestionViewModel
    {
        // Filters - selected values
        public int? SelectedFramework { get; set; }
        public int? SelectedOutcome { get; set; }
        public int? SelectedOutput { get; set; }
        public int? SelectedSubOutput { get; set; }
        public int? SelectedIndicator { get; set; }
        public int? SelectedProject { get; set; }
        public int? SelectedMinistry { get; set; }
        public int? SelectedRegion { get; set; }
        public int? SelectedSector { get; set; }
        public int? SelectedDonor { get; set; }

        // Dropdown options
        public List<SelectListItem> Frameworks { get; set; } = new();
        public List<SelectListItem> Outcomes { get; set; } = new();
        public List<SelectListItem> Outputs { get; set; } = new();
        public List<SelectListItem> SubOutputs { get; set; } = new();
        public List<SelectListItem> Indicators { get; set; } = new();
        public List<SelectListItem> Projects { get; set; } = new();
        public List<SelectListItem> Ministries { get; set; } = new();
        public List<SelectListItem> Regions { get; set; } = new();
        public List<SelectListItem> Sectors { get; set; } = new();
        public List<SelectListItem> Donors { get; set; } = new();

        // Chart data
        public List<ChartItem> ChartItems { get; set; } = new();
    }

    public class ChartItem
    {
        public int ID { get; set; }
        public string Label { get; set; }
        public string Title { get; set; }
        public double Actual { get; set; }
        public double Target { get; set; }
        public string Unit { get; set; } = "";
    }
}
