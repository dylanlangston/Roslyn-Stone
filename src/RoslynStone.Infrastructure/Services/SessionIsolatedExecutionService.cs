using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Helpers;
using RoslynStone.Infrastructure.Models;

namespace RoslynStone.Infrastructure.Services;

/// <summary>
/// Service for executing C# code with proper session isolation using UnloadableAssemblyLoadContext
/// Each session gets its own AssemblyLoadContext that can be properly unloaded
/// Supports NuGet packages with true isolation per context
/// </summary>
public class SessionIsolatedExecutionService
{
    private readonly CompilationService _compilationService;
    private readonly SecurityConfiguration _securityConfig;
    private readonly ILogger<SessionIsolatedExecutionService>? _logger;
    private readonly ConcurrentDictionary<string, SessionContext> _contexts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionIsolatedExecutionService"/> class
    /// </summary>
    public SessionIsolatedExecutionService(
        CompilationService compilationService,
        SecurityConfiguration? securityConfig = null,
        ILogger<SessionIsolatedExecutionService>? logger = null
    )
    {
        _compilationService = compilationService;
        _securityConfig = securityConfig ?? SecurityConfiguration.CreateProductionDefaults();
        _logger = logger;
    }

    /// <summary>
    /// Execute C# code in an isolated session context with NuGet package support
    /// </summary>
    /// <param name="contextId">Session context ID</param>
    /// <param name="code">C# code to execute</param>
    /// <param name="packageReferences">Optional metadata references for NuGet packages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result with output, return value, errors, and timing</returns>
    public async Task<ExecutionResult> ExecuteInContextAsync(
        string contextId,
        string code,
        IEnumerable<MetadataReference>? packageReferences = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextId);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var stopwatch = Stopwatch.StartNew();

        // Get or create context
        var sessionContext = _contexts.GetOrAdd(
            contextId,
            _ => new SessionContext(
                new UnloadableAssemblyLoadContext(_securityConfig.BlockedAssemblies, _logger)
            )
        );

        try
        {
            // Compile the code with package references
            var compilationResult = CompileWithReferences(code, packageReferences);

            if (!compilationResult.Success)
            {
                stopwatch.Stop();
                return new ExecutionResult
                {
                    Success = false,
                    Errors =
                        compilationResult
                            .Diagnostics?.Select(d => new CompilationError
                            {
                                Code = d.Id,
                                Message = d.GetMessage(),
                                Severity = d.Severity.ToString(),
                                Line = d.Location.GetLineSpan().StartLinePosition.Line,
                                Column = d.Location.GetLineSpan().StartLinePosition.Character,
                            })
                            .ToList() ?? [],
                    ExecutionTime = stopwatch.Elapsed,
                };
            }

            // Load and execute in the session's isolated context
            var result = await ExecuteInUnloadableContextAsync(
                sessionContext,
                compilationResult,
                cancellationToken
            );

            stopwatch.Stop();
            // Return new result with execution time set
            return new ExecutionResult
            {
                Success = result.Success,
                ReturnValue = result.ReturnValue,
                Output = result.Output,
                Errors = result.Errors,
                Warnings = result.Warnings,
                ExecutionTime = stopwatch.Elapsed,
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            return new ExecutionResult
            {
                Success = false,
                Errors =
                [
                    new CompilationError
                    {
                        Code = "EXECUTION_ERROR",
                        Message = ex.Message,
                        Severity = "Error",
                    },
                ],
                ExecutionTime = stopwatch.Elapsed,
            };
        }
    }

