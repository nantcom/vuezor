using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.Blazor.Vuezor
{

    /// <summary>
    /// Specify that the property is generated as vue property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class VueComputedAttribute : Attribute
    {

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
    /// This property will not be visible in Client-side
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class VueIgnoreAttribute : Attribute
    {

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

        /// <summary>
        /// List of properties that is affected by this method.
        /// </summary>
        public string[] Affected { get; set; }

        /// <summary>
        /// Prevent Default event at client side
        /// </summary>
        public bool PreventDefault { get; set; } = true;
    }

}
