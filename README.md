# NC-Blazor : VueZor
Use Vue with Blazor. Actually, it is Blazor but rendered on client-side with Vue!

# Why you need this ?
It might seem counter-intuitive to complement SPA-framework like Blazor with another SPA-framework like Vue.

While Blazor spoil us with the convinience of using C# everywhere, many little things that could be handled at client side, such as: switching between tabs, collapsible panels (accordion), showing/hiding contents using conditional rendering, has to be handled by Blazor in its Virtual DOM on server side. It does make sense for Blazor WebAssembly, but it does not on Blazor Server, at least for me.

So I have decided to merge Vue with Blazor and get best of both worlds!

No No No, this is Blazor Comopnent, **_you will not be writing any JavaScript!_**

# How it works?
VueZor **generates** [Vue component](https://v3.vuejs.org/guide/introduction.html#declarative-rendering)
(the JS code in 'Declarative Rendering') from your C# Class and [mount](https://v3.vuejs.org/api/application-api.html#mount) it with the template you wrote in **Razor**.

Of course, you can mix Razor syntax and Vue mustaces in the same component. Razor syntax will serve as "Pre-rendering" for your Vue template while Vue (and VueZor) take care of stuff happenning on the Client-side.

````cshtml
<VueApp TData="SampleVM">

    Hello from Razor - now is @(DateTime.Now) <br/>
    Hello from Vue @context.Mustache(nameof(context.counter))

</VueApp>
````
# Getting Started
1) Install ````NC-Blazor.Vuezor```` NuGet package using method of your choice
2) In your ````_Imports.razor````, add

````cs
@using NC.Blazor
@using NC.Blazor.Vuezor
````
3) In your C# ViewModel, inherit from VueVM
````cs
public class MyData : VueVM
{
    // You will be required to override this:
    // - true : means all your method/properties has no side-effect. That is if you
    //          change Property A, it wont change value of property B. 
    //
    //          VueZor can just set the property value of A without worrying about
    //          getting value of B from Blazor.

    // - false : you have property that cascade changes to another Property.
    //           Such as, setting Score to 40 will cause Grade to change to F. 
    //           In this case, VueZor has to get changes from Blazor.
    protected override bool IsOnewaySyncSupported => false;
    
    // Properties are automatically visible to Vue
    public int Counter { get; set; }
    
    // Public Methods as well
    public void Increment()
    {
        this.Counter++;
    }    
}
````
4) Create VueApp Component and set TData to your ViewModel class. And create your View in the VueApp component.
### Points of interest:
- You can reduce bug from your View by using ````@context.Mustache```` and ````nameof```` to create quite a strongly typed binding.
**DO NOT USE ````{{ nameof(context.counter) }}```` since it will cause Vue template compiler to fail. (I will look into the root cause soon)**
- **v-on:** can be bound directly to your C# methods. 
````html
<VueApp TData="MyData">

    Hello from Vue : @context.Mustache(nameof(context.counter))<br/>
    {{ counter }}

    <button v-on:click="Increment">Increment</button>
        
</VueApp>
````
5) Run. Vue Library is added for you automatically at run-time in the page that use VueApp component!

# Using 3rd Party Vue Component
> Please note that VueZor was developed against V.3 of Vue. Component developed for V.2 has different approaches to
register the components and may not work.

## Practical example: [MDB for Vue](https://mdbootstrap.com/docs/b5/vue/getting-started/installation/)

1) Add link/script tags per [manual (or CDN) installation instructions](https://mdbootstrap.com/docs/b5/vue/getting-started/installation/) of the library to ````_Host.cshtml````.
**EXCEPT** script tag for Vue Library. Use this instead:

````html
<script src="https://cdnjs.cloudflare.com/ajax/libs/vue/3.0.11/vue.global.prod.js"></script>
````
VueZor will notice that you already have Vue Library loaded and will not add its own ````<script>```` tag to load Vue.

For MDB Vue , the whole code would look like this:
````cshtml
@page "/"
@namespace NC.Vuezor.Sample.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />

    <link href="css/site.css" rel="stylesheet" />
    <link href="NC.Vuezor.styles.css" rel="stylesheet" />

    <link href="https://use.fontawesome.com/releases/v5.15.1/css/all.css" rel="stylesheet" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/mdb-vue-ui-kit/css/mdb.min.css">

    <title>NC.Vuezor</title>

    <base href="~/" />
</head>
<body>
    <div class="container">
        <component type="typeof(App)" render-mode="ServerPrerendered" />
    </div>
    <div id="blazor-error-ui">
        <environment include="Staging,Production">
            An error has occurred. This application may no longer respond until reloaded.
        </environment>
        <environment include="Development">
            An unhandled exception has occurred. See browser dev tools for details.
        </environment>
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <!-- CANNOT USE THIS : script src="https://unpkg.com/vue@next"></script -->
    <script src="https://cdnjs.cloudflare.com/ajax/libs/vue/3.0.11/vue.global.prod.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/mdb-vue-ui-kit/js/mdb.umd.min.js"></script>

    <script src="_framework/blazor.server.js"></script>
</body>
</html>

````
Notice that the instruction stated that:
>_All components will be accessible from the global variable **mdb**._
so we need to add that to your VueApp components.

2) In your ````VueZor```` tag, add a VueComponents parameter as follows:
````cshtml
<VueApp TData="SampleVM"
        VueComponents="@(new string[] { "mdb" })"
        >
````
VueZor will notice that you want to include component from global variable **mdb**. It would scan for
all components that was defined as property within that global variable.
> You can also specify **mdb.MDBBtn** to use only MDBBtn component in your View

3) In you View, use all **lowercase** as tag name to use the imported components. This will make sure Razor will not confuse Vue component with Razor Component and show errors

````cshtml
<VueApp TData="SampleVM"
        VueComponents="@(new string[] { "mdb" })"
        IsLoggingEnabled="false">

    <h1 style="margin-bottom: 20px">
        Vuezor Demo
    </h1>

    <mdbcard style="margin-bottom: 20px">
        <mdbcardbody>
            <mdbcardtitle class="text-primary">
                <h2>Counter Demo</h2>
            </mdbcardtitle>
            <mdbcardtext>

                Hello from Razor - now is @(DateTime.Now) <br/>
                Hello from Vue @context.Mustache(nameof(context.counter))

                <div style="margin-top: 20px">
                    <mdbinput label="Two Way binding:" v-model="counter" />
                </div>
            </mdbcardtext>

            <mdbbtn color="primary" v-on:click="Increment">Increment on Server</mdbbtn>
        </mdbcardbody>
    </mdbcard>
</VueApp>
````
4) if the **attribute** of the component has camelCasing, such as [inputGroup](https://mdbootstrap.com/docs/b5/vue/forms/input-group/) in MDBInput - use kebab-case
````cshtml
<form style="margin-top: 20px"
       v-on:submit="AddItem" >
    <mdbinput input-group label="Todo Text" v-model="NewItem.ToDo" required>
        <mdbbtn color="secondary"type="submit">Add Item</mdbbtn>
    </mdbinput>
</form>
````




