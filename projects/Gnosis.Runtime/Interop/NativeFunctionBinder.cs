using System.Reflection;
using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Interop;

public class NativeFunctionBinder
{
    private readonly NativeFunctionRegistry _registry;

    public NativeFunctionBinder(NativeFunctionRegistry registry)
    {
        _registry = registry;
    }

    public void Bind(object target)
    {
        var type = target.GetType();
        Bind(type, target);
    }

    public void Bind(Type type, object? target = null)
    {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            var attr = method.GetCustomAttribute<NativeFunctionBindingAttribute>();
            if (attr is null)
            {
                continue;
            }

            var isStatic = method.IsStatic;
            if (!isStatic && target is null)
            {
                continue;
            }

            var func = new ReflectedNativeFunction(attr.Id, attr.Name, method, isStatic ? null : target);
            _registry.Register(func);
        }
    }

    public void Unbind(Type type)
    {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            var attr = method.GetCustomAttribute<NativeFunctionBindingAttribute>();
            if (attr is null)
            {
                continue;
            }

            _registry.Unregister(attr.Id);
        }
    }

    private sealed class ReflectedNativeFunction : INativeFunction
    {
        private readonly MethodInfo _method;
        private readonly object? _target;

        public int Id { get; }
        public string Name { get; }
        public int ParameterCount => _method.GetParameters().Length - 1;

        public ReflectedNativeFunction(int id, string name, MethodInfo method, object? target)
        {
            Id = id;
            Name = name;
            _method = method;
            _target = target;
        }

        public GGValue Execute(IVMState vm, GGValue[] args)
        {
            var parameters = new object?[args.Length + 1];
            parameters[0] = vm;

            for (var i = 0; i < args.Length; i++)
            {
                parameters[i + 1] = ReferenceMarshaller.MarshalFromGG(args[i]);
            }

            try
            {
                var result = _method.Invoke(_target, parameters);
                return ReferenceMarshaller.MarshalToGG(result);
            }
            catch (TargetInvocationException ex)
            {
                throw ExceptionBridge.WrapException(ex.InnerException ?? ex);
            }
        }
    }
}
