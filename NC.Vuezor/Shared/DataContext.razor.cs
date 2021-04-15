using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NC.Vuezor.Shared
{
    public partial class DataContext<TData> where TData : VueVM, new()
    {


        ElementReference VueContextId;

        [Parameter]
        public RenderFragment<TData> ChildContent { get; set; }

        [Parameter]
        public TData DataInstance
        {
            get => _DataInstance;
            set
            {
                if (_DataInstance != null)
                {
                    throw new InvalidOperationException("DataInstance can only be set once");
                }

                _DataInstance = value;
            }
        }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        /// <summary>
        /// Reference to the Data Instance
        /// </summary>
        private DotNetObjectReference<TData> _DataRef;
        private TData _DataInstance;


        /// <summary>
        /// Reference to client data context
        /// </summary>
        IJSObjectReference _DataContextClientRef;

        protected override void OnInitialized()
        {
            if (_DataInstance == null)
            {
                _DataInstance = new();
            }
            _DataRef = DotNetObjectReference.Create(_DataInstance);

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender == false)
            {
                return;
            }

            var module = await this.JSRuntime.InvokeAsync<IJSObjectReference>("import", "/DataContext.razor.js");
            _DataContextClientRef = await module.InvokeAsync<IJSObjectReference>("VuezorDataContext", this.VueContextId, true, _DataInstance.PrepareForVue(), _DataRef, false);
        }

    }
}
