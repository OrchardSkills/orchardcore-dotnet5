using System.Threading.Tasks;
using OrchardCore.Contents.ViewModels;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace OrchardCore.Contents.Drivers
{
    public class ContentOptionsDisplayDriver : DisplayDriver<ContentOptionsViewModel>
    {
        // Maintain the Options prefix for compatability with binding.
        protected override void BuildPrefix(ContentOptionsViewModel model, string htmlFieldPrefix)
        {
            Prefix = "Options";
        }

        public override IDisplayResult Display(ContentOptionsViewModel model)
        {
            return Initialize<ContentOptionsViewModel>("ContentsAdminList__BulkActions", m => BuildContentOptionsViewModel(m, model)).Location("BulkActions", "Content:10");
        }

        public override IDisplayResult Edit(ContentOptionsViewModel model)
        {
            return Combine(
                Initialize<ContentOptionsViewModel>("ContentsAdminList__Search", m => BuildContentOptionsViewModel(m, model)).Location("Search:10"),
                Initialize<ContentOptionsViewModel>("ContentsAdminList__Create", m => BuildContentOptionsViewModel(m, model)).Location("Create:10"),
                Initialize<ContentOptionsViewModel>("ContentsAdminList__Summary", m => BuildContentOptionsViewModel(m, model)).Location("Summary:10"),
                Initialize<ContentOptionsViewModel>("ContentsAdminList__Filters", m => BuildContentOptionsViewModel(m, model)).Location("Actions:10.1"),
                Initialize<ContentOptionsViewModel>("ContentsAdminList_Fields_BulkActions", m => BuildContentOptionsViewModel(m, model)).Location("Actions:10.1")
            );
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentOptionsViewModel model, IUpdateModel updater)
        {
            var viewModel = new ContentOptionsViewModel();
            if (await updater.TryUpdateModelAsync(viewModel, Prefix))
            {
                model.RouteValues.TryAdd("Options.OrderBy", viewModel.OrderBy);
                model.RouteValues.TryAdd("Options.ContentsStatus", viewModel.ContentsStatus);
                model.RouteValues.TryAdd("Options.SelectedContentType", viewModel.SelectedContentType);
                model.RouteValues.TryAdd("Options.DisplayText", viewModel.DisplayText);
            }

            return Edit(model);
        }

        private static void BuildContentOptionsViewModel(ContentOptionsViewModel m, ContentOptionsViewModel model)
        {
            m.ContentTypeOptions = model.ContentTypeOptions;
            m.ContentStatuses = model.ContentStatuses;
            m.ContentSorts = model.ContentSorts;
            m.ContentsBulkAction = model.ContentsBulkAction;
            m.CreatableTypes = model.CreatableTypes;
            m.StartIndex = model.StartIndex;
            m.EndIndex = model.EndIndex;
            m.ContentItemsCount = model.ContentItemsCount;
            m.TotalItemCount = model.TotalItemCount;
        }
    }
}
