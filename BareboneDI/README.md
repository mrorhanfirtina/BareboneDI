
# BareboneDI – Lightweight Dependency Injection for .NET Framework

**Package:** `BareboneDI`  
**Target Framework:** .NET Framework 4.7.2+  
**Author:** Orhan Fırtına  
**Version:** 1.0.0

---

## 📌 What is BareboneDI?

BareboneDI is a lightweight, extensible Dependency Injection container designed to bring modern IoC container capabilities to legacy and enterprise .NET Framework projects. It provides powerful features similar to Autofac or SimpleInjector but is implemented from scratch with full source code transparency and no external dependencies.

---

## ✅ Key Features

- ✅ Open Generic Registrations  
- ✅ Keyed / Named Registrations  
- ✅ Property Injection (`[Inject]` attribute)  
- ✅ Lifetime Management: Transient, Singleton, Scoped  
- ✅ Lifetime Scopes (Nested containers)  
- ✅ Auto Registration (Assembly Scanning)  
- ✅ Module Support  
- ✅ Factory / Delegate Registration  
- ✅ Parameter Overrides  
- ✅ IEnumerable<T> Collection Resolution  
- ✅ Interception Stub (RealProxy / DispatchProxy Ready)

---

## 🚀 Getting Started

### Step 1: Install the NuGet Package

Add BareboneDI to your project:

```
PM> Install-Package BareboneDI
```

Or reference the `.nupkg` locally if you're consuming it internally.

### Step 2: Configure Your DI Container

```csharp
public static class DIConfig
{
    public static DependencyContainer Container { get; private set; }

    public static void RegisterServices()
    {
        Container = new DependencyContainer();

        // Regular Registration
        Container.Register<ILogger, ConsoleLogger>(Lifetime.Singleton);

        // Open Generic Registration
        Container.Register<IRepository<>, Repository<>>(Lifetime.Transient);

        // Keyed Registration
        Container.Register<IPaymentService, CreditCardPayment>(Lifetime.Transient, "Credit");
        Container.Register<IPaymentService, PayPalPayment>(Lifetime.Transient, "PayPal");

        // Factory Registration
        Container.Register<ISettings>(c => new Settings("prod"), Lifetime.Singleton);

        // Auto Registration (e.g., all *Service classes)
        Container.RegisterAssemblyTypes(Assembly.GetExecutingAssembly(), t => t.Name.EndsWith("Service"));

        // Module Registration
        Container.RegisterModule(new CoreModule());
    }
}
```

### Step 3: Hook into Web API

```csharp
GlobalConfiguration.Configuration.DependencyResolver = new BareboneDIResolver(DIConfig.Container);
```

---

## 🧪 Usage Examples

### 1. Constructor Injection

```csharp
public class CustomerController : ApiController
{
    private readonly ILogger _logger;
    private readonly IRepository<Customer> _repository;

    public CustomerController(ILogger logger, IRepository<Customer> repository)
    {
        _logger = logger;
        _repository = repository;
    }
}
```

### 2. Property Injection

```csharp
public class OrderController : ApiController
{
    [Inject]
    public IOrderService OrderService { get; set; }
}
```

### 3. Keyed Resolution

```csharp
var payPal = Container.Resolve<IPaymentService>("PayPal");
```

### 4. Lifetime Scopes

```csharp
using (var scope = Container.BeginLifetimeScope())
{
    var unitOfWork = scope.Resolve<IUnitOfWork>();
}
```

### 5. Parameter Overrides

```csharp
var overrides = new Dictionary<string, object> { { "connectionString", "MyConn" } };
var dbService = Container.Resolve<IDatabaseService>(overrides);
```

### 6. IEnumerable<T> Support

```csharp
public class MultiLogger : ILogger
{
    public MultiLogger(IEnumerable<ILogger> loggers) { ... }
}
```

### 7. Module Definition

```csharp
public class CoreModule : Module
{
    public override void Load(DependencyContainer container)
    {
        container.Register<ILogger, FileLogger>(Lifetime.Singleton);
        container.Register<IUserService, UserService>();
    }
}
```

---

## 🧩 Architecture Overview

- **DependencyContainer**: Core registration and resolution logic
- **LifetimeScope**: Manages per-scope instance cache
- **Registration**: Holds metadata for each service (type, lifetime, factory, singleton cache)
- **InjectAttribute**: Marks a property as injectable
- **Module**: Supports grouped service loading

---

## 🛠️ Custom Interception

The `ApplyInterceptors()` method is a stub. You can extend it with `RealProxy` or `DispatchProxy` to implement:

- Logging
- AOP validations
- Dynamic decorators

---

## 📄 License

MIT License – free for commercial and open-source usage.

---

## 👋 Author

Orhan Fırtına  
Contact: [LinkedIn](https://www.linkedin.com/in/orhan-f%C4%B1rt%C4%B1na/)
