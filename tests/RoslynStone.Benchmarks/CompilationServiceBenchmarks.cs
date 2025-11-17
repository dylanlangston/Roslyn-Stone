using BenchmarkDotNet.Attributes;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Benchmarks;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class CompilationServiceBenchmarks
{
    private CompilationService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new CompilationService();
    }

    [Benchmark]
    public bool CompileSimpleClass()
    {
        var result = _service.Compile(
            @"
            using System;
            public class TestClass
            {
                public int Add(int a, int b) => a + b;
            }
        "
        );
        return result.Success;
    }

    [Benchmark]
    public bool CompileComplexCode()
    {
        var result = _service.Compile(
            @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            public class Calculator
            {
                public int Sum(IEnumerable<int> numbers) => numbers.Sum();
                public double Average(IEnumerable<int> numbers) => numbers.Average();
                public int Max(IEnumerable<int> numbers) => numbers.Max();
            }
        "
        );
        return result.Success;
    }

    [Benchmark]
    public bool CompileWithError()
    {
        var result = _service.Compile("int x = \"string\";");
        return result.Success;
    }

    [Benchmark]
    public bool CompileMultipleClasses()
    {
        var result = _service.Compile(
            @"
            using System;
            
            public class Person
            {
                public string Name { get; set; }
                public int Age { get; set; }
            }
            
            public class PersonService
            {
                public bool IsAdult(Person person) => person.Age >= 18;
            }
        "
        );
        return result.Success;
    }
}
