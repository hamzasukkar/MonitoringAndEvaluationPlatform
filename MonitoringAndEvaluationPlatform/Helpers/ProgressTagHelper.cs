using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MonitoringAndEvaluationPlatform.Helpers
{
    // TagHelpers/ProgressTagHelper.cs
    [HtmlTargetElement("progress-bar")]
    public class ProgressBarTagHelper : TagHelper
    {
        public double Value { get; set; }
        public string Label { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var (barClass, valueClass) = ProgressHelper.GetProgressClasses(Value);

            output.TagName = "div";
            output.Attributes.Add("class", "metric");

            output.Content.SetHtmlContent(
                $@"<div class='metric-value {valueClass}'>{Value}%</div>
               <div class='metric-label'>{Label}</div>
               <div class='progress-container'>
                   <div class='progress-bar {barClass}' style='width:{Value}%'></div>
               </div>");
        }
    }
}
