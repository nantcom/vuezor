using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NC.Blazor.Vuezor
{

    public abstract class VueVM
    {
        private class VueVMData
        {
            public Dictionary<string, MethodInfo> Setters = new();
            public Dictionary<string, MethodInfo> Getters = new();
            public Dictionary<string, MethodInfo> Methods = new();
            public Dictionary<string, string[]> MethodsSideEffects = new();
            public Dictionary<string, VueDataAttribute> PropertyInfo = new();
            public Dictionary<string, VueComputedAttribute> ComputedPropertyInfo = new();

            public string VueVMJson;
        }

        private static Dictionary<Type, VueVMData> _VueVMDataCache = new();

        private VueVMData VMData
        {
            get
            {
                VueVMData result;
                if (_VueVMDataCache.TryGetValue(this.GetType(), out result) == false)
                {
                    result = this.PrepareVueVMData();
                    _VueVMDataCache[this.GetType()] = result;
                }

                return result;
            }
        }

        /// <summary>
        /// Get JSON for initializing Vuezor
        /// </summary>
        /// <returns></returns>
        public string GetVueVMJson()
        {
            return this.VMData.VueVMJson;
        }

        /// <summary>
        /// Whether this VM can sync one way (i.e. there is no side-effect from settings property values)
        /// </summary>
        protected abstract bool IsOnewaySyncSupported { get; }

        /// <summary>
        /// Get JSON containing only changed properties
        /// </summary>
        /// <param name="oldJson"></param>
        /// <returns></returns>
        private string GetJSONChanges(string oldJson, string stateFromClient = null, string[] keep = null)
        {
            var oldObject = JObject.Parse(oldJson);
            var serverObject = JObject.FromObject(this);

            if (oldJson == serverObject.ToString())
            {
                return "{}";
            }

            HashSet<string> affected = new();
            if (keep != null)
            {
                foreach (var key in keep)
                {
                    affected.Add(key);
                }
            }

            // remove properties that are same
            foreach (var prop in serverObject.Properties().ToList())
            {
                if (affected.Contains(prop.Name))
                {
                    continue;
                }

                var oldProp = oldObject.Property(prop.Name);

                if (prop.ToString() == oldProp.ToString())
                {
                    serverObject.Remove(prop.Name);
                    continue;
                }
            }

            // if there is a change in computed property, change key to "computed__"
            foreach (var key in this.VMData.ComputedPropertyInfo.Keys)
            {
                var prop = serverObject.Property(key);
                if (prop != null)
                {
                    serverObject.Remove(key);
                    serverObject[$"computed__{key}"] = prop.Value;
                }
            }

            // also try to merge with state from client too
            // (which will have computed__key here
            if (stateFromClient != null)
            {
                var clientState = JObject.Parse(stateFromClient);
                foreach (var clientProp in clientState.Properties().ToList())
                {
                    var serverProp = serverObject.Property(clientProp.Name);

                    if (serverProp != null &&
                        serverProp.ToString() == clientProp.ToString())
                    {
                        if (affected.Contains(serverProp.Name))
                        {
                            continue;
                        }

                        serverObject.Remove(serverProp.Name);
                        continue;
                    }
                }
            }

            return serverObject.ToString();
        }

        [JSInvokable]
        public object Getter(string property)
        {
            MethodInfo m;
            if (this.VMData.Getters.TryGetValue(property, out m))
            {
                return m?.Invoke(this, new object[0]);
            }

            return null;
        }

        [JSInvokable]
        public string Setter(string property, string jsonValue)
        {
            var oldJson = JsonConvert.SerializeObject(this);

            this.SetterPure(property, jsonValue);

            return this.GetJSONChanges(oldJson);
        }

        [JSInvokable]
        public void SetterPure(string property, string jsonValue)
        {
            MethodInfo m;
            if (this.VMData.Setters.TryGetValue(property, out m))
            {
                var value = JToken.Parse(jsonValue).ToObject(m.GetParameters()[0].ParameterType);
                m?.Invoke(this, new object[] { value });
            }
        }

        [JSInvokable]
        public string InvokeMethod(string methodName, string clientState)
        {
            var oldJson = JsonConvert.SerializeObject(this);

            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            JsonConvert.PopulateObject(clientState, this, settings);

            MethodInfo m;
            if (this.VMData.Methods.TryGetValue(methodName, out m))
            {
                m?.Invoke(this, new object[0]);
            }

            string[] keep = null;
            this.VMData.MethodsSideEffects.TryGetValue(methodName, out keep);

            return this.GetJSONChanges(oldJson, clientState, keep);
        }

        [JSInvokable]
        public void InvokeMethodPure(string methodName, string currentSate)
        {
            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            JsonConvert.PopulateObject(currentSate, this, settings);

            MethodInfo m;
            if (this.VMData.Methods.TryGetValue(methodName, out m))
            {
                m?.Invoke(this, new object[0]);
            }
        }

        [JSInvokable]
        public string Synchronize(string currentSate)
        {
            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            JsonConvert.PopulateObject(currentSate, this, settings);

            return this.GetJSONChanges(currentSate);
        }

        [JSInvokable]
        public void SynchronizeOneway(string currentSate)
        {
            var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            JsonConvert.PopulateObject(currentSate, this, settings);
        }

        public string Mustache(string prop)
        {
            return "{{" + prop + "}}";
        }

        /// <summary>
        /// Create JSON to be passed to Vuezor client-side
        /// </summary>
        /// <returns></returns>
        private VueVMData PrepareVueVMData()
        {
            var vmdata = new VueVMData();
            var toReturn = new JObject();

            var methods = new JArray();
            foreach (var method in this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.GetCustomAttribute<VueIgnoreAttribute>() is VueIgnoreAttribute)
                {
                    continue;
                }

                bool pure = false;
                bool preventDefault = true;
                if (method.GetCustomAttribute<VueMethodAttribute>() is VueMethodAttribute vuemethod)
                {
                    pure = vuemethod.IsNoSideEffect;

                    if (vuemethod.Affected != null && vuemethod.Affected.Length > 0)
                    {
                        vmdata.MethodsSideEffects[method.Name] = vuemethod.Affected;
                    }

                    preventDefault = vuemethod.PreventDefault;
                }

                methods.Add(JObject.FromObject(new
                {
                    pure = pure,
                    name = method.Name,
                    preventDefault = preventDefault
                }));
                vmdata.Methods[method.Name] = method;

            }

            var data = new JObject();
            var computed = new JArray();
            var watch = new JObject();

            foreach (var p in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetCustomAttribute<VueIgnoreAttribute>() is VueIgnoreAttribute)
                {
                    continue;
                }

                var value = p.GetValue(this);
                var valueJO = value != null ? JToken.FromObject(p.GetValue(this)) : null;

                data[p.Name] = valueJO;

                if (p.GetCustomAttribute<VueComputedAttribute>() is VueComputedAttribute v)
                {
                    if (data[p.Name] != null)
                    {
                        data.Remove(p.Name);
                    }

                    data["computed__" + p.Name] = valueJO;

                    computed.Add(JObject.FromObject(new
                    {
                        pure = v.IsNoSideEffect,
                        getter = v.GetterJS,
                        name = p.Name,
                    }));

                    vmdata.Setters[p.Name] = p.GetSetMethod();
                    vmdata.Getters[p.Name] = p.GetGetMethod();
                    vmdata.ComputedPropertyInfo[p.Name] = v;
                    continue;
                }

                if (p.GetCustomAttribute<VueDataAttribute>() is VueDataAttribute vd)
                {
                    vmdata.PropertyInfo[p.Name] = vd;

                    if (vd.Watch)
                    {
                        watch[p.Name] = JObject.FromObject(new
                        {
                            deep = p.PropertyType.IsArray || p.PropertyType.IsClass,
                            pure = vd.IsNoSideEffect
                        });

                        vmdata.Setters[p.Name] = p.GetSetMethod();
                    }
                }

            }

            toReturn["data"] = data;
            toReturn["computed"] = computed;
            toReturn["methods"] = methods;
            toReturn["watch"] = watch;
            toReturn["onewaysync"] = this.IsOnewaySyncSupported;

            vmdata.VueVMJson = ((JObject)toReturn).ToString();
            return vmdata;
        }
    }

}
