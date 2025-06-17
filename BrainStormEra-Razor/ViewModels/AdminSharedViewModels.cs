using System.ComponentModel.DataAnnotations;

namespace BrainStormEra_Razor.ViewModels
{
    // Admin Header Component ViewModel
    public class AdminHeaderViewModel
    {
        public string Title { get; set; } = "Admin Dashboard";
        public string Subtitle { get; set; } = "Comprehensive analytics and system overview";
        public string Icon { get; set; } = "fa-chart-line";
        public string AdminName { get; set; } = "Admin";
        public string AdminImage { get; set; } = "/SharedMedia/defaults/default-avatar.svg";
        public DateTime LoginTime { get; set; } = DateTime.Now;
        public bool ShowQuickLinks { get; set; } = true;
        public bool ShowRefresh { get; set; } = true;
        public bool ShowExport { get; set; } = true;
        public bool ShowBackButton { get; set; } = false;
        public string BackUrl { get; set; } = "/admin";
    }

    // Statistics Cards Component ViewModel
    public class StatCardViewModel
    {
        public string Value { get; set; } = "0";
        public string Label { get; set; } = "";
        public string CssClass { get; set; } = "";
        public string? Change { get; set; }
        public bool IsPositive { get; set; } = true;
        public string? Icon { get; set; }
    }

    // Filter Section Component ViewModels
    public enum FilterType
    {
        Text,
        Select
    }

    public class FilterOptionViewModel
    {
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public class FilterViewModel
    {
        public string Name { get; set; } = "";
        public string Label { get; set; } = "";
        public FilterType Type { get; set; } = FilterType.Text;
        public string? Value { get; set; }
        public string? Placeholder { get; set; }
        public List<FilterOptionViewModel> Options { get; set; } = new List<FilterOptionViewModel>();
    }

    public class FilterSectionViewModel
    {
        public string ActionUrl { get; set; } = "";
        public string ClearUrl { get; set; } = "";
        public List<FilterViewModel> Filters { get; set; } = new List<FilterViewModel>();
    }

    // Pagination Component ViewModel
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string BaseUrl { get; set; } = "";
        public Dictionary<string, string?> QueryParameters { get; set; } = new Dictionary<string, string?>();

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public string GetPageUrl(int page)
        {
            var parameters = new List<string>();

            foreach (var param in QueryParameters)
            {
                if (!string.IsNullOrEmpty(param.Value) && param.Key.ToLower() != "currentpage")
                {
                    parameters.Add($"{param.Key}={Uri.EscapeDataString(param.Value)}");
                }
            }

            parameters.Add($"CurrentPage={page}");

            var queryString = string.Join("&", parameters);
            return string.IsNullOrEmpty(queryString) ? $"{BaseUrl}?CurrentPage={page}" : $"{BaseUrl}?{queryString}";
        }
    }

    // Page Header Component ViewModels
    public class PageActionViewModel
    {
        public string Text { get; set; } = "";
        public string Url { get; set; } = "";
        public string Icon { get; set; } = "";
        public string CssClass { get; set; } = "btn-filter";
    }

    public class PageHeaderViewModel
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public List<PageActionViewModel> Actions { get; set; } = new List<PageActionViewModel>();
    }
}