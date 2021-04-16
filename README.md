# NC-Blazor : VueZor
Comebine power of Blazor with VueJs. Write your code in C# and Blazor, render on client-side with Vue! Use Vue with Blazor by just installing one NuGet Package!  
Live Demo: https://vuezor.nant.co/

🟩 **No JavaScript**  
🟩 Actually, also does not need to know much about Vue 🤣  
🟩 Call Server-side C# Code directly from Vue bindings  
🟩 Server-Side Computed (Getter/Setter) Property (with some caveats for Getter)  
🟩 ````@(Razor)```` Syntax and ````{{Vue Mustache}}```` syntax in one view

![](https://vuezor.nant.co/images/vuezor-demo.gif)

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
# Start Using VueZor
Ready? Read the [Getting Started Guide](https://github.com/nantcom/vuezor/wiki/Getting-Started)