    /// <summary>
    /// Compile code with optional package metadata references
    /// </summary>
    private CompilationResult CompileWithReferences(
        string code,
        IEnumerable<MetadataReference>? packageReferences
    )
    {
        var assemblyName = $"SessionAssembly_{Guid.NewGuid():N}";

        // Transform REPL-style code to console application code
        // Convert "return expression" to "Console.WriteLine(expression)"
        var transformedCode = TransformReplCodeToTopLevelStatements(code);

        // Parse with C# preview to support top-level statements and top-level await
        var parseOptions = new CSharpParseOptions(
            kind: SourceCodeKind.Regular,
            languageVersion: LanguageVersion.Preview
        );

        var syntaxTree = CSharpSyntaxTree.ParseText(transformedCode, parseOptions);

        // Perform security checks if enabled
        if (_securityConfig.EnableApiRestrictions)
        {
            var securityDiagnostics = AnalyzeForDangerousApis(syntaxTree);
            if (securityDiagnostics.Any())
            {
                return new CompilationResult
                {
                    Success = false,
                    Diagnostics = securityDiagnostics,
                    ErrorMessages = securityDiagnostics
                        .Select(d => $"{d.Id}: {d.GetMessage()}")
                        .ToList(),
                };
            }
        }

        // Get default references
        var defaultOptions = MetadataReferenceHelper.GetDefaultScriptOptions();
        var references = defaultOptions
            .MetadataReferences.OfType<PortableExecutableReference>()
            .ToList();

        // Add package references if provided
        if (packageReferences != null)
        {
            references.AddRange(packageReferences.OfType<PortableExecutableReference>());
        }

        // Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: false
            )
        );

        // Emit to memory
        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();

        var emitResult = compilation.Emit(peStream, pdbStream);

        if (!emitResult.Success)
        {
            var failures = emitResult
                .Diagnostics.Where(d =>
                    d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error
                )
                .ToList();

            return new CompilationResult
            {
                Success = false,
                Diagnostics = failures,
                ErrorMessages = failures.Select(d => $"{d.Id}: {d.GetMessage()}").ToList(),
            };
        }

        // Copy to new streams
        var assemblyStream = new MemoryStream();
        var symbolsStream = new MemoryStream();
        peStream.Seek(0, SeekOrigin.Begin);
        pdbStream.Seek(0, SeekOrigin.Begin);
        peStream.CopyTo(assemblyStream);
        pdbStream.CopyTo(symbolsStream);
        assemblyStream.Seek(0, SeekOrigin.Begin);
        symbolsStream.Seek(0, SeekOrigin.Begin);

        return new CompilationResult
        {
            Success = true,
            AssemblyName = assemblyName,
            AssemblyStream = assemblyStream,
            SymbolsStream = symbolsStream,
        };
    }

    /// <summary>
    /// Execute assembly in the session's isolated context with memory and timeout limits
    /// </summary>
    private async Task<ExecutionResult> ExecuteInUnloadableContextAsync(
        SessionContext sessionContext,
        CompilationResult compilationResult,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(compilationResult.AssemblyStream);

        try
        {
            // Load assembly into session's isolated context
            var assembly = sessionContext.LoadContext.LoadFromStream(
                compilationResult.AssemblyStream
            );

            // Find entry point
            var entryPoint = FindEntryPoint(assembly);
            if (entryPoint == null)
            {
                return new ExecutionResult
                {
                    Success = false,
                    Errors =
                    [
                        new CompilationError
                        {
                            Code = "NO_ENTRY_POINT",
                            Message =
                                "No entry point found. Ensure code has Main method or top-level statements.",
                            Severity = "Error",
                        },
                    ],
                    ExecutionTime = TimeSpan.Zero,
                };
            }

            // Execute with memory monitoring and timeout
            return await ExecuteWithLimitsAsync(entryPoint, cancellationToken);
        }
        finally
        {
            // Dispose streams
            if (compilationResult.AssemblyStream != null)
                await compilationResult.AssemblyStream.DisposeAsync();
            if (compilationResult.SymbolsStream != null)
                await compilationResult.SymbolsStream.DisposeAsync();
        }
    }

    /// <summary>
    /// Execute entry point with timeout and memory monitoring
    /// </summary>
    private async Task<ExecutionResult> ExecuteWithLimitsAsync(
        MethodInfo entryPoint,
        CancellationToken cancellationToken
    )
    {
        var outputBuilder = new StringBuilder();
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            await using var outputWriter = new StringWriter(outputBuilder);
            Console.SetOut(outputWriter);
            Console.SetError(outputWriter);

            // Get memory baseline
            var memoryBefore = GC.GetTotalMemory(forceFullCollection: false);

            // Create linked cancellation token source for timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_securityConfig.EnableExecutionTimeout)
            {
                cts.CancelAfter(_securityConfig.ExecutionTimeout);
            }

            // Execute entry point on separate task to allow timeout/cancellation
            Task<object?> executionTask = Task.Run(
                () =>
                {
                    try
                    {
                        return entryPoint.Invoke(
                            null,
                            entryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]
                        );
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        // Unwrap reflection exceptions
                        throw ex.InnerException;
                    }
                },
                cts.Token
            );

            object? result;

            // Wait for execution with timeout
            try
            {
                result = await executionTask.WaitAsync(
                    _securityConfig.EnableExecutionTimeout
                        ? _securityConfig.ExecutionTimeout
                        : Timeout.InfiniteTimeSpan,
                    cts.Token
                );
            }
            catch (TimeoutException)
            {
                return new ExecutionResult
                {
                    Success = false,
                    Errors =
                    [
                        new CompilationError
                        {
                            Code = "EXECUTION_TIMEOUT",
                            Message =
                                $"Execution exceeded timeout limit of {_securityConfig.ExecutionTimeout.TotalSeconds} seconds",
                            Severity = "Error",
                        },
                    ],
                    Output = outputBuilder.ToString(),
                    ExecutionTime = _securityConfig.ExecutionTimeout,
                };
            }

            // Handle async Task return types
            if (result is Task task)
            {
                // Run with memory monitoring
                await MonitorExecutionAsync(task, memoryBefore, cts.Token);
            }
            else if (_securityConfig.EnableMemoryLimits)
            {
                // Check memory for sync execution
                var memoryNow = GC.GetTotalMemory(false);
                if (
                    _securityConfig.MaxMemoryBytes > 0
                    && (memoryNow - memoryBefore) > _securityConfig.MaxMemoryBytes
                )
                {
                    throw new InsufficientMemoryException(
                        $"Execution exceeded memory limit of {_securityConfig.MaxMemoryBytes / (1024 * 1024)} MB"
                    );
                }
            }

            await Console.Out.FlushAsync(cancellationToken);
            await outputWriter.FlushAsync(cancellationToken);

            // For transformed REPL code, the output IS the return value
            var output = outputBuilder.ToString();
            var returnValue = result is Task ? null : result;

            // If there's console output but no explicit return value, use the output as the return value
            if (returnValue == null && !string.IsNullOrWhiteSpace(output))
            {
                returnValue = output.Trim();
            }

            return new ExecutionResult
            {
                Success = true,
                ReturnValue = returnValue,
                Output = output,
                ExecutionTime = TimeSpan.Zero, // Will be set by caller
            };
        }
        catch (OperationCanceledException)
        {
            return new ExecutionResult
            {
                Success = false,
                Errors =
                [
                    new CompilationError
                    {
                        Code = "EXECUTION_TIMEOUT",
                        Message =
                            $"Execution exceeded timeout limit of {_securityConfig.ExecutionTimeout.TotalSeconds} seconds",
                        Severity = "Error",
                    },
                ],
                Output = outputBuilder.ToString(),
                ExecutionTime = _securityConfig.ExecutionTimeout,
            };
        }
        catch (InsufficientMemoryException ex)
        {
            return new ExecutionResult
            {
                Success = false,
                Errors =
                [
                    new CompilationError
                    {
                        Code = "MEMORY_LIMIT_EXCEEDED",
                        Message = ex.Message,
                        Severity = "Error",
                    },
                ],
                Output = outputBuilder.ToString(),
                ExecutionTime = TimeSpan.Zero,
            };
        }
        catch (Exception ex)
        {
            return new ExecutionResult
            {
                Success = false,
                Errors =
                [
                    new CompilationError
                    {
                        Code = "RUNTIME_ERROR",
                        Message = ex.InnerException?.Message ?? ex.Message,
                        Severity = "Error",
                    },
                ],
                Output = outputBuilder.ToString(),
                ExecutionTime = TimeSpan.Zero,
            };
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>
    /// Monitor task execution with memory polling
    /// </summary>
    private async Task MonitorExecutionAsync(
        Task executionTask,
        long memoryBaseline,
        CancellationToken cancellationToken
    )
    {
        if (!_securityConfig.EnableMemoryLimits || _securityConfig.MaxMemoryBytes <= 0)
        {
            await executionTask;
            return;
        }

        // Poll memory during execution
        while (!executionTask.IsCompleted)
        {
            await Task.Delay(50, cancellationToken); // Poll every 50ms

            var memoryNow = GC.GetTotalMemory(false);
            var memoryUsed = memoryNow - memoryBaseline;

            if (memoryUsed > _securityConfig.MaxMemoryBytes)
            {
                throw new InsufficientMemoryException(
                    $"Execution exceeded memory limit of {_securityConfig.MaxMemoryBytes / (1024 * 1024)} MB (used: {memoryUsed / (1024 * 1024)} MB)"
                );
            }

            // Check if task completed while we were checking memory
            if (executionTask.IsCompleted)
                break;
        }

        // Await final result
        await executionTask;
    }

    /// <summary>
    /// Find entry point in assembly (Main method or top-level statements)
    /// </summary>
    private static MethodInfo? FindEntryPoint(Assembly assembly)
    {
        // Look for top-level statements entry point first
        var entryPointMethod = assembly
            .GetTypes()
            .SelectMany(t =>
                t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            )
            .FirstOrDefault(m => m.Name == "<Main>$");

        if (entryPointMethod != null)
            return entryPointMethod;

        // Look for traditional Main method in Program class
        var programType = assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "Program" || t.Name.Contains("Program"));

        return programType?.GetMethod(
            "Main",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
        );
    }

    /// <summary>
    /// Unload a session context and free its resources
    /// </summary>
    /// <param name="contextId">Context ID to unload</param>
    /// <returns>True if context was found and unloaded, false if not found</returns>
    public async Task<bool> UnloadContextAsync(string contextId)
    {
        if (!_contexts.TryRemove(contextId, out var sessionContext))
        {
            return false;
        }

        // Unload the AssemblyLoadContext
        sessionContext.LoadContext.Unload();

        // Force garbage collection and verify unloading
        var weakRef = new WeakReference(sessionContext.LoadContext, trackResurrection: true);

        await Task.Run(() =>
        {
            for (int i = 0; i < 10 && weakRef.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        });

        _logger?.LogInformation(
            "Unloaded session context {ContextId}. IsAlive: {IsAlive}",
            ContextIdMasker.Mask(contextId, !_securityConfig.LogContextIds),
            weakRef.IsAlive
        );

        return true;
    }

    /// <summary>
    /// Check if a context exists
    /// </summary>
    public bool ContextExists(string contextId) => _contexts.ContainsKey(contextId);

    /// <summary>
    /// Get count of active contexts
    /// </summary>
    public int GetActiveContextCount() => _contexts.Count;

    /// <summary>
    /// Analyze syntax tree for dangerous API usage (filesystem, network, process, etc.)
    /// </summary>
    private static List<Diagnostic> AnalyzeForDangerousApis(SyntaxTree syntaxTree)
    {
        var diagnostics = new List<Diagnostic>();
        var root = syntaxTree.GetRoot();

        // Banned identifiers (namespace and type names)
        var bannedIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Filesystem APIs
            "File",
            "Directory",
            "FileInfo",
            "DirectoryInfo",
            "DriveInfo",
            "FileStream",
            "StreamWriter",
            "StreamReader",
            // Process APIs
            "Process",
            "ProcessStartInfo",
            // Network APIs
            "HttpClient",
            "WebClient",
            "TcpClient",
            "TcpListener",
            "UdpClient",
            "Socket",
            "HttpWebRequest",
            "HttpWebResponse",
            // Native interop
            "DllImport",
            "Marshal",
            "GCHandle",
            // Environment manipulation
            "Environment.Exit",
            "Environment.FailFast",
        };

        // Walk syntax tree looking for banned identifiers
        var identifiers = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax>();

        foreach (var identifier in identifiers)
        {
            var name = identifier.Identifier.ValueText;
            if (bannedIdentifiers.Contains(name))
            {
                var location = identifier.GetLocation();
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FORBIDDEN_API",
                        "Forbidden API Usage",
                        $"Use of '{name}' is forbidden for security reasons",
                        "Security",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true
                    ),
                    location
                );
                diagnostics.Add(diagnostic);
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Transform REPL/script-style code to console application using Roslyn syntax trees
    /// Handles return statements, expressions, and proper value capture
    /// </summary>
    private static string TransformReplCodeToTopLevelStatements(string code)
    {
        return ReplCodeTransformer.TransformToConsoleApp(code);
    }
}

