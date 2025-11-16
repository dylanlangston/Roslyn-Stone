# Copilot Agents

This directory contains specialized Copilot agents that provide expert assistance for specific tasks in the Roslyn-Stone repository. Each agent has deep domain expertise and can be invoked when working on related functionality.

## Available Agents

### 1. C# Expert (`CSharpExpert.agent.md`)
**General .NET development expertise**

Use this agent for:
- General C# and .NET development tasks
- Code design and architecture decisions
- SOLID principles and design patterns
- Async/await patterns and best practices
- Dependency injection
- Performance optimization
- General testing guidance

**Lines of code**: 192

### 2. Roslyn Expert (`RoslynExpert.agent.md`)
**Microsoft Roslyn compiler platform and scripting APIs**

Use this agent for:
- Roslyn scripting API implementation
- Script execution and state management
- Syntax tree and semantic model navigation
- Dynamic compilation and code analysis
- AssemblyLoadContext and memory management
- REPL implementation patterns
- Compilation diagnostics and error handling

**Lines of code**: 317

### 3. MCP Integration Expert (`McpIntegrationExpert.agent.md`)
**Model Context Protocol tools, prompts, and integration**

Use this agent for:
- Designing MCP tools and prompts
- LLM-friendly API design
- Structured response patterns
- Tool discoverability and documentation
- MCP server configuration
- Sampling integration
- Error handling in MCP context

**Lines of code**: 452

### 4. Testing Expert (`TestingExpert.agent.md`)
**xUnit testing patterns and test design**

Use this agent for:
- Writing xUnit tests (Facts, Theories)
- Test design patterns (AAA pattern)
- Async testing and cancellation
- Test fixtures and lifecycle management
- Mocking and test doubles
- Integration testing
- Test coverage strategies
- Test organization and naming

**Lines of code**: 647

### 5. Documentation Expert (`DocumentationExpert.agent.md`)
**Technical documentation and XML comments**

Use this agent for:
- XML documentation comments
- README file structure and content
- API documentation
- LLM-friendly documentation patterns
- Examples and usage guides
- Migration guides
- Documentation maintenance

**Lines of code**: 617

### 6. Security & Validation Expert (`SecurityExpert.agent.md`)
**Security, validation, and secure code execution**

Use this agent for:
- Input validation and sanitization
- Security best practices for code execution
- Code sandboxing and isolation
- Rate limiting and resource management
- Secure assembly loading
- Authentication and authorization
- Security event logging
- Secure defaults and configuration

**Lines of code**: 752

## How to Use Agents

### In GitHub Copilot
Agents can be invoked in conversations by mentioning their expertise area. GitHub Copilot will automatically select the most appropriate agent based on your task.

### Agent Selection Guidelines

**Choose C# Expert when:**
- Working on general .NET development tasks
- Need guidance on C# language features
- Implementing design patterns
- General code architecture questions

**Choose Roslyn Expert when:**
- Implementing REPL functionality
- Working with Roslyn scripting APIs
- Managing AssemblyLoadContext
- Analyzing or generating C# code dynamically
- Handling compilation diagnostics

**Choose MCP Integration Expert when:**
- Creating or modifying MCP tools
- Designing tool parameters and responses
- Implementing prompts
- Ensuring LLM-friendly API design
- Working with MCP server configuration

**Choose Testing Expert when:**
- Writing new tests
- Refactoring existing tests
- Setting up test fixtures
- Testing async operations
- Improving test coverage
- Organizing test suites

**Choose Documentation Expert when:**
- Adding XML documentation comments
- Updating README files
- Creating API documentation
- Writing usage examples
- Documenting breaking changes
- Ensuring LLM-friendly documentation

**Choose Security Expert when:**
- Implementing input validation
- Adding security features
- Reviewing code for security issues
- Implementing rate limiting
- Working with secure assembly loading
- Configuring security settings
- Handling authentication/authorization

## Agent Design Principles

All agents follow these principles:

1. **Specialized Expertise**: Each agent has a focused domain of expertise
2. **Non-Overlapping**: Agents complement each other without duplication
3. **Comprehensive**: Agents provide in-depth guidance with examples
4. **Actionable**: All guidance includes practical code examples
5. **Best Practices**: Agents encode industry best practices and patterns
6. **Repository-Specific**: Agents are tailored to the Roslyn-Stone codebase

## Agent Format

Each agent file follows this structure:

```markdown
---
name: Agent Name
description: Brief description of agent expertise
# version: YYYY-MM-DDa
---

Introduction and expertise overview

When invoked:
- Bullet points describing agent behavior

# Major Section
## Subsection
Content with code examples
```

## Updating Agents

When updating agents:

1. Increment the version date in the frontmatter
2. Maintain the existing structure and formatting
3. Add new examples and patterns as they emerge
4. Keep content relevant to the Roslyn-Stone repository
5. Ensure no overlap with other agents' expertise

## Contributing

When adding new agents:

1. Identify a clear, focused domain of expertise
2. Ensure no overlap with existing agents
3. Follow the standard agent format
4. Include comprehensive examples and patterns
5. Add agent to this README with usage guidelines
6. Update version to current date

## Version History

- **2025-11-16**: Initial set of specialized agents created
  - RoslynExpert.agent.md
  - McpIntegrationExpert.agent.md
  - TestingExpert.agent.md
  - DocumentationExpert.agent.md
  - SecurityExpert.agent.md
- **2025-10-27**: C# Expert agent created
