using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Templates
{
    public class AdminTemplatesPermissions : IPermissionProvider
    {
        public static readonly Permission ManageAdminTemplates = new Permission("ManageAdminTemplates", "Manage admin templates");

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(new[] { ManageAdminTemplates }.AsEnumerable());
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ManageAdminTemplates }
                }
            };
        }
    }
}