/// <summary>
/// Represents an isolated session context with its own AssemblyLoadContext
/// </summary>
internal class SessionContext
{
    public UnloadableAssemblyLoadContext LoadContext { get; }

    public SessionContext(UnloadableAssemblyLoadContext loadContext)
    {
        LoadContext = loadContext;
    }
}

/// <summary>
/// Helper class to transform REPL/script-style C# code to console application format
/// Uses Roslyn syntax trees for proper code analysis and transformation
/// </summary>
internal static class ReplCodeTransformer
{
    /// <summary>
    /// Transform REPL-style code to console application top-level statements
    /// Handles return statements and standalone expressions by converting them to Console.WriteLine calls
    /// </summary>
    public static string TransformToConsoleApp(string code)
    {
        // Parse as script to accept REPL syntax, with latest language features
        var scriptTree = CSharpSyntaxTree.ParseText(
            code,
            new CSharpParseOptions(
                kind: SourceCodeKind.Script,
                languageVersion: LanguageVersion.Preview
            )
        );

        var root = scriptTree.GetRoot();

        // Check if code contains await expressions
        var hasAwait = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AwaitExpressionSyntax>()
            .Any();

        // Find all return statements and expression statements
        var returnStatements = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ReturnStatementSyntax>()
            .ToList();

        // Check if the last statement is an expression statement (REPL-style implicit return)
        var statements = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
            .ToList();

        var lastStatement = statements.LastOrDefault();
        bool hasImplicitReturn =
            lastStatement
                is Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax expressionStmt
            && !returnStatements.Any();

        if (returnStatements.Count == 0 && !hasImplicitReturn)
        {
            // No return statements or implicit returns - code is already compatible
            return code;
        }

        // Replace return statements and convert implicit returns to Console.WriteLine
        var rewriter = new ReturnStatementRewriter(
            convertLastExpression: hasImplicitReturn,
            needsAsyncUsing: hasAwait
        );
        var newRoot = rewriter.Visit(root);

        return newRoot.ToFullString();
    }
}

