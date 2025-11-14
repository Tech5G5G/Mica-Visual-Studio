using System.Reflection;
using System.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;

namespace MicaVisualStudio.Extensions
{
    public static class ExpressionExtensions
    {
        public static UnaryExpression Convert(this Expression expression, Type type) =>
            Expression.Convert(expression, type);

        public static UnaryExpression Convert<T>(this Expression expression) =>
            expression.Convert(typeof(T));

        public static TypeBinaryExpression TypeIs(this Expression expression, Type type) =>
            Expression.TypeIs(expression, type);

        public static TypeBinaryExpression TypeIs<T>(this Expression expression) =>
            expression.TypeIs(typeof(T));

        public static MemberExpression Property(this Expression expression, PropertyInfo property) =>
            Expression.Property(expression, property);

        public static Func<T, TResult> Compile<T, TResult>(this Expression body, ParameterExpression parameter) =>
            Expression.Lambda<Func<T, TResult>>(body, parameter).Compile();
    }
}
