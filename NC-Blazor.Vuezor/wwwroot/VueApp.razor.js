export function VuezorDataContext(rootElementRef, useLocal, model, dotNet, log) {

    var $me = {};

    function wait(lib, name, callback) {

        if (window[name]) {
            callback(window[name]);
            return;
        }

        if (lib != null) {

            if (document.querySelectorAll("script[src='" + lib + "']").length == 0) {

                var newScript = document.createElement("script");
                newScript.src = lib;
                document.getElementsByTagName("head")[0].appendChild(newScript);
            }
        }

        window.setTimeout(function () {

            wait(null, name, callback);
        }, 100);
    }

    var dgroups = {};
    function debounce(group, action, delay) {

        if (dgroups[group] == null) {
            dgroups[group] = 0;
        }

        if (delay == null) {
            delay = 125;
        }

        var myCount = ++dgroups[group];

        window.setTimeout(function () {

            if (myCount < dgroups[group]) {
                return;
            }

            action();

        }, delay);

    };

    var initialize = function (vueLib) {

        model = JSON.parse(model);

        if (log) {

            $me.log = console.log;
        } else {

            $me.log = function () { };
        }

        $me.pureOverride = false;
        $me.latestState = {};
        $me.justApply = false;
        $me.apply = function (vm, result) {

            $me.justApply = false;

            // copy changes to local object
            $me.log("apply:");
            $me.log(result);

            for (var prop in result) {

                vm.$data[prop] = result[prop];
            }

            $me.latestState = $me.serializeState(true);

            $me.log("apply result:");
            $me.log($me.latestState);

            $me.justApply = true;
        };

        $me.serializeState = function (full) {

            var state = {};
            for (var prop in $me.vm.$data) {

                var json = JSON.stringify($me.vm.$data[prop]);
                if (full) {
                    state[prop] = JSON.parse(json);
                    continue;
                }

                var lastState = JSON.stringify($me.latestState[prop]);

                if (json !== lastState) {

                    state[prop] = JSON.parse(json);
                }
            }

            return state;
        };

        $me.methodInvoker = function (methodName, pure) {

            if ($me.vm == null) {

                $me.log("invoked before initialized:" + methodName);
                return;
            }

            debounce(methodName, function () {

                $me.log("dotNet Invoke:" + methodName);
                $me.log("state:");

                var state = $me.serializeState();
                $me.log(state);

                if ($me.pureOverride) {
                    pure = false;
                }

                if (pure) {

                    dotNet.invokeMethodAsync("InvokeMethodPure", methodName, JSON.stringify(state));
                }
                else {

                    dotNet.invokeMethodAsync("InvokeMethod", methodName, JSON.stringify(state))
                        .then(r => $me.apply($me.vm, JSON.parse(r)));
                }

            });
        };

        $me.setter = function (propName, newValue, oldValue, pure) {

            var oldJ = JSON.stringify(oldValue);
            var newJ = JSON.stringify(newValue);

            if (oldJ == newJ) {
                return;
            }

            $me.log("dotNet setter:" + propName);
            if ($me.pureOverride) {
                pure = false;
            }

            if (pure) {

                debounce("SetterPure-" + propName, function () {

                    dotNet.invokeMethodAsync("SetterPure", propName, newJ);
                });
            }
            else {

                debounce("Setter-" + propName, function () {

                    dotNet.invokeMethodAsync("Setter", propName, newJ)
                        .then(r => $me.apply($me.vm, JSON.parse(r)));
                });
            }
        };

        // synchronize view data to server
        $me.sync = function () {

            if ($me.justApply) {
                $me.justApply = false;
                return;
            }

            debounce("sync", function () {

                $me.log("syncing state");
                var state = $me.serializeState();
                $me.log(state);

                dotNet.invokeMethodAsync("Synchronize", JSON.stringify(state))
                    .then(r => $me.apply($me.vm, JSON.parse(r)));
            });

        };

        $me.sync1way = function () {

            if ($me.justApply) {
                $me.justApply = false;
                return;
            }

            debounce("sync", function () {

                $me.log("syncing state (1way)");
                var state = $me.serializeState();
                $me.log(state);

                dotNet.invokeMethodAsync("SynchronizeOneway", JSON.stringify(state));

            }, 400);

        };

        const vueInit = {};

        // data - just copy from data
        vueInit.data = function () {

            return model.data;
        };

        // methods - create a method invoker
        vueInit.methods = {};
        model.methods.forEach(function (method) {

            vueInit.methods[method.name] = function (e) {

                if (e != null && method.preventDefault == true) {

                    e.preventDefault();
                }

                $me.methodInvoker(method.name);
            };
        });

        var synctype = model.onewaysync ? $me.sync1way : $me.sync;
        vueInit.methods.sync = synctype;

        // computed view data
        vueInit.computed = {};
        model.computed.forEach(function (computed) {

            vueInit.computed[computed.name] = {};

            if (computed.getter == null || computed.getter == "") {

                console.log("Vuezor Warning: GetterJS was not defined for " + computed.name + ". Value will not update unless there is roundtrip (method calls/setter) to server. Pure Calls are also disabled.");

                // for getters, we could not actually create getter
                // because vue could not observe it
                // we use watch to help 
                vueInit.computed[computed.name].get = function () {

                    return this["computed__" + computed.name];
                };

                $me.pureOverride = true;
            }
            else {

                // use javascript getter by compiling getter from string
                var getter = new Function("source", computed.getter);
                vueInit.computed[computed.name].get = function () {

                    return getter(this);
                };
            }

            vueInit.computed[computed.name].set = function (newValue) {

                $me.setter(computed.name, newValue, "", computed.pure);
            };

        });

        // watches (auto)
        vueInit.watch = model.watch;
        for (var prop in vueInit.watch) {

            var name = prop;
            var oldValue = null;
            vueInit.watch[prop].handler = function (newValue) {

                if (oldValue == null) {

                    $me.setter(name, newValue, "", vueInit.watch[name].pure);
                }
                else {

                    $me.setter(name, newValue, oldValue, vueInit.watch[name].pure);
                }
                oldValue = JSON.parse( JSON.stringify(newValue) );
            };
        }

        $me.log("Vue Initializing, init");
        $me.log(model);
        $me.log(vueInit);

        $me.app = vueLib.createApp(vueInit);

        var components = rootElementRef.getAttribute("data-vue-components").split(",");
        components.forEach(function (component) {

            var instance = Window[component];

            if (typeof instance === "undefined") {

                try {
                    instance = eval(component);
                } catch (e) {
                    instance = null;
                }
            }

            if (typeof instance === "object") {

                if (component.indexOf(".") == -1) {

                    for (var key in instance) {

                        var name = instance[key].name;
                        if (name == null) {
                            name = key;
                        }

                        $me.app.component(name.toLowerCase(), instance[key]);
                    }
                }
                else {

                    var name = instance.name;
                    if (name == null) {
                        name = component.substring( component.indexOf(".") + 1 );
                    }

                    $me.app.component(instance.name.toLowerCase(), instance);
                }
            }
        });

        $me.vm = $me.app.mount(rootElementRef);

        $me.log("Vue Initialized");
    };

    if (useLocal) {

        wait("./_content/NC-Blazor.Vuezor/vue/vue.global.prod.js", "Vue", initialize)
    }
    else {

        wait("https://cdnjs.cloudflare.com/ajax/libs/vue/3.0.11/vue.global.prod.js", "Vue", initialize)
    }

    return $me;
}