/// <summary>
/// Syntax rewriter that converts return statements and trailing expressions to Console.WriteLine statements
/// Also adds required using directives
/// </summary>
internal class ReturnStatementRewriter : Microsoft.CodeAnalysis.CSharp.CSharpSyntaxRewriter
{
    private readonly bool _convertLastExpression;
    private readonly bool _needsAsyncUsing;
    private bool _hasConverted;

    public ReturnStatementRewriter(bool convertLastExpression = false, bool needsAsyncUsing = false)
    {
        _convertLastExpression = convertLastExpression;
        _needsAsyncUsing = needsAsyncUsing;
        _hasConverted = false;
    }

    public override Microsoft.CodeAnalysis.SyntaxNode? VisitCompilationUnit(
        Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax node
    )
    {
        // First, visit children to do transformations
        var visited = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)
            base.VisitCompilationUnit(node)!;

        // Get existing usings
        var existingUsings = visited.Usings.Select(u => u.Name?.ToString()).ToHashSet();

        var usingsToAdd = new List<Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax>();

        // Add using System; if not present (needed for Console)
        if (!existingUsings.Contains("System"))
        {
            var usingDirective = Microsoft
                .CodeAnalysis.CSharp.SyntaxFactory.ParseCompilationUnit("using System;")
                .Usings[0];
            usingsToAdd.Add(usingDirective);
        }

