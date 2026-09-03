// Reflection test runner: finds [Test] and [TestCase] methods and runs them.
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public static class Runner
{
    public static int Main()
    {
        var assembly = Assembly.GetExecutingAssembly();
        int passed = 0, failed = 0;

        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes<TestAttribute>().Any()
                         || m.GetCustomAttributes<TestCaseAttribute>().Any())
                .ToArray();

            if (methods.Length == 0)
                continue;

            Console.WriteLine($"\n{type.Name}");

            foreach (var method in methods)
            {
                var cases = method.GetCustomAttributes<TestCaseAttribute>().ToArray();

                if (cases.Length == 0)
                {
                    Run(type, method, Array.Empty<object>(), ref passed, ref failed);
                    continue;
                }

                foreach (var testCase in cases)
                    Run(type, method, testCase.Arguments, ref passed, ref failed);
            }
        }

        Console.WriteLine($"\n================================");
        Console.WriteLine($"  pasados: {passed}   fallidos: {failed}");
        Console.WriteLine($"================================");
        return failed == 0 ? 0 : 1;
    }

    private static void Run(Type type, MethodInfo method, object[] args, ref int passed, ref int failed)
    {
        var label = args.Length == 0
            ? method.Name
            : $"{method.Name}({string.Join(", ", args.Select(a => a == null ? "null" : $"\"{a}\""))})";

        try
        {
            var instance = Activator.CreateInstance(type);
            method.Invoke(instance, args);
            passed++;
            Console.WriteLine($"  PASS  {label}");
        }
        catch (TargetInvocationException exception)
        {
            failed++;
            Console.WriteLine($"  FAIL  {label}\n          {exception.InnerException?.Message}");
        }
        catch (Exception exception)
        {
            failed++;
            Console.WriteLine($"  ERROR {label}\n          {exception.Message}");
        }
    }
}
