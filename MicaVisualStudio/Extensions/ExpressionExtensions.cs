using System.Reflection;

namespace System.Linq.Expressions;

public static class ExpressionExtensions
{
    public static UnaryExpression Convert(this Expression expression, Type type)
    {
        return Expression.Convert(expression, type);
    }

    public static UnaryExpression Convert<T>(this Expression expression)
    {
        return expression.Convert(typeof(T));
    }

    public static TypeBinaryExpression TypeIs(this Expression expression, Type type)
    {
        return Expression.TypeIs(expression, type);
    }

    public static TypeBinaryExpression TypeIs<T>(this Expression expression)
    {
        return expression.TypeIs(typeof(T));
    }

    public static MemberExpression Property(this Expression expression, PropertyInfo property)
    {
        return Expression.Property(expression, property);
    }

    public static Func<T, TResult> Compile<T, TResult>(this Expression body, ParameterExpression parameter)
    {
        return Expression.Lambda<Func<T, TResult>>(body, parameter).Compile();
    }
}
