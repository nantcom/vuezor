using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NC.Blazor.Vuezor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NC.Blazor
{
    public partial class VueApp<TData> where TData : VueVM, new()
    {
        ElementReference VueContextId;

        /// <summary>
        /// Vue Template to be rendered
        /// </summary>
        [Parameter]
        public RenderFragment<TData> ChildContent { get; set; }

        /// <summary>
        /// List of Vue components to include.
        /// The string must be either: 1) imported module name such as "mdb" for MDB Boostrap, 2) string which eval to component instance such as "mdb.MDBBtn".
        /// Component names are registered in lower case format using either name property of the component if specified. Or the string which was evaled to component instance (such as mdb.MDBBtn will be registered as mdbbtn).
        /// </summary>
        [Parameter]
        public string[] VueComponents { get; set; }

        /// <summary>
        /// Whether the component will log its output to browser console
        /// </summary>
        [Parameter]
        public bool IsLoggingEnabled { get; set; } = false;

        /// <summary>
        /// Whether the component will use local copy of vue. If set to false, Vuejs package will be loaded from cdnjs
        /// </summary>
        [Parameter]
        public bool IsUseLocalVueJS { get; set; } = false;

        /// <summary>
        /// Script to be executed (using eval) before
        /// </summary>
        [Parameter]
        public string IncludeJS { get; set; } = null;

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
#if DEBUG
            this.IsLoggingEnabled = true;
#endif

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



            var module = await this.JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/NC-Blazor.Vuezor/VueApp.razor.js");
            _DataContextClientRef = await module.InvokeAsync<IJSObjectReference>("VuezorDataContext",
                this.VueContextId,
                this.IsUseLocalVueJS,
                _DataInstance.GetVueVMJson(),
                _DataRef,
                this.IsLoggingEnabled);
        }

    }
}
