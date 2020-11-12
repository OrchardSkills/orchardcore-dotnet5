using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;

namespace OrchardCore.Contents.ViewModels
{
    public class ContentOptionsViewModel
    {
        public ContentOptionsViewModel()
        {
            OrderBy = ContentsOrder.Modified;
            BulkAction = ViewModels.ContentsBulkAction.None;
            ContentsStatus = ContentsStatus.Latest;
        }

        public string DisplayText { get; set; }

        public string SelectedContentType { get; set; }

        public bool CanCreateSelectedContentType { get; set; }

        public ContentsOrder OrderBy { get; set; }

        public ContentsStatus ContentsStatus { get; set; }

        public ContentsBulkAction BulkAction { get; set; }

        #region Values to populate

        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public int ContentItemsCount { get; set; }
        public int TotalItemCount { get; set; }
        public RouteValueDictionary RouteValues { get; set; } = new RouteValueDictionary();

        #endregion

        #region Lists to populate

        [BindNever]
        public List<SelectListItem> ContentStatuses { get; set; }

        [BindNever]
        public List<SelectListItem> ContentSorts { get; set; }

        [BindNever]
        public List<SelectListItem> ContentsBulkAction { get; set; }

        [BindNever]
        public List<SelectListItem> ContentTypeOptions { get; set; }

        [BindNever]
        public List<SelectListItem> CreatableTypes { get; set; }

        #endregion Lists to populate
    }

    public enum ContentsOrder
    {
        Modified,
        Published,
        Created,
        Title,
    }

    public enum ContentsStatus
    {
        Draft,
        Published,
        AllVersions,
        Latest,
        Owner
    }

    public enum ContentsBulkAction
    {
        None,
        PublishNow,
        Unpublish,
        Remove
    }
}
