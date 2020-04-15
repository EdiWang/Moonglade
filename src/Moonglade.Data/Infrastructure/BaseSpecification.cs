using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Moonglade.Data.Infrastructure
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        protected BaseSpecification()
        {
        }

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        public Expression<Func<T, bool>> Criteria { get; private set; }
        public Func<IQueryable<T>, IIncludableQueryable<T, object>> Include { get; private set; }
        public List<string> IncludeStrings { get; } = new List<string>();
        public Expression<Func<T, object>> OrderBy { get; private set; }
        public Expression<Func<T, object>> OrderByDescending { get; private set; }

        public int Take { get; private set; }
        public int Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        public void AddCriteria(Expression<Func<T, bool>> criteria)
        {
            if (null != Criteria)
            {
                // TODO: Check if this never gets called because the child class constructor didn't finish executing while invoking AddCriteria()
                Criteria.AndAlso(criteria);
            }
            else
            {
                Criteria = criteria;
            }
        }

        protected virtual void AddInclude(Func<IQueryable<T>, IIncludableQueryable<T, object>> includeExpression)
        {
            Include = includeExpression;
        }

        protected virtual void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }

        protected virtual void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        {
            OrderByDescending = orderByDescendingExpression;
        }
    }

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

        private class ReplaceExpressionVisitor
            : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                return node == _oldValue ? _newValue : base.Visit(node);
            }
        }
    }
}