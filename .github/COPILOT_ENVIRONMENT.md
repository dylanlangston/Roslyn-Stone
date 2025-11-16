# Custom Copilot Environment

This repository uses a custom environment for GitHub Copilot coding agent to ensure all necessary development tools are available when working on C# code.

## Overview

The custom environment is configured through the `.github/workflows/copilot-setup-steps.yml` workflow, which automatically runs before Copilot starts working on the repository.

## Installed Tools

### .NET 10 SDK
- **Version**: 10.0.x (latest)
- **Purpose**: Provides the latest C# features and runtime
- **Usage**: All C# compilation and runtime operations

### CSharpier
- **Purpose**: Opinionated code formatter for C#
- **Usage**: Format C# code consistently across the project
- **Command**: `dotnet csharpier <file-or-directory>`
- **Documentation**: https://csharpier.com/

### ReSharper Command Line Tools
- **Purpose**: Code analysis, inspection, and refactoring
- **Usage**: Run code inspections and analysis
- **Command**: `jb <command>`
- **Documentation**: https://www.jetbrains.com/help/resharper/ReSharper_Command_Line_Tools.html

### Cake Build Tool
- **Purpose**: Cross-platform build automation
- **Usage**: Execute build scripts written in C#
- **Command**: `dotnet cake <script.cake>`
- **Documentation**: https://cakebuild.net/

### dotnet-outdated
- **Purpose**: Check for outdated NuGet package dependencies
- **Usage**: Identify packages that can be updated
- **Command**: `dotnet-outdated` or `dotnet-outdated -u` to upgrade
- **Documentation**: https://github.com/dotnet-outdated/dotnet-outdated

## How It Works

1. **Automatic Triggers**: The workflow runs automatically when:
   - The workflow file itself is modified (push or pull request)
   - Manually triggered via `workflow_dispatch`

2. **Setup Process**:
   - Checkout the repository
   - Install .NET 10 SDK
   - Install all development tools globally
   - Cache NuGet packages for faster subsequent runs
   - Verify all tools are properly installed

3. **Copilot Integration**: 
   - Copilot uses this prepared environment when executing code or running builds
   - All tools are available in the PATH for immediate use
   - NuGet packages are cached to speed up dependency resolution

## Customization

To add more tools to the environment:

1. Edit `.github/workflows/copilot-setup-steps.yml`
2. Add installation steps in the `steps` section
3. Use `dotnet tool install --global <tool-name>` for .NET tools
4. Use standard `apt-get` or other package managers for system tools
5. Commit and push the changes - the workflow will run automatically

Example of adding a new .NET tool:
```yaml
- name: Install <ToolName>
  run: dotnet tool install --global <package-name>
```

## Benefits

- **Consistency**: Same tools available every time Copilot works on the code
- **Performance**: Cached dependencies reduce setup time
- **Reliability**: Known-good versions of tools eliminate version mismatch issues
- **Productivity**: Copilot can use advanced tools without manual setup

## Troubleshooting

If tools are not available in Copilot sessions:

1. Check the workflow run logs in GitHub Actions
2. Verify the workflow completed successfully
3. Ensure the tool installation steps didn't fail
4. Check that the workflow is enabled in the repository settings

## References

- [GitHub Copilot Custom Environment Documentation](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-environment)
- [.NET SDK Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [GitHub Actions Workflow Syntax](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions)
