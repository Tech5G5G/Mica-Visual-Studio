using System.Reflection;

namespace System.Linq.Expressions;

/// <summary>
/// Contains extensions for dealing with <see cref="Expression"/>s.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Creates a <see cref="UnaryExpression"/> that represents a type conversion operation to <paramref name="type"/>.
    /// </summary>
    /// <remarks>Equivalent to <see cref="Expression.Convert(Expression, Type)"/>.</remarks>
    /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
    /// <param name="type">A <see cref="Type"/> to set the <see cref="Expression.Type"/> property equal to.</param>
    /// <returns>
    /// A <see cref="UnaryExpression"/> that has the <see cref="Expression.NodeType"/> property equal to <see cref="ExpressionType.Convert"/>.
    /// </returns>
    public static UnaryExpression Convert(this Expression expression, Type type)
    {
        return Expression.Convert(expression, type);
    }

    /// <summary>
    /// Creates a <see cref="UnaryExpression"/> that represents a type conversion operation to <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>Equivalent to <see cref="Expression.Convert(Expression, Type)"/>.</remarks>
    /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
    /// <typeparam name="T">The type to set the <see cref="Expression.Type"/> property equal to.</typeparam>
    /// <returns>
    /// A <see cref="UnaryExpression"/> that has the <see cref="Expression.NodeType"/> property equal to <see cref="ExpressionType.Convert"/>.
    /// </returns>
    public static UnaryExpression Convert<T>(this Expression expression)
    {
        return expression.Convert(typeof(T));
    }

    /// <summary>
    /// Creates a <see cref="TypeBinaryExpression"/> that represents a type check of <paramref name="type"/>.
    /// </summary>
    /// <remarks>Equivalent to <see cref="Expression.TypeIs(Expression, Type)"/>.</remarks>
    /// <param name="expression">An <see cref="Expression"/> to set the <see cref="TypeBinaryExpression.Expression"/> property equal to.</param>
    /// <param name="type">A <see cref="Type"/> to set the <see cref="TypeBinaryExpression.TypeOperand"/> property equal to.</param>
    /// <returns>
    /// A <see cref="TypeBinaryExpression"/> for which the <see cref="Expression.NodeType"/> property is equal to <see cref="ExpressionType.TypeIs"/>.
    /// </returns>
    public static TypeBinaryExpression TypeIs(this Expression expression, Type type)
    {
        return Expression.TypeIs(expression, type);
    }

    /// <summary>
    /// Creates a <see cref="TypeBinaryExpression"/> that represents a type check of <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>Equivalent to <see cref="Expression.TypeIs(Expression, Type)"/>.</remarks>
    /// <typeparam name="T">The type to set the <see cref="TypeBinaryExpression.TypeOperand"/> property equal to.</typeparam>
    /// <param name="expression">An <see cref="Expression"/> to set the <see cref="TypeBinaryExpression.Expression"/> property equal to.</param>
    /// <returns>
    /// A <see cref="TypeBinaryExpression"/> for which the <see cref="Expression.NodeType"/> property is equal to <see cref="ExpressionType.TypeIs"/>.
    /// </returns>
    public static TypeBinaryExpression TypeIs<T>(this Expression expression)
    {
        return expression.TypeIs(typeof(T));
    }

    /// <summary>
    /// Creates a <see cref="MemberExpression"/> that represents accessing a property.
    /// </summary>
    /// <remarks>Equivalent to <see cref="Expression.Property(Expression, PropertyInfo)"/>.</remarks>
    /// <param name="expression">An <see cref="Expression"/> to set the <see cref="MemberExpression.Expression"/> property equal to.</param>
    /// <param name="property">The <see cref="PropertyInfo"/> to set the <see cref="MemberExpression.Member"/> property equal to.</param>
    /// <returns>
    /// A <see cref="MemberExpression"/> that has the <see cref="Expression.NodeType"/> property equal to <see cref="ExpressionType.MemberAccess"/>.
    /// </returns>
    public static MemberExpression Property(this Expression expression, PropertyInfo property)
    {
        return Expression.Property(expression, property);
    }

    /// <summary>
    /// Compiles <paramref name="body"/> into executable code and produces a <see cref="Func{T, TResult}"/> that represents it.
    /// </summary>
    /// <remarks>
    /// Equivalent to <see cref="Expression.Lambda(Expression, ParameterExpression[])"/>
    /// followed with <see cref="Expression{TDelegate}.Compile()"/>.
    /// </remarks>
    /// <typeparam name="T">The type of the parameter used by <paramref name="body"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="body"/>.</typeparam>
    /// <param name="body">The <see cref="Expression"/> to compile into executable code.</param>
    /// <param name="parameter">The <see cref="ParameterExpression"/> used by <paramref name="body"/> as a parameter.</param>
    /// <returns>A <see cref="Func{T, TResult}"/> that represents the compiled <see cref="Expression"/>.</returns>
    public static Func<T, TResult> Compile<T, TResult>(this Expression body, ParameterExpression parameter)
    {
        return Expression.Lambda<Func<T, TResult>>(body, parameter).Compile();
    }
}
