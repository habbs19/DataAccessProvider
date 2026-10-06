# DataAccessProvider.Core

.NET 10 contracts, immutable database commands/results, custom provider SPI, dependency injection, mapping, and file/static clients. Database consumers install one provider package; Core flows transitively. Core-only consumers may install this package directly.

```csharp
using DataAccessProvider.Core;
using DataAccessProvider.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
await using var host = new ServiceCollection().AddDataAccessProviderCore().BuildServiceProvider();
var value = await new StaticValueSource<int>(42).ReadAsync();
Console.WriteLine(value);
```

JsonFileClient offers cancellable text/JSON reads, file length, and writes with explicit ExistingOnly/CreateOrOverwrite/CreateNew modes. Counts are actual encoded bytes. The default is ExistingOnly.

See the repository migration guide for legacy adapters, custom providers, ownership, cancellation and the later 2.0 retirement gate. Version 1.4.0 is a local candidate until release validation/publication.