        // Add using System.Threading.Tasks; if async and not present
        if (_needsAsyncUsing && !existingUsings.Contains("System.Threading.Tasks"))
        {
            var usingDirective = Microsoft
                .CodeAnalysis.CSharp.SyntaxFactory.ParseCompilationUnit(
                    "using System.Threading.Tasks;"
                )
                .Usings[0];
            usingsToAdd.Add(usingDirective);
        }

        // Add new usings to the compilation unit
        if (usingsToAdd.Count > 0)
        {
            visited = visited.AddUsings(usingsToAdd.ToArray());
        }

        return visited;
    }

    public override Microsoft.CodeAnalysis.SyntaxNode? VisitReturnStatement(
        Microsoft.CodeAnalysis.CSharp.Syntax.ReturnStatementSyntax node
    )
    {
        if (node.Expression == null)
        {
            // return; -> no output
            return Microsoft.CodeAnalysis.CSharp.SyntaxFactory.EmptyStatement();
        }

        // return expression; -> Console.WriteLine(expression);
        var invocation = CreateConsoleWriteLine(node.Expression);
        return Microsoft
            .CodeAnalysis.CSharp.SyntaxFactory.ExpressionStatement(invocation)
            .WithTriviaFrom(node);
    }

    public override Microsoft.CodeAnalysis.SyntaxNode? VisitExpressionStatement(
        Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax node
    )
    {
        // If we should convert the last expression and haven't converted yet
        // and this is a value-returning expression, wrap it in Console.WriteLine
        if (_convertLastExpression && !_hasConverted)
        {
            // Check if this is the last statement in the compilation unit
            var parent = node.Parent;
            if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.GlobalStatementSyntax globalStmt)
            {
                var compilationUnit =
                    globalStmt.Parent as Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax;
                if (compilationUnit != null)
                {
                    var allGlobalStatements = compilationUnit
                        .Members.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.GlobalStatementSyntax>()
                        .ToList();

                    if (globalStmt == allGlobalStatements.LastOrDefault())
                    {
                        // This is the last statement - wrap in Console.WriteLine
                        _hasConverted = true;
                        var invocation = CreateConsoleWriteLine(node.Expression);
                        return Microsoft
                            .CodeAnalysis.CSharp.SyntaxFactory.ExpressionStatement(invocation)
                            .WithTriviaFrom(node);
                    }
                }
            }
        }

        return base.VisitExpressionStatement(node);
    }

    private static Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax CreateConsoleWriteLine(
        Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax expression
    )
    {
        return Microsoft.CodeAnalysis.CSharp.SyntaxFactory.InvocationExpression(
            Microsoft.CodeAnalysis.CSharp.SyntaxFactory.MemberAccessExpression(
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxFactory.IdentifierName("Console"),
                Microsoft.CodeAnalysis.CSharp.SyntaxFactory.IdentifierName("WriteLine")
            ),
            Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ArgumentList(
                Microsoft.CodeAnalysis.CSharp.SyntaxFactory.SingletonSeparatedList(
                    Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Argument(expression)
                )
            )
        );
    }
}
