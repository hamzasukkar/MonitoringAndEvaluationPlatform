using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace MonitoringAndEvaluationPlatform.Helpers
{
    /// <summary>
    /// Information about the back button to display
    /// </summary>
    public class BackButtonInfo
    {
        public bool ShouldDisplay { get; set; }
        public string Url { get; set; } = string.Empty;
        public string IconClass { get; set; } = "fas fa-arrow-left";
        public string Text { get; set; } = "Back";
    }

    /// <summary>
    /// Interface for navigation helper service
    /// </summary>
    public interface INavigationHelper
    {
        BackButtonInfo GetBackButtonInfo(ViewDataDictionary viewData, RouteData routeData, string? referer, IUrlHelper urlHelper);
    }

    /// <summary>
    /// Helper service for determining back button navigation
    /// Leverages existing breadcrumb logic and route parameters
    /// </summary>
    public class NavigationHelper : INavigationHelper
    {
        public BackButtonInfo GetBackButtonInfo(ViewDataDictionary viewData, RouteData routeData, string? referer, IUrlHelper urlHelper)
        {
            var controller = routeData.Values["controller"]?.ToString() ?? string.Empty;
            var action = routeData.Values["action"]?.ToString() ?? string.Empty;
            var breadcrumbLevel = viewData["breadcrumbLevel"] as string ?? string.Empty;

            // Extract route parameters from ViewData (same as breadcrumb logic)
            var frameworkCode = viewData["frameworkCode"] as int?;
            var outcomeCode = viewData["outcomeCode"] as int?;
            var outputCode = viewData["outputCode"] as int?;
            var subOutputCode = viewData["subOutputCode"] as int?;

            // Try hierarchical navigation first (based on breadcrumbLevel)
            var hierarchicalBack = GetHierarchicalBackButton(breadcrumbLevel, frameworkCode, outcomeCode, outputCode, subOutputCode, urlHelper);
            if (hierarchicalBack != null)
            {
                return hierarchicalBack;
            }

            // Try fallback map for non-hierarchical pages
            var fallbackBack = GetFallbackBackButton(controller, action, routeData, viewData, urlHelper);
            if (fallbackBack != null)
            {
                return fallbackBack;
            }

            // Use referer as last resort
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                // Only use referer if it's from the same host (security)
                var currentHost = routeData.Values["host"]?.ToString();
                if (string.IsNullOrEmpty(currentHost) || refererUri.Host.Contains("localhost") || refererUri.Host.Contains("127.0.0.1"))
                {
                    return new BackButtonInfo
                    {
                        ShouldDisplay = true,
                        Url = referer,
                        Text = "Back"
                    };
                }
            }

            // No back button for top-level or unknown pages
            return new BackButtonInfo { ShouldDisplay = false };
        }

        /// <summary>
        /// Determine back button for hierarchical pages using breadcrumb logic
        /// </summary>
        private BackButtonInfo? GetHierarchicalBackButton(string breadcrumbLevel, int? frameworkCode, int? outcomeCode, int? outputCode, int? subOutputCode, IUrlHelper urlHelper)
        {
            switch (breadcrumbLevel.ToLower())
            {
                case "framework":
                    // Framework level - back to Frameworks list
                    return new BackButtonInfo
                    {
                        ShouldDisplay = true,
                        Url = urlHelper.Action("Index", "Frameworks") ?? "/Frameworks",
                        Text = "Back"
                    };

                case "outcome":
                    // Outcome level - back to Outcomes list (within framework)
                    if (frameworkCode.HasValue)
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Index", "Outcomes", new { frameworkCode }) ?? $"/Outcomes?frameworkCode={frameworkCode}",
                            Text = "Back"
                        };
                    }
                    break;

                case "output":
                    // Output level - back to Outputs list (within outcome)
                    if (frameworkCode.HasValue && outcomeCode.HasValue)
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Index", "Outputs", new { frameworkCode, outcomeCode }) ?? $"/Outputs?frameworkCode={frameworkCode}&outcomeCode={outcomeCode}",
                            Text = "Back"
                        };
                    }
                    break;

                case "suboutput":
                    // SubOutput level - back to SubOutputs list (within output)
                    if (frameworkCode.HasValue && outputCode.HasValue)
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Index", "SubOutputs", new { frameworkCode, outputCode }) ?? $"/SubOutputs?frameworkCode={frameworkCode}&outputCode={outputCode}",
                            Text = "Back"
                        };
                    }
                    break;

                case "indicators":
                case "indicator":
                    // Indicators level - back to Indicators list (within suboutput)
                    if (frameworkCode.HasValue && subOutputCode.HasValue)
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Index", "Indicators", new { frameworkCode, subOutputCode }) ?? $"/Indicators?frameworkCode={frameworkCode}&subOutputCode={subOutputCode}",
                            Text = "Back"
                        };
                    }
                    break;
            }

            return null;
        }

        /// <summary>
        /// Fallback navigation map for non-hierarchical pages
        /// </summary>
        private BackButtonInfo? GetFallbackBackButton(string controller, string action, RouteData routeData, ViewDataDictionary viewData, IUrlHelper urlHelper)
        {
            var controllerAction = $"{controller}.{action}";

            switch (controllerAction.ToLower())
            {
                // Projects
                case "projects.details":
                    return new BackButtonInfo
                    {
                        ShouldDisplay = true,
                        Url = urlHelper.Action("Index", "Projects") ?? "/Projects",
                        Text = "BackToList"
                    };

                case "projects.edit":
                case "projects.create":
                    var projectId = routeData.Values["id"]?.ToString();
                    if (!string.IsNullOrEmpty(projectId))
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Details", "Projects", new { id = projectId }) ?? $"/Projects/Details/{projectId}",
                            Text = "Back"
                        };
                    }
                    else
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Index", "Projects") ?? "/Projects",
                            Text = "BackToList"
                        };
                    }

                // ActionPlans
                case "actionplans.actionplan":
                    var actionPlanProjectId = viewData["ProjectID"]?.ToString();
                    if (!string.IsNullOrEmpty(actionPlanProjectId))
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Details", "Projects", new { id = actionPlanProjectId }) ?? $"/Projects/Details/{actionPlanProjectId}",
                            Text = "Back"
                        };
                    }
                    break;

                // Measures
                case "measures.index":
                case "measures.create":
                case "measures.edit":
                    var frameworkCode = viewData["frameworkCode"] as int?;
                    var subOutputCode = viewData["subOutputCode"] as int?;
                    if (frameworkCode.HasValue && subOutputCode.HasValue)
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Index", "Indicators", new { frameworkCode, subOutputCode }) ?? $"/Indicators?frameworkCode={frameworkCode}&subOutputCode={subOutputCode}",
                            Text = "Back"
                        };
                    }
                    break;

                // Activities
                case "activities.index":
                case "activities.create":
                case "activities.edit":
                    var activityProjectId = viewData["ProjectID"]?.ToString() ?? routeData.Values["projectId"]?.ToString();
                    if (!string.IsNullOrEmpty(activityProjectId))
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Details", "Projects", new { id = activityProjectId }) ?? $"/Projects/Details/{activityProjectId}",
                            Text = "Back"
                        };
                    }
                    break;

                // Plans
                case "plans.index":
                case "plans.create":
                case "plans.edit":
                    var planProjectId = viewData["ProjectID"]?.ToString() ?? routeData.Values["projectId"]?.ToString();
                    if (!string.IsNullOrEmpty(planProjectId))
                    {
                        return new BackButtonInfo
                        {
                            ShouldDisplay = true,
                            Url = urlHelper.Action("Details", "Projects", new { id = planProjectId }) ?? $"/Projects/Details/{planProjectId}",
                            Text = "Back"
                        };
                    }
                    break;

                // Admin/Setup pages - back to respective index
                case "ministries.create":
                case "ministries.edit":
                    return new BackButtonInfo
                    {
                        ShouldDisplay = true,
                        Url = urlHelper.Action("Index", "Ministries") ?? "/Ministries",
                        Text = "BackToList"
                    };

                case "sectors.create":
                case "sectors.edit":
                    return new BackButtonInfo
                    {
                        ShouldDisplay = true,
                        Url = urlHelper.Action("Index", "Sectors") ?? "/Sectors",
                        Text = "BackToList"
                    };

                case "donors.create":
                case "donors.edit":
                    return new BackButtonInfo
                    {
                        ShouldDisplay = true,
                        Url = urlHelper.Action("Index", "Donors") ?? "/Donors",
                        Text = "BackToList"
                    };

                // Top-level pages - no back button
                case "frameworks.index":
                case "dashboard.index":
                case "home.index":
                case "monitoring.index":
                case "projects.index":
                case "ministries.index":
                case "sectors.index":
                case "donors.index":
                    return new BackButtonInfo { ShouldDisplay = false };
            }

            return null;
        }
    }
}
