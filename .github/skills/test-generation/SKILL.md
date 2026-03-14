---
name: test-generation
description: 'Generate unit, integration, and component tests from existing code or requirements. Covers Jest/Vitest (TypeScript), xUnit (.NET), and Vue Test Utils. Use when adding test coverage to handlers, services, composables, or components.'
argument-hint: File, handler, or component to generate tests for
---

# Test Generation

## When to Use

- Adding tests for a new handler, service, or component.
- Improving coverage for existing code with missing tests.
- Writing regression tests after a bug fix.
- Generating integration tests for API endpoints.
- Creating component tests for Vue composables or components.

## Procedure

1. **Read the source code** — Understand inputs, outputs, dependencies, and edge cases.
2. **Identify test scenarios** — Happy path, error cases, edge cases, boundary values.
3. **Determine test type** — Unit (isolated), integration (with deps), or component (UI).
4. **Set up mocks** — Mock external dependencies per framework conventions.
5. **Write tests** — One assertion per test case, descriptive names.
6. **Verify** — Run tests to confirm they pass and cover intended behavior.

## Test File Placement

| Project | Test Location |
|---------|--------------|
| Nexus (TypeScript) | Dedicated `__tests__/` or `test/` directory within each library |
| CaddyAdmin.Api (.NET) | Separate `Tests/` project: `CaddyAdmin.Tests/` |
| VaultFacade (.NET) | Separate `Tests/` project: `VaultFacade.Tests/` |
| CaddyAdmin.Web (Vue) | Co-located `__tests__/` or `*.spec.ts` files |
| NexusFrontend (Vue) | Co-located `__tests__/` or `*.spec.ts` files |

**Nexus rule**: Test files are NEVER placed alongside source files. Always in dedicated test directories.

## Jest / Vitest (TypeScript)

### Unit Test for Handler

```typescript
describe('CreateDeviceHandler', () => {
  let handler: CreateDeviceHandler;
  let mockRepository: jest.Mocked<IDeviceRepository>;

  beforeEach(() => {
    mockRepository = {
      save: jest.fn(),
      findById: jest.fn(),
    } as any;
    handler = new CreateDeviceHandler(mockRepository);
  });

  it('should create device with valid command', async () => {
    const command = new CreateDeviceCommand({
      name: 'Living Room Light',
      type: DeviceType.Light,
      protocol: Protocol.Zigbee,
    });

    mockRepository.save.mockResolvedValue(expectedDevice);

    const result = await handler.execute(command);

    expect(result.isSuccess).toBe(true);
    expect(mockRepository.save).toHaveBeenCalledOnce();
  });

  it('should fail when name is empty', async () => {
    const command = new CreateDeviceCommand({
      name: '',
      type: DeviceType.Light,
      protocol: Protocol.Zigbee,
    });

    const result = await handler.execute(command);

    expect(result.isFailure).toBe(true);
  });
});
```

### Composable Test (Vue)

```typescript
import { ref } from 'vue';
import { useDomainFilter } from '../composables/useDomainFilter';

describe('useDomainFilter', () => {
  it('should filter items by search query', () => {
    const items = ref([
      { name: 'Living Room' },
      { name: 'Kitchen' },
      { name: 'Bedroom' },
    ]);

    const { searchQuery, filtered } = useDomainFilter(items);

    searchQuery.value = 'kitchen';

    expect(filtered.value).toHaveLength(1);
    expect(filtered.value[0].name).toBe('Kitchen');
  });

  it('should return all items when query is empty', () => {
    const items = ref([{ name: 'A' }, { name: 'B' }]);
    const { filtered } = useDomainFilter(items);

    expect(filtered.value).toHaveLength(2);
  });
});
```

## xUnit (.NET)

### Unit Test for Handler

```csharp
public class CreateDomainHandlerTests
{
    private readonly Mock<IDomainRepository> _repositoryMock = new();
    private readonly CreateDomainHandler _handler;

    public CreateDomainHandlerTests()
    {
        _handler = new CreateDomainHandler();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesDomain()
    {
        var command = new CreateDomainCommand("example.com", "http://localhost:8080", AccessLevel.Public);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Domain>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, _repositoryMock.Object, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("example.com", result.Name);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Domain>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_EmptyName_ThrowsValidationException(string? name)
    {
        var command = new CreateDomainCommand(name!, "http://localhost", AccessLevel.Public);

        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, _repositoryMock.Object, CancellationToken.None));
    }
}
```

## Test Scenario Checklist

For each function/handler/component, generate tests for:

- [ ] **Happy path** — Valid input produces expected output.
- [ ] **Invalid input** — Empty, null, malformed values are rejected.
- [ ] **Boundary values** — Min/max lengths, zero, negative numbers.
- [ ] **Error handling** — External dependency failures are handled gracefully.
- [ ] **Edge cases** — Concurrent calls, duplicate data, missing optional fields.
- [ ] **State transitions** — If stateful, verify correct state after operations.

## Naming Convention

```
// TypeScript: describe block + "should" statement
describe('HandlerName', () => {
  it('should do expected behavior when given specific input', () => {});
});

// C#: Method_Scenario_ExpectedResult
[Fact]
public async Task Handle_ValidCommand_ReturnsCreatedEntity() {}

[Fact]
public async Task Handle_DuplicateName_ThrowsConflictException() {}
```

## Guardrails

- ✅ Always: One logical assertion per test case.
- ✅ Always: Use descriptive test names that read as documentation.
- ✅ Always: Mock external dependencies; never call real APIs or databases.
- ✅ Always: Place tests in correct location per project convention.
- 🚫 Never: Test implementation details (private methods, internal state).
- 🚫 Never: Create tests that depend on execution order.
- 🚫 Never: Use `any` type in test code; maintain type safety.
