using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NC.Vuezor
{
    public abstract class VueVM
    {
        private Dictionary<string, MethodInfo> _Setters = new();
        private Dictionary<string, MethodInfo> _Getters = new();
        private Dictionary<string, MethodInfo> _Methods = new();
        
        /// <summary>
        /// Whether this VM can sync one way (i.e. there is no side-effect from settings property values)
        /// </summary>
        protected abstract bool IsOnewaySyncSupported { get; }

        [JSInvokable]
        public object Getter(string property)
        {
            MethodInfo m;
            if (this._Getters.TryGetValue(property, out m))
            {
                return m?.Invoke(this, new object[0]);
            }

            return null;
        }

        [JSInvokable]
        public string Setter(string property, string jsonValue)
        {
            this.SetterPure(property, jsonValue);
            return JsonConvert.SerializeObject(this);
        }

        [JSInvokable]
        public void SetterPure(string property, string jsonValue)
        {
            MethodInfo m;
            if (this._Setters.TryGetValue(property, out m))
            {
                var value = JToken.Parse(jsonValue).ToObject(m.GetParameters()[0].ParameterType);
                m?.Invoke(this, new object[] { value });
            }
        }

        [JSInvokable]
        public string InvokeMethod(string methodName, string currentSate)
        {
            this.InvokeMethodPure(methodName, currentSate);
            return JsonConvert.SerializeObject(this);
        }

        [JSInvokable]
        public void InvokeMethodPure(string methodName, string currentSate)
        {
            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            JsonConvert.PopulateObject(currentSate, this, settings);

            MethodInfo m;
            if (this._Methods.TryGetValue(methodName, out m))
            {
                m?.Invoke(this, new object[0]);
            }
        }

        [JSInvokable]
        public string Synchronize(string currentSate)
        {
            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            JsonConvert.PopulateObject(currentSate, this, settings);

            return JsonConvert.SerializeObject(this);
        }

        [JSInvokable]
        public void SynchronizeOneway(string currentSate)
        {
            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            JsonConvert.PopulateObject(currentSate, this, settings);
        }

        public string Mustache( string prop )
        {
            return "{{" + prop + "}}";
        }

        /// <summary>
        /// Create JSON to be passed to Vuezor client-side
        /// </summary>
        /// <returns></returns>
        public string PrepareForVue()
        {
            var toReturn = new JObject();

            var methods = new JArray();
            foreach (var method in this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.GetCustomAttribute<VueMethodAttribute>() is VueMethodAttribute vuemethod)
                {
                    methods.Add( JObject.FromObject( new
                    {
                        pure = vuemethod.IsNoSideEffect,
                        name = method.Name
                    }));

                    _Methods[method.Name] = method;
                }
            }

            var data = new JObject();
            var computed = new JArray();
            var watch = new JObject();

            foreach (var p in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.Name == "Getter" || p.Name == "Setter")
                {
                    continue;
                }

                if (p.GetCustomAttribute<VueDataAttribute>() is VueDataAttribute vd)
                {
                    var value = p.GetValue(this);
                    data[p.Name] = value != null ? JToken.FromObject(p.GetValue(this)) : null;

                    if (vd.Watch)
                    {
                        if (p.PropertyType.IsArray || p.PropertyType.IsClass)
                        {
                            watch[p.Name] = JObject.FromObject(new { deep = true, pure = vd.IsNoSideEffect });
                        }
                        else
                        {
                            watch[p.Name] = JObject.FromObject(new { deep = false, pure = vd.IsNoSideEffect });
                        }

                        _Setters[p.Name] = p.GetSetMethod();
                    }

                }

                if (p.GetCustomAttribute<VueComputedAttribute>() is VueComputedAttribute v)
                {
                    if (data[p.Name] != null)
                    {
                        data.Remove(p.Name);
                    }

                    var value = p.GetValue(this);
                    data["computed__" + p.Name] = value != null ? JToken.FromObject(p.GetValue(this)) : null;

                    computed.Add(JObject.FromObject(new
                    {
                        pure = v.IsNoSideEffect,
                        getter = v.GetterJS,
                        name = p.Name,
                    }));

                    _Setters[p.Name] = p.GetSetMethod();
                    _Getters[p.Name] = p.GetGetMethod();
                }
            }

            toReturn["data"] = data;
            toReturn["computed"] = computed;
            toReturn["methods"] = methods;
            toReturn["watch"] = watch;
            toReturn["onewaysync"] = this.IsOnewaySyncSupported;

            return ((JObject)toReturn).ToString();
        }
    }

    public struct WatchType
    {
        public bool deep { get; set; }
        public string toWatch { get; set; }

        public WatchType( string toWatch, bool deep = false)
        {
            this.deep = deep;
            this.toWatch = toWatch;
        }
    }

    /// <summary>
    /// Specify that the property is generated as vue property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class VueComputedAttribute : Attribute {

        /// <summary>
        /// JS for Getter (todo: compile from C# to this automatically)
        /// </summary>
        public string GetterJS { get; set; }

        /// <summary>
        /// whether the setter will cause no side-effect
        /// </summary>
        public bool IsNoSideEffect { get; set; } = false;
    }

    /// <summary>
    /// Specify that the property is generated as vue field (not getter/setter)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class VueDataAttribute : Attribute
    {
        /// <summary>
        /// Whether changes to this property will cause setter of this property to be called
        /// </summary>
        public bool Watch { get; set; }

        /// <summary>
        /// Whether changes to this property will not cause other Vue visible property to change
        /// </summary>
        public bool IsNoSideEffect { get; set; } = false;
    }

    /// <summary>
    /// Specify that the method can be called from Vue
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class VueMethodAttribute : Attribute
    {
        /// <summary>
        /// whether the method  will not cause other Vue visible property to change
        /// </summary>
        public bool IsNoSideEffect { get; set; } = false;
    }

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

        [VueData]
        public int counter { get; set; }

        [VueData]
        public string firstName { get; set; } = "Jirawat";

        [VueData]
        public string lastName { get; set; } = "P.";

        [VueData(Watch = true, IsNoSideEffect = true)]
        public List<Todo> TodoItems { get; set; } = new();

        [VueData]
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

        [VueMethod]
        public void Increment()
        {
            this.counter++;
        }

        [VueMethod(IsNoSideEffect = true)]
        public void Pure()
        {
            Debug.WriteLine("Pure Method");
        }

        [VueMethod]
        public void AddItem()
        {
            this.TodoItems.Add(this.NewItem);
            this.NewItem = new();
        }
    }
}
