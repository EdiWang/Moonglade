using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Data;

// https://stackoverflow.com/questions/457316/combining-two-expressions-expressionfunct-bool
public static class ExpressionExtentions
{
    public static Expression<Func<T, bool>> AndAlso<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr1.Body);

        var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expr2.Body);

        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(left ?? throw new InvalidOperationException(),
                right ?? throw new InvalidOperationException()), parameter);
    }

    private class ReplaceExpressionVisitor(Expression oldValue, Expression newValue) : ExpressionVisitor
    {
        public override Expression Visit(Expression node)
        {
            return node == oldValue ? newValue : base.Visit(node);
        }
    }
}