namespace MonitoringAndEvaluationPlatform.Models
{
    public static class Permissions
    {
        // Interface 1 - Login permissions (all users can login)
        public const string Login = "Login";
        public const string RecoverPassword = "RecoverPassword";

        // Interface 2 - Strategy Management
        public const string ReadStrategies = "ReadStrategies";
        public const string AddStrategy = "AddStrategy";
        public const string ModifyStrategy = "ModifyStrategy";
        public const string DeleteStrategy = "DeleteStrategy";

        // Interface 3 - Policy Management
        public const string ReadPolicies = "ReadPolicies";
        public const string AddPolicy = "AddPolicy";
        public const string ModifyPolicy = "ModifyPolicy";
        public const string DeletePolicy = "DeletePolicy";

        // Interface 4 - Program Management
        public const string ReadPrograms = "ReadPrograms";
        public const string AddProgram = "AddProgram";
        public const string EditProgram = "EditProgram";
        public const string DeleteProgram = "DeleteProgram";

        // Interface 5 - Subprogram Management
        public const string ReadSubprograms = "ReadSubprograms";
        public const string AddSubprogram = "AddSubprogram";
        public const string EditSubprogram = "EditSubprogram";
        public const string DeleteSubprogram = "DeleteSubprogram";

        // Interface 6 - Project Management
        public const string ReadProjects = "ReadProjects";
        public const string AddProject = "AddProject";
        public const string EditProject = "EditProject";
        public const string DeleteProject = "DeleteProject";

        // Interface 7 - Project Forms
        public const string ReadProjectForms = "ReadProjectForms";
        public const string FillProjectForm = "FillProjectForm";
        public const string EditProjectForm = "EditProjectForm";
        public const string DeleteProjectForm = "DeleteProjectForm";

        // Interface 8 - Project Metrics
        public const string ReadProjectMetrics = "ReadProjectMetrics";
        public const string AddMetricValue = "AddMetricValue";
        public const string EditMetricValues = "EditMetricValues";
        public const string DeleteMetricValues = "DeleteMetricValues";

        // Interface 9 - Action Plans
        public const string ReadActionPlans = "ReadActionPlans";
        public const string ModifyPlanStatus = "ModifyPlanStatus";
        public const string DeleteActionPlan = "DeleteActionPlan";

        // Interface 10 - General Control Panel
        public const string ViewControlPanel = "ViewControlPanel";

        // Interface 11 - Ministries Management
        public const string ReadMinistries = "ReadMinistries";
        public const string CreateMinistry = "CreateMinistry";
        public const string ModifyMinistry = "ModifyMinistry";
        public const string DeleteMinistry = "DeleteMinistry";
        public const string DisplayMinistryIndicators = "DisplayMinistryIndicators";

        // Interface 12 - Project Dashboard
        public const string BrowseProjects = "BrowseProjects";
        public const string MonitorPerformance = "MonitorPerformance";
        public const string ClassifyProjects = "ClassifyProjects";

        // Interface 13 - Strategic Indicators Dashboard
        public const string DisplayStrategicIndicators = "DisplayStrategicIndicators";
        public const string ComparePerformance = "ComparePerformance";
        public const string EditStrategyData = "EditStrategyData";

        // Interface 15 - Comprehensive Performance Reporting
        public const string ViewReports = "ViewReports";
        public const string AnalyzePerformance = "AnalyzePerformance";
        public const string ExportReports = "ExportReports";

        // Interface 16 - Outcome Management
        public const string ReadOutcomes = "ReadOutcomes";
        public const string AddOutcome = "AddOutcome";
        public const string ModifyOutcome = "ModifyOutcome";
        public const string DeleteOutcome = "DeleteOutcome";

        // Interface 17 - Output Management
        public const string ReadOutputs = "ReadOutputs";
        public const string AddOutput = "AddOutput";
        public const string ModifyOutput = "ModifyOutput";
        public const string DeleteOutput = "DeleteOutput";

        // Interface 18 - SubOutput Management
        public const string ReadSubOutputs = "ReadSubOutputs";
        public const string AddSubOutput = "AddSubOutput";
        public const string ModifySubOutput = "ModifySubOutput";
        public const string DeleteSubOutput = "DeleteSubOutput";

        // Interface 19 - Indicator Management
        public const string ReadIndicators = "ReadIndicators";
        public const string AddIndicator = "AddIndicator";
        public const string ModifyIndicator = "ModifyIndicator";
        public const string DeleteIndicator = "DeleteIndicator";

        // Additional permissions for indicators analysis
        public const string IndicatorAnalysis = "IndicatorAnalysis";
    }
}