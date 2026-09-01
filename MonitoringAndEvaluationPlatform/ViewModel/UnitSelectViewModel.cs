namespace MonitoringAndEvaluationPlatform.ViewModel
{
    /// <summary>
    /// Options for the shared _UnitSelect partial — the dropdown that replaced the free-text
    /// unit inputs on the impact indicator, framework goal and measure forms.
    ///
    /// The partial loads the unit list itself rather than taking it here, so a page can drop the
    /// control in without its controller having to populate a ViewBag.
    /// </summary>
    public class UnitSelectViewModel
    {
        /// <summary>The posted form field name — normally "UnitCode".</summary>
        public string FieldName { get; set; } = "UnitCode";

        /// <summary>
        /// Element id for the select. Must be unique on the page; pages that render the control
        /// more than once (an edit row per record) pass a suffixed id.
        /// </summary>
        public string ElementId { get; set; } = "UnitCode";

        /// <summary>Currently selected MeasurementUnit.Code, or null for none.</summary>
        public int? SelectedCode { get; set; }

        /// <summary>Adds the required attribute and drops the empty option.</summary>
        public bool Required { get; set; }

        /// <summary>Extra CSS classes for the select element.</summary>
        public string CssClass { get; set; } = "form-select";

        /// <summary>
        /// When false the "add new unit" control is hidden, leaving a plain dropdown.
        /// </summary>
        public bool AllowAddNew { get; set; } = true;

        /// <summary>
        /// Renders only the selected option and fills the rest in from JSON the first time the
        /// user opens the list. Set this wherever the control repeats per table row: the
        /// Measures page has hundreds of rows, and spelling out every unit in each of them added
        /// megabytes to the HTML.
        /// </summary>
        public bool DeferOptions { get; set; }
    }
}
