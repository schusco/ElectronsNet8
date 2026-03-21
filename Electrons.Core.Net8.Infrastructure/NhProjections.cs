using Electrons.Core.Net8.Entities;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Dialect.Function;
using NHibernate.Impl;
using NHibernate.Type;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Electrons.Core.Net8.Infrastructure
{
    public static class NhProjections
    {
        /// <summary>
        /// Creates a NHibernate projection which concatenates the results of the two provided expressions.
        /// </summary>
        /// <param name="firstArg">Expression which represents the first argument to concatenate.</param>
        /// <param name="secondArg">Expression which represents the second argument to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator</returns>
        public static IProjection Concat<T>(Expression<Func<T, object>> firstArg, Expression<Func<T, object>> secondArg)
        {
            return Concat(Projections.Property(firstArg), Projections.Property(secondArg));
        }

        /// <summary>
        /// Creates a NHibernate projection which concatenates the results of the two provided expressions.
        /// </summary>
        /// <param name="firstArg">Expression which represents the first argument to concatenate.</param>
        /// <param name="secondArg">Expression which represents the second argument to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator</returns>
        public static IProjection Concat(Expression<Func<object>> firstArg, Expression<Func<object>> secondArg)
        {
            return Concat(Projections.Property(firstArg), Projections.Property(secondArg));
        }

        /// <summary>
        /// Creates a NHibernate projection which concatenates the two provided projections.
        /// </summary>
        /// <param name="firstArg">Projection of the first argument to concatenate.</param>
        /// <param name="secondArg">Projection of the second argument to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator</returns>
        public static IProjection Concat(IProjection firstArg, IProjection secondArg)
        {
            return Projections.SqlFunction("concat", NHibernateUtil.String, firstArg, secondArg);
        }

        /// <summary>
        /// Creates a NHibernate projection which concatenates the provided arguments with the provided separator.
        /// </summary>
        /// <param name="args">A params array of the expressions you wish to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator.</returns>
        public static IProjection Concat(params Expression<Func<object>>[] args)
        {
            return args.Aggregate<Expression<Func<object>>, IProjection>(null,
                                                                         (current, expression) =>
                                                                         current == null
                                                                             ? Projections.Property(expression)
                                                                             : Concat(current,
                                                                                        Projections.Property(expression)));
        }

        /// <summary>
        /// Creates a NHibernate projection which concatenates the provided arguments with the provided separator.
        /// </summary>
        /// <param name="args">A params array of the NHibernate projections you wish to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator.</returns>
        public static IProjection Concat(params IProjection[] args)
        {
            return args.Aggregate<IProjection, IProjection>(null,
                                                            (current, projection) =>
                                                            current == null
                                                                ? projection
                                                                : Concat(current, projection));
        }

        /// <summary>
        /// Creates a NHibernate projection which concatenates the results of the two expressions with the provided separator.
        /// </summary>
        /// <param name="separator">Text to use to separate the two strings.</param>
        /// <param name="firstArg">Expression representing the first argument to concatenate.</param>
        /// <param name="secondArg">Expression representing the second argument to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator.</returns>
        public static IProjection ConcatWs<T>(string separator,
                                              Expression<Func<T, object>> firstArg,
                                              Expression<Func<T, object>> secondArg)
        {
            return ConcatWs(separator, Projections.Property(firstArg), Projections.Property(secondArg));
        }

        /// <summary>
        /// Creates a NHibernate projection which concatenates the results of the two expressions with the provided separator.
        /// </summary>
        /// <param name="separator">Text to use to separate the two strings.</param>
        /// <param name="firstArg">Expression representing the first argument to concatenate.</param>
        /// <param name="secondArg">Expression representing the second argument to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator.</returns>
        public static IProjection ConcatWs(string separator,
                                           Expression<Func<object>> firstArg, Expression<Func<object>> secondArg)
        {
            return ConcatWs(separator, Projections.Property(firstArg), Projections.Property(secondArg));
        }

        /// <summary>
        /// Creates an NHibernate projection which concatenates the results of the two projections with the provided separator
        /// </summary>
        /// <param name="separator">Text to use to separate the two strings.</param>
        /// <param name="first">NHibernate projection representing the first argument to concatenate.</param>
        /// <param name="second">NHibernate projection representing the second argument to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator.</returns>
        public static IProjection ConcatWs(string separator, IProjection first, IProjection second)
        {
            return Projections.SqlFunction("concat", NHibernateUtil.String, first, Projections.Constant(separator),
                                           second);
        }

        /// <summary>
        /// Creates a NHibernate projection which concatenates the provided arguments with the provided separator.
        /// </summary>
        /// <param name="separator">The text to use to separate the concatenated strings.</param>
        /// <param name="args">A params array of the NHibernate projections you wish to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator.</returns>
        public static IProjection ConcatWs(string separator, params IProjection[] args)
        {
            return args.Aggregate<IProjection, IProjection>(null,
                                                            (current, projection) =>
                                                            current == null
                                                                ? projection
                                                                : ConcatWs(separator, current, projection));
        }

        /// <summary>
        /// Creates a NHibernate projection which concatenates the provided arguments with the provided separator.
        /// </summary>
        /// <param name="separator">The text to use to separate the concatenated strings.</param>
        /// <param name="args">A params array of the expressions you wish to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator.</returns>
        public static IProjection ConcatWs<T>(string separator, params Expression<Func<T, object>>[] args)
        {
            return args.Aggregate<Expression<Func<T, object>>, IProjection>(null,
                                                                            (current, expression) =>
                                                                            current == null
                                                                                ? Projections.Property(expression)
                                                                                : ConcatWs(separator, current,
                                                                                           Projections.Property(
                                                                                               expression)));
        }

        /// <summary>
        /// Creates a NHibernate projection which concatenates the provided arguments with the provided separator.
        /// </summary>
        /// <param name="separator">The text to use to separate the concatenated strings.</param>
        /// <param name="args">A params array of the expressions you wish to concatenate.</param>
        /// <returns>A NHibernate projection of the DB2 || operator.</returns>
        public static IProjection ConcatWs(string separator, params Expression<Func<object>>[] args)
        {
            return args.Aggregate<Expression<Func<object>>, IProjection>(null,
                                                                         (current, expression) =>
                                                                         current == null
                                                                             ? Projections.Property(expression)
                                                                             : ConcatWs(separator, current,
                                                                                        Projections.Property(expression)));
        }

        /// <summary>
        /// Creates an NHibernate projection which null coalesces the provided expression with the provided default value.
        /// </summary>
        /// <param name="expression">The expression which represents the property to coalesce.</param>
        /// <param name="defaultVal">The default value returned if the expression resolved to null</param>
        /// <param name="type">An NHibernate type which represents the type of the property being coalesed the default type is NHibernateUtil.String.</param>
        /// <returns>A NHibernate projection of the coalesce() function.</returns>
        public static IProjection Coalesce(Expression<Func<object>> expression, object defaultVal, IType type = null)
        {
            if (type == null)
                type = NHibernateUtil.String;
            return Coalesce(Projections.Property(expression), defaultVal, type);
        }

        /// <summary>
        /// Creates an NHibernate projection which null coalesces the provided expression with the provided default value.
        /// </summary>
        /// <param name="expression">The expression which represents the property to coalesce.</param>
        /// <param name="defaultVal">The default value returned if the expression resolved to null</param>
        /// <param name="type">An NHibernate type which represents the type of the property being coalesed the default type is NHibernateUtil.String.</param>
        /// <returns>A NHibernate projection of the coalesce() function.</returns>
        public static IProjection Coalesce<T>(Expression<Func<T, object>> expression, object defaultVal, IType type = null)
        {
            if (type == null)
                type = NHibernateUtil.String;
            return Coalesce(Projections.Property(expression), defaultVal, type);
        }

        /// <summary>
        /// Creates an NHibernate projection which null coalesces the provided expression with the provided default value.
        /// </summary>
        /// <param name="projection">A NHIbernate projection which represents the property to coalesce.</param>
        /// <param name="defaultVal">The default value returned if the expression resolved to null</param>
        /// <param name="type">An NHibernate type which represents the type of the property being coalesed the default type is NHibernateUtil.String.</param>
        /// <returns>A NHibernate projection of the coalesce() function.</returns>
        public static IProjection Coalesce(IProjection projection, object defaultVal, IType type = null)
        {
            if (type == null)
                type = NHibernateUtil.String;
            return Projections.SqlFunction("coalesce", type, projection, Constant(defaultVal, type));
        }

        /// <summary>
        /// Creates an NHibernate projection which null coalesces the provided expression with the provided default value.
        /// </summary>
        /// <param name="expression">The expression which represents the property to coalesce.</param>
        /// <param name="defaultVal">The default value returned if the expression resolved to null</param>
        /// <param name="type">An NHibernate type which represents the type of the property being coalesed the default type is NHibernateUtil.String.</param>
        /// <returns>A NHibernate projection of the coalesce() function.</returns>
        public static IProjection Coalesce(Expression<Func<object>> expression, Expression<Func<object>> defaultVal,
                                           IType type = null)
        {
            return Coalesce(Projections.Property(expression), Projections.Property(defaultVal), type);
        }

        /// <summary>
        /// Creates an NHibernate projection which null coalesces the provided expression with the provided default value.
        /// </summary>
        /// <param name="projection">The projection which represents the property to coalesce.</param>
        /// <param name="defaultVal">The default value returned if the expression resolved to null</param>
        /// <param name="type">An NHibernate type which represents the type of the property being coalesed the default type is NHibernateUtil.String.</param>
        /// <returns>A NHibernate projection of the coalesce() function.</returns>
        public static IProjection Coalesce(IProjection projection, IProjection defaultVal, IType type = null)
        {
            if (type == null)
                type = NHibernateUtil.String;
            return Projections.SqlFunction("coalesce", type, projection, defaultVal);
        }

        /// <summary>
        /// Creates an NHibernate projection to return a substring of the provided property given the provided start index and the provided length.
        /// </summary>
        /// <param name="property">An expression which represents the property to substring</param>
        /// <param name="start">The start position in the string.</param>
        /// <param name="length">The length of the resulting string.</param>
        /// <returns>A NHibernate projection of the substring() function.</returns>
        public static IProjection Substring(Expression<Func<object>> property, int start, int length)
        {
            return Substring(Projections.Property(property), start, length);
        }

        /// <summary>
        /// Creates an NHibernate projection to return a substring of the provided property given the provided start index and the provided length.
        /// </summary>
        /// <param name="property">An expression which represents the property to substring</param>
        /// <param name="start">The start position in the string.</param>
        /// <param name="length">The length of the resulting string.</param>
        /// <returns>A NHibernate projection of the substring() function.</returns>
        public static IProjection Substring<T>(Expression<Func<T, object>> property, int start, int length)
        {
            return Substring(Projections.Property(property), start, length);
        }

        /// <summary>
        /// Creates an NHibernate projection to return a substring of the provided property given the provided start index and the provided length.
        /// </summary>
        /// <param name="property">An expression which represents the property to substring</param>
        /// <param name="start">The start position in the string.</param>
        /// <param name="length">The length of the resulting string.</param>
        /// <returns>A NHibernate projection of the substring() function.</returns>
        public static IProjection Substring(IProjection property, int start, int length)
        {
            return Projections.SqlFunction("substring", NHibernateUtil.String, property,
                                           Constant(start, NHibernateUtil.Int32),
                                           Constant(length, NHibernateUtil.Int32));
        }

        /// <summary>
        /// Creates a projection of a constant argument.
        /// </summary>
        /// <param name="val">The constant value for the projection</param>
        /// <param name="type">The nhibernate data type of the value (default is string)</param>
        /// <returns>A NHibernate projection of a constant.</returns>
        public static IProjection Constant(object val, IType type = null)
        {
            if (type == null)
                type = NHibernateUtil.String;
            return Projections.Cast(type, Projections.Constant(val));
        }

        /// <summary>
        /// Creates a NHibernate projection which returns the trimmed value of the property in the member expression
        /// </summary>
        /// <param name="expression">The member expression you which to apply the trim function to.</param>
        /// <returns>A NHibernate projection of the TRIM() function.</returns>
        public static IProjection Trim(Expression<Func<object>> expression)
        {
            return Trim(Projections.Property(expression));
        }

        /// <summary>
        /// Creates a NHibernate projection which returns the trimmed value of the property in the member expression
        /// </summary>
        /// <param name="expression">The member expression you which to apply the trim function to.</param>
        /// <returns>A NHibernate projection of the TRIM() function.</returns>
        public static IProjection Trim<T>(Expression<Func<T, object>> expression)
        {
            return Trim(Projections.Property(expression));
        }

        /// <summary>
        /// Creates a NHibernate projection which returns the trimmed value of the property in the provided projection.
        /// </summary>
        /// <param name="property">A NHibernate projection of the property to trim.</param>
        /// <returns>A NHibernate projection of the TRIM() function.</returns>
        public static IProjection Trim(IProjection property)
        {
            return Projections.SqlFunction("trim", NHibernateUtil.String, property);
        }

        /// <summary>
        /// Creates a NHibernate projection which returns the upper case equivalent of the property in the provided expression.
        /// </summary>
        /// <param name="expression">The member expression you which to apply the upper function to.</param>
        /// <returns>A NHinernate projection of the Upper() sql function.</returns>
        public static IProjection Upper(Expression<Func<object>> expression)
        {
            return Upper(Projections.Property(expression));
        }

        /// <summary>
        /// Creates a NHibernate projection which returns the upper case equivalent of the property in the provided expression.
        /// </summary>
        /// <param name="expression">The member expression you which to apply the upper function to.</param>
        /// <returns>A NHinernate projection of the Upper() sql function.</returns>
        public static IProjection Upper<T>(Expression<Func<T, object>> expression)
        {
            return Upper(Projections.Property(expression));
        }

        /// <summary>
        /// Creates a NHibernate projection which returns the upper case equivalent of the property in the provided projection.
        /// </summary>
        /// <param name="property">A NHibernate projection of the property to apply the upper function to.</param>
        /// <returns>A NHinernate projection of the Upper() sql function.</returns>
        public static IProjection Upper(IProjection property)
        {
            return Projections.SqlFunction("upper", NHibernateUtil.String, property);
        }

        /// <summary>
        /// Creates a NHibernate projection which returns the lower case equivalent of the property in the provided expression.
        /// </summary>
        /// <param name="expression">The member expression you which to apply the lower function to.</param>
        /// <returns>A NHinernate projection of the Lower() sql function.</returns>
        public static IProjection Lower(Expression<Func<object>> expression)
        {
            return Lower(Projections.Property(expression));
        }

        /// <summary>
        /// Creates a NHibernate projection which returns the lower case equivalent of the property in the provided expression.
        /// </summary>
        /// <param name="expression">The member expression you which to apply the lower function to.</param>
        /// <returns>A NHinernate projection of the Lower() sql function.</returns>
        public static IProjection Lower<T>(Expression<Func<T, object>> expression)
        {
            return Lower(Projections.Property(expression));
        }

        /// <summary>
        /// Creates a NHibernate projection which returns the lower case equivalent of the property in the provided projection.
        /// </summary>
        /// <param name="property">A NHibernate projection of the property to apply the lower function to.</param>
        /// <returns>A NHinernate projection of the Lower() sql function.</returns>
        public static IProjection Lower(IProjection property)
        {
            return Projections.SqlFunction("lower", NHibernateUtil.String, property);
        }

        /// <summary>
        /// Creates a projection of the property in the provided expression cast to the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object passed into the member expression.</typeparam>
        /// <param name="expression">A member expression which represents the property.</param>
        /// <param name="type">The data type of the property, default is string.</param>
        /// <returns>A NHibernate projection of a member expression.</returns>
        public static IProjection Property<T>(Expression<Func<T, object>> expression, IType type = null)
        {
            return Property(Projections.Property(expression), type);
        }

        /// <summary>
        /// Creates a projection of the property in the provided expression cast to the specified type.
        /// </summary>
        /// <param name="expression">A member expression which represents the property.</param>
        /// <param name="type">The data type of the property, default is string.</param>
        /// <returns>A NHibernate projection of a member expression.</returns>
        public static IProjection Property(Expression<Func<object>> expression, IType type = null)
        {
            return Property(Projections.Property(expression), type);
        }

        /// <summary>
        /// Creates a projection of the property in the provided expression cast to the specified type.
        /// </summary>
        /// <param name="projection">A NHibernate projection of a property.</param>
        /// <param name="type">The data type of the property, default is string.</param>
        /// <returns>A NHibernate projection of a member expression.</returns>
        public static IProjection Property(IProjection projection, IType type = null)
        {
            if (type == null)
                type = NHibernateUtil.String;
            return Projections.Cast(type, projection);
        }

        static PropertyProjection CreateProjection<T>(Expression<Func<IPerson>> aliasExpression, Expression<Func<T, object>> propertyExpression)
        {
            var alias = ExpressionProcessor.FindMemberExpression(aliasExpression.Body);
            var property = ExpressionProcessor.FindMemberExpression(propertyExpression.Body);
            return Projections.Property(string.Format("{0}.{1}", alias, property));
        }

        /// <summary>
        /// Returns a projection of a users name in the last name, first name format.
        /// </summary>
        /// <param name="userAlias">An expression which represents the an IPerson object.</param>
        /// <returns>An NHibernate IProjection object.</returns>
        public static IProjection NameFirstLastProjection(Expression<Func<IPerson>> userAlias)
        {
            var first = CreateProjection<IPerson>(userAlias, u => u.FirstName);
            var last = CreateProjection<IPerson>(userAlias, u => u.LastName);
            return ConcatWs(" ", first, last);
        }

        /// <summary>
        /// Returns a projection of a users name in first name last name format.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="userAlias">An expression which represents the an IPerson object.</param>
        /// <returns></returns>
        public static IProjection NameLastFirstProjection(Expression<Func<IPerson>> userAlias)
        {
            var first = CreateProjection<IPerson>(userAlias, u => u.FirstName);
            var last = CreateProjection<IPerson>(userAlias, u => u.LastName);
            return ConcatWs(", ", last, first);
        }
        internal static IProjection NameLastFirstInital(Expression<Func<IPerson>> p)
        {
            var first = CreateProjection<IPerson>(p, u => u.LastName);
            var last = Substring(CreateProjection<IPerson>(p, u => u.FirstName), 1, 1);
            return ConcatWs(", ", first, last);
        }
        /// <summary>
        /// creates a NHibernate projection of the sql month function.
        /// </summary>
        /// <param name="expression">A member expression which returns a date you which to get the month of.</param>
        /// <returns>A NHibernate projection of the sql month function</returns>
        public static IProjection Month(Expression<Func<object>> expression)
        {
            return Month(Projections.Property(expression));
        }

        /// <summary>
        /// creates a NHibernate projection of the sql month function.
        /// </summary>
        /// <param name="expression">A member expression which returns a date you which to get the month of.</param>
        /// <returns>A NHibernate projection of the sql month function</returns>
        public static IProjection Month<T>(Expression<Func<T, object>> expression)
        {
            return Month(Projections.Property(expression));
        }

        /// <summary>
        /// creates a NHibernate projection of the sql month function.
        /// </summary>
        /// <param name="projection">A NHibernate property projection which returns a date you which to get the month of.</param>
        /// <returns>A NHibernate projection of the sql month function</returns>
        public static IProjection Month(IProjection projection)
        {
            return Projections.SqlFunction("month", NHibernateUtil.Int32, projection);
        }

        public static IProjection Day(Expression<Func<object>> expression) => Day(Projections.Property(expression));

        /// <summary>
        /// creates a NHibernate projection of the sql month function.
        /// </summary>
        /// <param name="expression">A member expression which returns a date you which to get the month of.</param>
        /// <returns>A NHibernate projection of the sql month function</returns>
        public static IProjection Day<T>(Expression<Func<T, object>> expression) => Day(Projections.Property(expression));

        /// <summary>
        /// creates a NHibernate projection of the sql month function.
        /// </summary>
        /// <param name="projection">A NHibernate property projection which returns a date you which to get the month of.</param>
        /// <returns>A NHibernate projection of the sql month function</returns>
        public static IProjection Day(IProjection projection) => Projections.SqlFunction("day", NHibernateUtil.Int32, projection);
        /// <summary>
        /// creates a NHibernate projection of the sql year function.
        /// </summary>
        /// <param name="expression">A member expression which returns a date you which to get the year of.</param>
        /// <returns>A NHibernate projection of the sql year function</returns>
        public static IProjection Year(Expression<Func<object>> expression)
        {
            return Year(Projections.Property(expression));
        }

        /// <summary>
        /// creates a NHibernate projection of the sql year function.
        /// </summary>
        /// <param name="expression">A member expression which returns a date you which to get the year of.</param>
        /// <returns>A NHibernate projection of the sql year function</returns>
        public static IProjection Year<T>(Expression<Func<T, object>> expression)
        {
            return Year(Projections.Property(expression));
        }

        /// <summary>
        /// creates a NHibernate projection of the sql year function.
        /// </summary>
        /// <param name="projection">A NHibernate property projection which returns a date you which to get the year of.</param>
        /// <returns>A NHibernate projection of the sql year function</returns>
        public static IProjection Year(IProjection projection)
        {
            return Projections.SqlFunction("year", NHibernateUtil.Int32, projection);
        }
        /// <summary>
        /// Returns a projection of the sum of the provided expressions.
        /// </summary>
        /// <param name="arg1">An expression which returns the first argument for this operation.</param>
        /// <param name="arg2">An expression which returns the second argument for this operation.</param>
        /// <returns></returns>
        public static IProjection Add<T>(Expression<Func<T, object>> arg1, Expression<Func<T, object>> arg2)
        {
            return Add(Property(arg1, NHibernateUtil.Double), Property(arg2, NHibernateUtil.Double));
        }
        /// <summary>
        /// Returns a projection of the sum of the provided expressions.
        /// </summary>
        /// <param name="arg1">An expression which returns the first argument for this operation.</param>
        /// <param name="arg2">An expression which returns the second argument for this operation.</param>
        /// <returns></returns>
        public static IProjection Add(Expression<Func<object>> arg1, Expression<Func<object>> arg2)
        {
            return Add(Property(arg1, NHibernateUtil.Double), Property(arg2, NHibernateUtil.Double));
        }
        /// <summary>
        /// Returns a projection of the sum of the provided expressions.
        /// </summary>
        /// <param name="arg1">A projection which returns the first argument for this operation.</param>
        /// <param name="arg2">A projection which returns the second argument for this operation.</param>
        /// <returns></returns>
        public static IProjection Add(IProjection arg1, IProjection arg2)
        {
            return Projections.SqlFunction(new VarArgsSQLFunction("( ", " + ", " )"), NHibernateUtil.Double, arg1, arg2);
        }
        /// <summary>
        /// Returns a projection of the difference of the provided expressions.
        /// </summary>
        /// <param name="arg1">An expression which returns the first argument for this operation.</param>
        /// <param name="arg2">An expression which returns the second argument for this operation.</param>
        /// <returns></returns>
        public static IProjection Subtract<T>(Expression<Func<T, object>> arg1, Expression<Func<T, object>> arg2)
        {
            return Subtract(Property(arg1, NHibernateUtil.Double), Property(arg2, NHibernateUtil.Double));
        }
        /// <summary>
        /// Returns a projection of the difference of the provided expressions.
        /// </summary>
        /// <param name="arg1">An expression which returns the first argument for this operation.</param>
        /// <param name="arg2">An expression which returns the second argument for this operation.</param>
        /// <returns></returns>
        public static IProjection Subtract(Expression<Func<object>> arg1, Expression<Func<object>> arg2)
        {
            return Subtract(Property(arg1, NHibernateUtil.Double), Property(arg2, NHibernateUtil.Double));
        }
        /// <summary>
        /// Returns a projection of the different of the provided expressions.
        /// </summary>
        /// <param name="arg1">A projection which returns the first argument for this operation.</param>
        /// <param name="arg2">A projection which returns the second argument for this operation.</param>
        /// <returns></returns>
        public static IProjection Subtract(IProjection arg1, IProjection arg2)
        {
            return Projections.SqlFunction(new VarArgsSQLFunction("( ", " - ", " )"), NHibernateUtil.Double, arg1, arg2);
        }
        /// <summary>
        /// Returns a projection of the product of the provided expressions.
        /// </summary>
        /// <param name="arg1">An expression which returns the first argument for this operation.</param>
        /// <param name="arg2">An expression which returns the second argument for this operation</param>
        /// <returns></returns>
        public static IProjection Multiply<T>(Expression<Func<T, object>> arg1, Expression<Func<T, object>> arg2)
        {
            return Multiply(Property(arg1, NHibernateUtil.Double), Property(arg2, NHibernateUtil.Double));
        }
        /// <summary>
        /// Returns a projection of the product of the provided expressions.
        /// </summary>
        /// <param name="arg1">An expression which returns the first argument for this operation.</param>
        /// <param name="arg2">An expression which returns the second argument for this operation</param>
        /// <returns></returns>
        public static IProjection Multiply(Expression<Func<object>> arg1, Expression<Func<object>> arg2)
        {
            return Multiply(Property(arg1, NHibernateUtil.Double), Property(arg2, NHibernateUtil.Double));
        }
        /// <summary>
        /// Returns a projection of the product of the provided expressions.
        /// </summary>
        /// <param name="arg1">A projection which returns the first argument for this operation.</param>
        /// <param name="arg2">A projection which returns the second argument for this operation</param>
        /// <returns></returns>
        public static IProjection Multiply(IProjection arg1, IProjection arg2)
        {
            return Projections.SqlFunction(new VarArgsSQLFunction("( ", " * ", " )"), NHibernateUtil.Double, arg1, arg2);
        }
        /// <summary>
        /// Returns a projection which represents the quotient of the provided expressions.
        /// </summary>
        /// <param name="arg1">An expression which returns the dividend for this operation.</param>
        /// <param name="arg2">An expression which returns the divisor for this operation.</param>
        /// <returns></returns>
        public static IProjection Divide<T>(Expression<Func<T, object>> arg1, Expression<Func<T, object>> arg2)
        {
            return Divide(Property(arg1, NHibernateUtil.Double), Property(arg2, NHibernateUtil.Double));
        }
        /// <summary>
        /// Returns a projection which represents the quotient of the provided expressions.
        /// </summary>
        /// <param name="arg1">An expression which returns the dividend for this operation.</param>
        /// <param name="arg2">An expression which returns the divisor for this operation.</param>
        /// <returns></returns>
        public static IProjection Divide(Expression<Func<object>> arg1, Expression<Func<object>> arg2)
        {
            return Divide(Property(arg1, NHibernateUtil.Double), Property(arg2, NHibernateUtil.Double));
        }
        /// <summary>
        /// Returns a projection which represents the quotient of the provided projections.
        /// </summary>
        /// <param name="arg1">A projection which returns the dividend for this operation.</param>
        /// <param name="arg2">A projection which returns the divisor for this operation.</param>
        /// <returns></returns>
        public static IProjection Divide(IProjection arg1, IProjection arg2)
        {
            return Projections.SqlFunction(new VarArgsSQLFunction("( ", " / ", " )"), NHibernateUtil.Double, arg1, arg2);
        }
        /// <summary>
        /// Returns a NHibernate projection of the DB2 'days' function from the provided expression.
        /// </summary>
        /// <param name="arg1">An expression which returns the argument for the function.</param>
        /// <returns></returns>
        public static IProjection Days(Expression<Func<object>> arg1)
        {
            return Days(Property(arg1, NHibernateUtil.DateTime));
        }
        /// <summary>
        /// Returns a NHibernate projection of the DB2 'days' function from the provided projection.
        /// </summary>
        /// <param name="arg1">A NHibernate projection which returns the argument for the function.</param>
        /// <returns></returns>
        public static IProjection Days(IProjection arg1)
        {
            return Projections.SqlFunction("days", NHibernateUtil.DateTime, arg1);
        }
    }


}
