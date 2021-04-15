export function VuezorDataContext(rootElementRef, useLocal, model, dotNet, log) {

    var $me = {};

    function wait(lib, name, callback) {

        if (lib != null) {

            var key = "lib-" + name;
            if (document.getElementsByClassName(key).length == 0) {

                var newScript = document.createElement("script");
                newScript.src = lib;
                newScript.className = key;
                document.getElementsByTagName("head")[0].appendChild(newScript);
            }
        }

        if (window[name]) {
            callback(window[name]);
            return;
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
            delay = 250;
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

        $me.log = function (obj) {

            if (log) {

                console.log(obj);
            }
        }

        $me.justApply = false;
        $me.apply = function (vm, result) {

            $me.justApply = false;

            // copy changes to local object
            $me.log("apply:");
            $me.log(result);

            for (var prop in vm.$data) {

                var oldJ = JSON.stringify(vm.$data[prop]);
                var newJ = "";
                var key = prop;

                if (prop.indexOf("computed__") >= 0) {

                    key = prop.substr(10);
                }

                if (typeof result[key] !== 'undefined') {

                    newJ = JSON.stringify(result[key]);
                }

                if (oldJ !== newJ) {

                    vm.$data[prop] = result[prop];
                }
            }

            $me.log("apply result:");
            $me.log(result);

            $me.justApply = true;
        };

        $me.serializeState = function () {

            var state = {};
            for (var prop in $me.vm.$data) {

                state[prop] = $me.vm.$data[prop];
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

                if (pure) {

                    dotNet.invokeMethodAsync("InvokeMethodPure", methodName, JSON.stringify(state));
                }
                else {

                    dotNet.invokeMethodAsync("InvokeMethod", methodName, JSON.stringify(state))
                        .then(r => $me.apply($me.vm, JSON.parse(r)));
                }

            }, 125 );
        };

        $me.setter = function (propName, newValue, oldValue, pure) {

            var oldJ = JSON.stringify(oldValue);
            var newJ = JSON.stringify(newValue);

            if (oldJ == newJ) {
                return;
            }


            $me.log("dotNet setter:" + propName);

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

            vueInit.methods[method.name] = function () {

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

                $me.log("Warning: GetterJS was not defined for " + computed.name + ". Value will not update unless there is roundtrip (method calls/setter) to server.");

                // for getters, we could not actually create getter
                // because vue could not observe it
                // we use watch to help 
                vueInit.computed[computed.name].get = function () {

                    return this["computed__" + computed.name];
                };
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
            vueInit.watch[prop].handler = function (newValue, oldValue) {

                $me.setter(name, newValue, "", vueInit.watch[name].pure);
            };
        }

        $me.log("Vue Initializing, init");
        $me.log(vueInit);

        $me.app = vueLib.createApp(vueInit);
        $me.vm = $me.app.mount(rootElementRef);

        $me.log("Vue Initialized");
    };

    if (useLocal) {
        wait("./vue/vue.global.prod.js", "Vue", initialize)

    }
    else {
        wait("https://cdnjs.cloudflare.com/ajax/libs/vue/3.0.11/vue.global.prod.js", "Vue", initialize)

    }

    return $me;
}