﻿#if !NETSTANDARD2_1 && !NET5_0
using GridShared.Filtering;
using GridShared.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GridBlazor.Pages
{
    public partial class TimeOnlyFilterComponent<T>
    {
        protected bool _clearVisible = false;
        protected Filter[] _filters;
        protected string _condition;
        protected int _offset = 0;
        protected bool _init = false;
        protected bool _initPosition = false;

        protected ElementReference timeOnlyFilter;
        protected ElementReference firstSelect;

        [Inject]
        private IJSRuntime jSRuntime { get; set; }

        [CascadingParameter(Name = "GridHeaderComponent")]
        private GridHeaderComponent<T> GridHeaderComponent { get; set; }

        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public string ColumnName { get; set; }

        [Parameter]
        public IEnumerable<ColumnFilterValue> FilterSettings { get; set; }

        protected override void OnParametersSet()
        {
            // Remove 'z' at the end of values if grid uses an OData back-end      
            if (GridHeaderComponent.GridComponent.Grid.ServerAPI == ServerAPI.OData)
            {
                var filters = FilterSettings.ToArray();
                for (int i = 0; i < filters.Count(); i++)
                {
                    if (filters[i].FilterType != GridFilterType.Condition)
                    {
                        var timeOnly = TimeOnly.Parse(filters[i].FilterValue);
                        filters[i].FilterValue = timeOnly.ToString("HH':'mm");
                    }
                }
                FilterSettings = filters.ToList();
            }
            _condition = FilterSettings.SingleOrDefault(r => r != ColumnFilterValue.Null
                && r.FilterType == GridFilterType.Condition).FilterValue;
            if (string.IsNullOrWhiteSpace(_condition))
                _condition = GridFilterCondition.And.ToString("d");

            var filterSettings = FilterSettings.Where(r => r != ColumnFilterValue.Null
               && r.FilterType != GridFilterType.Condition).Select(r =>
                   new Filter(r.FilterType.ToString("d"), r.FilterValue)).ToList();
            _clearVisible = filterSettings.Count() > 0;
            if (filterSettings.Count() == 0)
                filterSettings.Add(new Filter(GridFilterType.Equals.ToString("d"), ""));
            _filters = filterSettings.ToArray();
            _init = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_initPosition && firstSelect.Id != null && timeOnlyFilter.Id != null)
            {
                await jSRuntime.InvokeVoidAsync("gridJsFunctions.focusElement", firstSelect);
                ScreenPosition sp = await jSRuntime.InvokeAsync<ScreenPosition>("gridJsFunctions.getPosition", timeOnlyFilter);
                ScreenPosition gridTableSP = await jSRuntime.InvokeAsync<ScreenPosition>("gridJsFunctions.getPosition", 
                    GridHeaderComponent.GridComponent.GridTable);
                if (sp != null && gridTableSP != null)
                {
                    if (gridTableSP.X + gridTableSP.Width < sp.X + sp.Width)
                    {
                        _offset = gridTableSP.X + gridTableSP.Width - sp.X - sp.Width;
                        StateHasChanged();
                    }
                    else if (sp.X < gridTableSP.X)
                    {
                        _offset = gridTableSP.X - sp.X;
                        StateHasChanged();
                    }
                }
                _initPosition = true;
            }
        }

        protected void AddColumnFilterValue()
        {
            Array.Resize(ref _filters, _filters.Length + 1);
            _filters[_filters.Length - 1] = new Filter(GridFilterType.Equals.ToString("d"), "");
        }

        protected void RemoveColumnFilterValue()
        {
            if (_filters.Length > 1)
            {
                Array.Resize(ref _filters, _filters.Length - 1);
            }
        }

        protected async Task ApplyButtonClicked()
        {
            FilterCollection filters = new FilterCollection(_filters);
            
            // Add 'z' at the end of values if grid uses an OData back-end
            if (GridHeaderComponent.GridComponent.Grid.ServerAPI == ServerAPI.OData)
            {
                foreach (var filter in filters)
                {
                    TimeOnly TimeOnly = TimeOnly.Parse(filter.Value);
                    filter.Value = ODataTimeOnlyConverter.ToTimeOnlyString(TimeOnly);
                }
            }
            
            if(filters.Count() > 1)
                filters.Add(GridFilterType.Condition.ToString("d"), _condition);
            await GridHeaderComponent.AddFilter(filters);
        }

        protected async Task ClearButtonClicked()
        {
            await GridHeaderComponent.RemoveFilter();
        }

        public async Task FilterKeyup(KeyboardEventArgs e)
        {
            if (e.Key == "Escape")
            {
                await GridHeaderComponent.FilterIconClicked();
            }
        }
    }
}
#endif
