using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace DynamicTickRate
{
    internal static class SignalRNoisePatch
    {
        public static void Apply(Harmony harmony)
        {
            // Late-bind to avoid compile-time dependency on SkyFrost.Base
            var appHubType = AccessTools.TypeByName("SkyFrost.Base.AppHub");
            if (appHubType is null)
                return;

            var targets = new MethodInfo?[]
            {
                FindAsyncBody(appHubType, "BroadcastSession"), // "SIGNALR: BroadcastSession ..."
                FindAsyncBody(appHubType, "KeyListenerAdded"), // "KeyListenerAdded ..." + "Sending info matching broadcast key ..."
            };

            foreach (var t in targets)
            {
                if (t is null) continue;
                harmony.Patch(t, transpiler: new HarmonyMethod(typeof(SignalRNoisePatch), nameof(TranspileNoLog)));
            }
        }

        private static IEnumerable<CodeInstruction> TranspileNoLog(IEnumerable<CodeInstruction> instructions)
        {
            var fakeStr = AccessTools.Method(typeof(SignalRNoisePatch), nameof(FakeLogString));
            var fakeObj = AccessTools.Method(typeof(SignalRNoisePatch), nameof(FakeLogObject));

            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Call && ins.operand is MethodInfo mi)
                {
                    if (mi.DeclaringType?.FullName == "Elements.Core.UniLog")
                    {
                        var pars = mi.GetParameters();
                        if (pars.Length >= 1)
                        {
                            var p0 = pars[0].ParameterType;
                            if (p0 == typeof(string))
                            {
                                yield return new CodeInstruction(OpCodes.Call, fakeStr);
                                continue;
                            }
                            if (p0 == typeof(object))
                            {
                                yield return new CodeInstruction(OpCodes.Call, fakeObj);
                                continue;
                            }
                        }

                        // Unexpected signature – drop the call anyway
                        yield return new CodeInstruction(OpCodes.Nop);
                        continue;
                    }
                }

                yield return ins;
            }
        }

        private static void FakeLogString(string _msg, bool _stack) { }
        private static void FakeLogObject(object _msg, bool _stack) { }

        private static MethodInfo? FindAsyncBody(Type type, string methodName)
        {
            var mi = AccessTools.Method(type, methodName);
            if (mi is null) return null;

            var attr = mi.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) as AsyncStateMachineAttribute;
            var smType = attr?.StateMachineType;
            if (smType is null) return null;

            return AccessTools.Method(smType, nameof(IAsyncStateMachine.MoveNext));
        }
    }
}
