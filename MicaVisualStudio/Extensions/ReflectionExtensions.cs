using System;
using System.Reflection;
using System.Windows;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Expression = System.Linq.Expressions.Expression;

namespace MicaVisualStudio.Extensions;

public static class ReflectionExtensions
{
    public static ILHook CreateDetour<T1, T2>(this MethodInfo info, Action<T1, T2> action) =>
        new(info, context =>
        {
            ILCursor cursor = new(context);
            cursor.Index = cursor.Instrs.Count - 1; // Move cursor to end, but before return

            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldarg_1); // First parameter

            cursor.EmitDelegate(action);
        });

    public static Func<TOwner, TReturn> CreateGetter<TOwner, TReturn>(this PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(TOwner));
        return parameter.Convert(property.DeclaringType)
                        .Property(property)
                        .Convert<TReturn>()
                        .Compile<TOwner, TReturn>(parameter);
    }

    public static DependencyProperty GetDependencyProperty(this Type type, string propertyName) =>
        (DependencyProperty)type.GetField($"{propertyName}Property", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                .GetValue(null);
}
