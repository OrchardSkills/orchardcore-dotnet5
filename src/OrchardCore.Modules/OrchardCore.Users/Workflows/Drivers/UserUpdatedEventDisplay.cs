using OrchardCore.DisplayManagement.Views;
using OrchardCore.Users.Services;
using OrchardCore.Users.Workflows.Activities;
using OrchardCore.Users.Workflows.ViewModels;
using OrchardCore.Workflows.Display;

namespace OrchardCore.Users.Workflows.Drivers
{
    public class UserUpdatedEventDisplay : ActivityDisplayDriver<UserUpdatedEvent, UserUpdatedEventViewModel>
    {
        public UserUpdatedEventDisplay(IUserService userService)
        {
            UserService = userService;
        }

        protected IUserService UserService { get; }

        protected override void EditActivity(UserUpdatedEvent source, UserUpdatedEventViewModel target)
        {
        }

        public override IDisplayResult Display(UserUpdatedEvent activity)
        {
            return Combine(
                Shape("UserUpdatedEvent_Fields_Thumbnail", new UserUpdatedEventViewModel(activity)).Location("Thumbnail", "Content"),
                Factory("UserUpdatedEvent_Fields_Design", ctx =>
                {
                    var shape = new UserUpdatedEventViewModel();
                    shape.Activity = activity;

                    return shape;
                }).Location("Design", "Content")
            );
        }
    }
}
