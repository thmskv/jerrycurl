using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fixie;
using Jerrycurl.Mvc;

namespace Jerrycurl.Test;

public abstract class DatabaseConvention : ITestProject, IDiscovery, IExecution
{
    public virtual bool Skip { get; } = false;
    public virtual string SkipReason { get; } = "Test skipped.";

    public abstract void Configure(DomainOptions options);

    public void Configure(TestConfiguration configuration, TestEnvironment environment)
        => configuration.Conventions.Add(this, this);

    public IEnumerable<Type> TestClasses(IEnumerable<Type> concreteClasses)
        => concreteClasses.Where(t => t.Name.EndsWith("Tests"));

    public IEnumerable<MethodInfo> TestMethods(IEnumerable<MethodInfo> publicMethods)
        => publicMethods.Where(m => !m.IsStatic);

    public async Task Run(TestSuite testSuite)
    {
        foreach (TestClass testClass in testSuite.TestClasses)
        {
            if (this.Skip)
            {
                foreach (Fixie.Test test in testClass.Tests)
                    await test.Skip(this.SkipReason);

                continue;
            }

            object instance = testClass.Construct();

            try
            {
                foreach (Fixie.Test test in testClass.Tests)
                    await test.Run(instance);
            }
            finally
            {
                (instance as IDisposable)?.Dispose();
            }
        }
    }

    protected static string GetEnvironmentVariable(string variableName) => Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine) ??
                                                                           Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User) ??
                                                                           Environment.GetEnvironmentVariable(variableName);
}
