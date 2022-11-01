using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Reflection;
using SourceGenerator;

namespace SourceGenExperiments
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var metadataLoadContext = new MetadataLoadContext(context.Compilation);

            DiscoverHubs(context, metadataLoadContext);
            DiscoverControllers(context, metadataLoadContext);
        }

        private static void DiscoverControllers(GeneratorExecutionContext context, MetadataLoadContext metadataLoadContext)
        {
            var nonControllerAttributeType = metadataLoadContext.ResolveType("Microsoft.AspNetCore.Mvc.NonControllerAttribute");
            var nonActionAttributeType = metadataLoadContext.ResolveType("Microsoft.AspNetCore.Mvc.NonActionAttribute");
            var controllerAttributeType = metadataLoadContext.ResolveType("Microsoft.AspNetCore.Mvc.ControllerAttribute");

            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);
            writer.WriteLine("using Microsoft.AspNetCore.Http;");

            foreach (var t in metadataLoadContext.Assembly.GetTypes())
            {
                if (!IsController(t))
                {
                    continue;
                }

                if (!t.GetTypeSymbol().IsPartial())
                {
                    // Report diagnostics
                    continue;
                }

                if (t.Namespace is { } ns)
                {
                    writer.WriteLine($"namespace {ns}");
                    writer.StartBlock();
                }

                //writer.WriteLine("");
                //writer.WriteLine("delegate ");
                //writer.WriteLine("");

                writer.WriteLine($"public partial class {t.Name}");
                writer.StartBlock();

                writer.WriteLine(@$"public static void BindHub(IDictionary<string, Func<{t}, Microsoft.AspNetCore.SignalR.HubConnectionContext, Microsoft.AspNetCore.SignalR.Protocol.HubMessage, {typeof(CancellationToken)}, Task>> definition)");
                writer.StartBlock();

                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!IsAction(t, m))
                    {
                        continue;
                    }

                    //writer.StartBlock();
                    //writer.EndBlock();
                }

                writer.EndBlock();
                writer.EndBlock();

                if (t.Namespace is not null)
                {
                    writer.EndBlock();
                }
            }

            context.AddSource("Controllers.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

            bool IsController(Type typeInfo)
            {
                if (!typeInfo.IsClass)
                {
                    return false;
                }

                if (typeInfo.IsAbstract)
                {
                    return false;
                }

                // We only consider public top-level classes as controllers. IsPublic returns false for nested
                // classes, regardless of visibility modifiers
                if (!typeInfo.IsPublic)
                {
                    return false;
                }

                if (typeInfo.ContainsGenericParameters)
                {
                    return false;
                }

                if (typeInfo.CustomAttributes.Any(a => nonControllerAttributeType.IsAssignableFrom(a.AttributeType)))
                {
                    return false;
                }

                if (!typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) &&
                    !typeInfo.CustomAttributes.Any(a => controllerAttributeType.IsAssignableFrom(a.AttributeType)))
                {
                    return false;
                }

                return true;
            }

            bool IsAction(Type typeInfo, MethodInfo methodInfo)
            {
                // The SpecialName bit is set to flag members that are treated in a special way by some compilers
                // (such as property accessors and operator overloading methods).
                if (methodInfo.IsSpecialName)
                {
                    return false;
                }

                if (methodInfo.CustomAttributes.Any(a => nonActionAttributeType.IsAssignableFrom(a.AttributeType)))
                {
                    return false;
                }

                // Overridden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
                if (methodInfo.GetBaseDefinition().DeclaringType.Equals(typeof(object)))
                {
                    return false;
                }

                // Dispose method implemented from IDisposable is not valid
                if (IsIDisposableMethod(methodInfo))
                {
                    return false;
                }

                if (methodInfo.IsStatic)
                {
                    return false;
                }

                if (methodInfo.IsAbstract)
                {
                    return false;
                }

                if (methodInfo.IsConstructor)
                {
                    return false;
                }

                if (methodInfo.IsGenericMethod)
                {
                    return false;
                }

                return methodInfo.IsPublic;
            }

            static bool IsIDisposableMethod(MethodInfo methodInfo)
            {
                // Ideally we do not want Dispose method to be exposed as an action. However there are some scenarios where a user
                // might want to expose a method with name "Dispose" (even though they might not be really disposing resources)
                // Example: A controller deriving from MVC's Controller type might wish to have a method with name Dispose,
                // in which case they can use the "new" keyword to hide the base controller's declaration.

                // Find where the method was originally declared
                var baseMethodInfo = methodInfo.GetBaseDefinition();
                var declaringType = baseMethodInfo.DeclaringType;

                return
                    (typeof(IDisposable).IsAssignableFrom(declaringType) &&
                     declaringType.GetInterfaceMap(typeof(IDisposable)).TargetMethods[0] == baseMethodInfo);
            }
        }

        private static void DiscoverHubs(GeneratorExecutionContext context, MetadataLoadContext metadataLoadContext)
        {
            var hubType = metadataLoadContext.ResolveType("Microsoft.AspNetCore.SignalR.Hub");
            var hubOfTType = metadataLoadContext.ResolveType("Microsoft.AspNetCore.SignalR.Hub`1");
            var asyncEnumerable = metadataLoadContext.ResolveType("System.Collections.Generic.IAsyncEnumerable`1");

            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var hubTypes = new List<Type>();

            foreach (var t in metadataLoadContext.Assembly.GetTypes())
            {
                if (hubType.IsAssignableFrom(t) && !t.Equals(hubType))
                {
                    if (!t.GetTypeSymbol().IsPartial())
                    {
                        // Diagnostic!
                        continue;
                    }

                    hubTypes.Add(t);
                }
            }

            foreach (var t in hubTypes)
            {
                var globalNs = t.Namespace is null;
                if (!globalNs)
                {
                    writer.WriteLine($"namespace {t.Namespace}");
                    writer.StartBlock();
                }
                writer.WriteLine($"{(t.IsPublic ? "public " : "")}partial class {t.Name}");
                writer.StartBlock();

                var generatedMethods = new List<(string, string)>();

                var index = 0;
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (index > 0)
                    {
                        writer.WriteLine("");
                    }

                    if (!IsHubMethod(m))
                    {
                        continue;
                    }

                    var parameters = m.GetParameters();
                    var streamingParameters = new Type[parameters.Length];
                    var hasStreamingParameters = false;

                    foreach (var p in parameters)
                    {
                        if (IsAsyncEnumerable(p.ParameterType))
                        {
                            // Streaming parameter
                            streamingParameters[p.Position] = p.ParameterType.GetGenericArguments()[0];
                            hasStreamingParameters = true;
                        }
                    }

                    var hasStreamingReturn = IsAsyncEnumerable(m.ReturnType);

                    var generatedMethod = $"{m.Name}Thunk";
                    generatedMethods.Add((m.Name, generatedMethod));

                    writer.WriteLine($"static async Task {generatedMethod}({hubType} hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, {typeof(CancellationToken)} cancellationToken)");
                    writer.StartBlock();

                    if (hasStreamingParameters)
                    {
                        writer.WriteLine("var invocation = (Microsoft.AspNetCore.SignalR.Protocol.StreamInvocationMessage)message;");
                        var streamingIndex = 0;
                        for (int i = 0; i < streamingParameters.Length; i++)
                        {
                            if (streamingParameters[i] is not Type streamType)
                            {
                                continue;
                            }
                            writer.WriteLine($"var channel{i} = System.Threading.Channels.Channel.CreateBounded<{streamType}>(10);");
                            writer.WriteLine("// Register this channel with the runtime based on this stream id");
                            writer.WriteLine($"// connection.AddStream(invocation.StreamIds[{streamingIndex++}], item => channel{i}.WriteAsync(({streamType})item), (Exception ex) => channel{i}.TryComplete(ex));");
                            writer.WriteLine($"var stream{i} = channel{i}.Reader.ReadAllAsync();");
                        }
                    }
                    else
                    {
                        writer.WriteLine("var invocation = (Microsoft.AspNetCore.SignalR.Protocol.InvocationMessage)message;");
                    }

                    if (!hasStreamingReturn)
                    {
                        // Hub invocation
                        writer.WriteLine("var args = invocation.Arguments;");

                        var task = m.ReturnType.Equals(typeof(Task)) ||
                                   m.ReturnType.Equals(typeof(ValueTask));
                        var taskOfT = m.ReturnType.IsGenericType &&
                            (m.ReturnType.GetGenericTypeDefinition().Equals(typeof(Task<>)) ||
                             m.ReturnType.GetGenericTypeDefinition().Equals(typeof(ValueTask<>)));

                        var hasVoidReturn = m.ReturnType.Equals(typeof(void));
                        var hasAwait = task || taskOfT;
                        var hasResult = taskOfT || !(hasVoidReturn || task);

                        if (hasResult)
                        {
                            var returnType = hasAwait ? m.ReturnType.GetGenericArguments()[0] : m.ReturnType;
                            writer.WriteLine($"{returnType} result = default;");
                        }

                        writer.WriteLine("try");
                        writer.StartBlock();
                        if (taskOfT)
                        {
                            writer.Write($"result = await (({t})hub).{m.Name}(");
                        }
                        else if (task)
                        {
                            writer.Write($"await (({t})hub).{m.Name}(");
                        }
                        else if (hasVoidReturn)
                        {
                            writer.Write($"(({t})hub).{m.Name}(");
                        }
                        else
                        {
                            writer.Write($"result = (({t})hub).{m.Name}(");
                        }
                        var argIndex = 0;
                        foreach (var p in parameters)
                        {
                            if (p.Position > 0)
                            {
                                writer.WriteNoIndent(", ");
                            }

                            if (streamingParameters[p.Position] is Type streamingType)
                            {
                                writer.WriteNoIndent($"stream{p.Position}");
                            }
                            else
                            {
                                writer.WriteNoIndent($"({p.ParameterType})args[{argIndex}]");
                                argIndex++;
                            }
                        }
                        writer.WriteLineNoIndent(");");

                        writer.EndBlock();
                        writer.WriteLine("catch (Exception ex) when (invocation.InvocationId is not null)");
                        writer.StartBlock();
                        writer.WriteLine($@"await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithError(invocation.InvocationId, ""Invoking {m.Name} failed""));");
                        writer.WriteLine("return;");
                        writer.EndBlock();

                        writer.WriteLine("finally");
                        writer.StartBlock();
                        if (hasStreamingParameters)
                        {
                            var streamingIndex = 0;
                            for (int i = 0; i < streamingParameters.Length; i++)
                            {
                                if (streamingParameters[i] is not Type streamType)
                                {
                                    continue;
                                }
                                writer.WriteLine($"channel{i}.Writer.TryComplete();");
                                writer.WriteLine("// Unregister this channel with the runtime based on this stream id");
                                writer.WriteLine($"// connection.RemoveStream(invocation.StreamIds[{streamingIndex++}]);");
                            }
                            writer.WriteLine("");
                        }
                        writer.EndBlock();

                        writer.WriteLine("");
                        writer.WriteLine("if (invocation.InvocationId is not null)");
                        writer.StartBlock();
                        writer.WriteLine($"await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithResult(invocation.InvocationId, {(hasResult ? "result" : "null")}));");
                        writer.EndBlock();
                    }
                    else
                    {
                        writer.WriteLine("var args = invocation.Arguments;");
                        writer.WriteLine("var streamItemMessage = new Microsoft.AspNetCore.SignalR.Protocol.StreamItemMessage(invocation.InvocationId, null);");

                        writer.WriteLine("try");
                        writer.StartBlock();

                        writer.Write($"await foreach (var item in (({t})hub).{m.Name}(");
                        var argIndex = 0;
                        foreach (var p in parameters)
                        {
                            if (p.Position > 0)
                            {
                                writer.WriteNoIndent(", ");
                            }

                            if (streamingParameters[p.Position] is Type streamingType)
                            {
                                writer.WriteNoIndent($"stream{p.Position}");
                            }
                            else if (p.ParameterType.Equals(typeof(CancellationToken)))
                            {
                                writer.WriteNoIndent("cancellationToken");
                            }
                            else
                            {
                                writer.WriteNoIndent($"({p.ParameterType})args[{argIndex}]");
                                argIndex++;
                            }
                        }
                        writer.WriteLineNoIndent(").WithCancellation(cancellationToken))");

                        // foreach
                        writer.StartBlock();
                        writer.WriteLine("streamItemMessage.Item = item;");
                        writer.WriteLine("await connection.WriteAsync(streamItemMessage);");
                        writer.EndBlock();

                        writer.EndBlock();
                        writer.WriteLine("catch (Exception ex)");
                        writer.StartBlock();
                        // writer.WriteLine($@"await connection.WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage.WithError(invocation.InvocationId, ""Invoking {m.Name} failed""));");
                        // writer.WriteLine("return;");
                        writer.EndBlock();

                        writer.WriteLine("finally");
                        writer.StartBlock();
                        if (hasStreamingParameters)
                        {
                            var streamingIndex = 0;
                            for (int i = 0; i < streamingParameters.Length; i++)
                            {
                                if (streamingParameters[i] is not Type streamType)
                                {
                                    continue;
                                }
                                writer.WriteLine($"channel{i}.Writer.TryComplete();");
                                writer.WriteLine("// Unregister this channel with the runtime based on this stream id");
                                writer.WriteLine($"// connection.RemoveStream(invocation.StreamIds[{streamingIndex++}]);");
                            }
                        }
                        writer.EndBlock();
                    }
                    writer.EndBlock();

                    index++;
                }

                writer.WriteLine("");
                writer.WriteLine(@$"public static void BindHub(IDictionary<string, Func<{hubType}, Microsoft.AspNetCore.SignalR.HubConnectionContext, Microsoft.AspNetCore.SignalR.Protocol.HubMessage, {typeof(CancellationToken)}, Task>> definition)");
                writer.WriteLine("{");
                writer.Indent();
                foreach (var (method, thunk) in generatedMethods)
                {
                    writer.WriteLine($@"definition.Add(""{method}"", {thunk});");
                }
                writer.Unindent();
                writer.WriteLine("}");

                writer.EndBlock();

                if (!globalNs)
                {
                    writer.EndBlock();
                }
            }

            context.AddSource("Hubs.g.cs", SourceText.From(sb.ToString().Trim(), Encoding.UTF8));


            bool IsAsyncEnumerable(Type type)
            {
                return type.IsGenericType && type.GetGenericTypeDefinition().Equals(asyncEnumerable);
            }

            bool IsHubMethod(MethodInfo methodInfo)
            {
                var baseDefinition = methodInfo.GetBaseDefinition().DeclaringType!;
                if (baseDefinition.Equals(typeof(object)) || methodInfo.IsSpecialName)
                {
                    return false;
                }

                var baseType = baseDefinition.IsGenericType ? baseDefinition.GetGenericTypeDefinition() : baseDefinition;
                return !hubType.Equals(baseType);
            }

            bool IsHubOfT(Type t, out Type interfaceType)
            {
                if (t.BaseType?.IsGenericType == true &&
                    t.BaseType.GetGenericTypeDefinition() == hubOfTType)
                {
                    interfaceType = t.GetGenericArguments()[0];
                    return true;
                }
                interfaceType = null;
                return false;
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {

        }
    }
}
