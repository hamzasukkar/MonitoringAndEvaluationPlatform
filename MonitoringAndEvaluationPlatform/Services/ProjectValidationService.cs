using Microsoft.AspNetCore.Mvc.ModelBinding;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Services
{
    public interface IProjectValidationService
    {
        void ValidateProjectCreation(
            Project project,
            List<LocationSelectionViewModel>? selectedLocations,
            List<string> selectedSectorCodes,
            List<int>? selectedIndicators,
            int plansCount,
            ModelStateDictionary modelState);
    }

    public class ProjectValidationService : IProjectValidationService
    {
        public void ValidateProjectCreation(
            Project project,
            List<LocationSelectionViewModel>? selectedLocations,
            List<string> selectedSectorCodes,
            List<int>? selectedIndicators,
            int plansCount,
            ModelStateDictionary modelState)
        {
            // Validate Plans Count
            if (plansCount < 1)
            {
                modelState.AddModelError("PlansCount", "Plans count must be at least 1 month.");
            }

            // Validate location selection
            if (selectedLocations == null || !selectedLocations.Any())
            {
                modelState.AddModelError("", "At least one project location must be selected. Please use the location selector to add project locations.");
            }

            // Validate sectors
            if (!selectedSectorCodes.Any())
            {
                modelState.AddModelError("Sectors", "At least one sector must be selected for the project.");
            }

            // Validate indicators
            if (selectedIndicators == null || !selectedIndicators.Any())
            {
                modelState.AddModelError("SelectedIndicators", "At least one performance indicator must be selected to measure project success.");
            }

            // Validate project manager and supervisor
            if (project.ProjectManagerCode <= 0)
            {
                modelState.AddModelError(nameof(project.ProjectManagerCode), "Please select a qualified project manager.");
            }

            if (project.SuperVisorCode <= 0)
            {
                modelState.AddModelError(nameof(project.SuperVisorCode), "Please select a supervisor to oversee the project.");
            }

            // Validate date range
            if (project.StartDate >= project.EndDate)
            {
                modelState.AddModelError(nameof(project.EndDate), "Project end date must be after the start date.");
            }

            // Validate future dates
            if (project.StartDate < DateTime.Today.AddDays(-30))
            {
                modelState.AddModelError(nameof(project.StartDate), "Project start date cannot be more than 30 days in the past.");
            }

            // Validate project duration (not too long)
            var projectDuration = (project.EndDate - project.StartDate).Days;
            if (projectDuration > 3650) // 10 years
            {
                modelState.AddModelError(nameof(project.EndDate), "Project duration cannot exceed 10 years.");
            }

            // Validate budget reasonableness
            if (project.EstimatedBudget > 1000000000) // 1 billion
            {
                modelState.AddModelError(nameof(project.EstimatedBudget), "Estimated budget seems unusually high. Please verify the amount.");
            }
        }
    }
}