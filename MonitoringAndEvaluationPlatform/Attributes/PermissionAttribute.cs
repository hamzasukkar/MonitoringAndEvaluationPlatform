using Microsoft.AspNetCore.Authorization;

namespace MonitoringAndEvaluationPlatform.Attributes
{
    public class PermissionAttribute : AuthorizeAttribute
    {
        public PermissionAttribute(string permission) : base(permission)
        {

        }
    }
}