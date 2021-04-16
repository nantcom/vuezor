using Microsoft.JSInterop;
using NC.Blazor.Vuezor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NC.Vuezor.Sample
{
    public class Todo
    {
        private DateTime _lastUpdated;

        public bool Completed { get; set; }

        public string ToDo { get; set; }

        public string LastUpdated
        {
            get
            {
                return _lastUpdated.ToString();
            }

        }
    }

    public class SampleVM : VueVM
    {
        protected override bool IsOnewaySyncSupported => false;

        public int counter { get; set; }

        public string firstName { get; set; } = "Jirawat";

        public string lastName { get; set; } = "P.";

        [VueData(Watch = true, IsNoSideEffect = true)]
        public List<Todo> TodoItems { get; set; } = new();

        public Todo NewItem { get; set; } = new();

        [VueComputed]
        public string Name
        {
            get
            {
                return this.firstName + "  " + this.lastName;
            }
            set
            {

                if (string.IsNullOrEmpty(value))
                {
                    this.firstName = string.Empty;
                    this.lastName = string.Empty;
                    return;
                }

                if (value.Contains(" " ) == false)
                {
                    this.firstName = value;
                    return;
                }

                var parts = value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                this.firstName = parts[0];

                if (parts.Length == 2)
                {
                    this.lastName = parts[1];
                }
                else
                {
                    this.lastName = null;
                }
            }
        }

        public void Increment()
        {
            this.counter++;
        }

        [VueMethod(IsNoSideEffect = true)]
        public void Pure()
        {
            Debug.WriteLine("Pure Method");
        }

        [VueMethod(Affected = new string[] { "NewItem", "TodoItems" })]
        public void AddItem()
        {
            this.TodoItems.Add(this.NewItem);
            this.NewItem = new();
        }
    }
}
