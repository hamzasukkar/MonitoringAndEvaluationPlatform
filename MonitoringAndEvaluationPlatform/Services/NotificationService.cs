using Microsoft.AspNetCore.Mvc;

namespace MonitoringAndEvaluationPlatform.Services
{
    public interface INotificationService
    {
        void AddSuccessMessage(Controller controller, string message);
        void AddErrorMessage(Controller controller, string message);
        void AddWarningMessage(Controller controller, string message);
        void AddInfoMessage(Controller controller, string message);
    }

    public class NotificationService : INotificationService
    {
        public void AddSuccessMessage(Controller controller, string message)
        {
            controller.TempData["SuccessMessage"] = message;
        }

        public void AddErrorMessage(Controller controller, string message)
        {
            controller.TempData["ErrorMessage"] = message;
        }

        public void AddWarningMessage(Controller controller, string message)
        {
            controller.TempData["WarningMessage"] = message;
        }

        public void AddInfoMessage(Controller controller, string message)
        {
            controller.TempData["InfoMessage"] = message;
        }
    }
}

public static class NotificationExtensions
{
    public static void SetSuccessMessage(this Controller controller, string message)
    {
        controller.TempData["SuccessMessage"] = message;
    }

    public static void SetErrorMessage(this Controller controller, string message)
    {
        controller.TempData["ErrorMessage"] = message;
    }

    public static void SetWarningMessage(this Controller controller, string message)
    {
        controller.TempData["WarningMessage"] = message;
    }

    public static void SetInfoMessage(this Controller controller, string message)
    {
        controller.TempData["InfoMessage"] = message;
    }
}