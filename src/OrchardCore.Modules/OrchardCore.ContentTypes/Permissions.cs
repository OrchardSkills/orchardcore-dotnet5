using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace OrchardCore.ContentTypes
{
    public class Permissions : IPermissionProvider
    {
        public static readonly Permission ViewContentTypes = new Permission("ViewContentTypes", "View content types.");
        public static readonly Permission EditContentTypes = new Permission("EditContentTypes", "Edit content types.");

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(GetPermissions());
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[] {
                new PermissionStereotype {
                    Name = "Administrator",
                    Permissions = GetPermissions()
                }
            };
        }

        private IEnumerable<Permission> GetPermissions()
        {
            return new[]
            {
                ViewContentTypes,
                EditContentTypes
            };
        }
    }
}
