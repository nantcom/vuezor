# NC-Blazor : VueZor
Comebine power of Blazor with VueJs. Write your code in C# and Blazor, render on client-side with Vue! Use Vue with Blazor by just installing one NuGet Package!  
Live Demo: https://vuezor.nant.co/

# Why you need this ?
Most of you would ask: Why not just use Blazor Web Assembly?

Blazor spoil us with the convinience of using C# everywhere but many little things that could be handled at client side, such as: switching between tabs, collapsible panels (accordion), showing/hiding contents using conditional rendering or update the binding text, has to be handled by BVirtual DOM on server side. 

With VueZor - you will be writing a View in Vue, which make it possible for binding to happen entirely in browser (no WebSocket Messages to/from server)

![](https://vuezor.nant.co/images/vuezor-client.gif)

But this is VueZor not Vue, the binding and method could be invoked back to Server-side!

![](https://vuezor.nant.co/images/vuezor-demo.gif)

So, client-side binding and components but tight integration with Server code and no need to create any API for frontend - this is why you need VueZor!

No No No, this is Blazor Comopnent, **_you will not be writing any JavaScript!_**

🟩 **No JavaScript**  
🟩 Actually, also does not need to know much about Vue 🤣  
🟩 Call Server-side C# Code directly from Vue bindings  
🟩 Server-Side Computed (Getter/Setter) Property (with some caveats for Getter)  
🟩 ````@(Razor)```` Syntax and ````{{Vue Mustache}}```` syntax in one view

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

# Roadmap
🟩 Compile simple Getter/Setter from C# to Javascript? :D
