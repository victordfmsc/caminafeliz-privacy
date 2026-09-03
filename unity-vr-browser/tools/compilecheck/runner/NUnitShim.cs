// Just enough NUnit to compile and execute the project's EditMode tests here.
using System;
using System.Collections.Generic;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Arguments { get; }
        public TestCaseAttribute(object arg) { Arguments = new[] { arg }; }
        public TestCaseAttribute(object a, object b) { Arguments = new[] { a, b }; }
        public TestCaseAttribute(params object[] args) { Arguments = args ?? new object[] { null }; }
    }

    public interface IConstraint { bool Matches(object actual); string Describe(); }

    public class StartsWithConstraint : IConstraint
    {
        private readonly string m_prefix;
        public StartsWithConstraint(string prefix) { m_prefix = prefix; }
        public bool Matches(object actual) => actual is string s && s.StartsWith(m_prefix, StringComparison.Ordinal);
        public string Describe() => $"string starting with \"{m_prefix}\"";
    }

    public static class Does
    {
        public static IConstraint StartWith(string prefix) => new StartsWithConstraint(prefix);
    }

    public class AssertionException : Exception { public AssertionException(string m) : base(m) { } }

    public static class Assert
    {
        public static void AreEqual(object expected, object actual)
        {
            if (!Equals(expected, actual))
                throw new AssertionException($"expected <{Show(expected)}> but was <{Show(actual)}>");
        }

        public static void AreEqual(object expected, object actual, string message)
        {
            if (!Equals(expected, actual))
                throw new AssertionException($"{message}: expected <{Show(expected)}> but was <{Show(actual)}>");
        }

        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition) throw new AssertionException(message ?? "expected true");
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition) throw new AssertionException(message ?? "expected false");
        }

        public static void That(object actual, IConstraint constraint)
        {
            if (!constraint.Matches(actual))
                throw new AssertionException($"expected {constraint.Describe()} but was <{Show(actual)}>");
        }

        private static string Show(object o) => o == null ? "null" : o.ToString();
    }
}
