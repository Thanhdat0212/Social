using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class DotNetPostSeeder
{
    private static readonly Guid ProgrammingInterestId = Guid.Parse("00000000-0000-0000-0000-000000000102");
    private static readonly Guid TechInterestId = Guid.Parse("00000000-0000-0000-0000-000000000101");

    private static readonly string[] ImagePool =
    {
        "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1607799279861-4dd421887fb3?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1542838132-92c53300491e?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1518770660439-4636190af475?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1519389950473-47ba0277781c?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1581291518857-4e27b48ff24e?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?w=1000&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1504639725590-34d0984388bd?w=1000&auto=format&fit=crop&q=80"
    };

    private record TopicInfo(string Title, string Category, string Question, string Analysis, string Code, string InterviewTip, string Hashtags);

    private static readonly TopicInfo[] Topics =
    {
        new(
            "Phân biệt Value Type và Reference Type trong C#",
            "C# Nền tảng",
            "Value Type và Reference Type được lưu trữ ở đâu trong bộ nhớ? Có phải Value Type luôn luôn nằm trên Stack?",
            "Value Type (struct, enum, int) lưu trực tiếp dữ liệu. Reference Type (class, interface, string) lưu con trỏ trỏ tới Heap.\nCạm bẫy: Value Type không phải luôn nằm trên Stack. Nếu là field của class hoặc bị Boxing, Value Type sẽ nằm trên Heap cùng object cha.",
            "public class Order {\n    public int OrderId; // Nằm trên Heap vì là field của class!\n}\nvoid Method() {\n    int count = 10; // Nằm trên Stack vì là local variable\n}",
            "Nhấn mạnh: Vị trí bộ nhớ của Value Type phụ thuộc vào ngữ cảnh khai báo.",
            "#CSharp #DotNet #MemoryManagement #StackVsHeap #InterviewPrep"
        ),
        new(
            "Boxing và Unboxing trong C#: Cái giá của Hiệu năng và GC Pressure",
            "C# Nền tảng",
            "Boxing và Unboxing là gì? Tác động tiêu cực của nó tới Garbage Collector trong hệ thống chịu tải cao?",
            "Boxing copy giá trị từ Stack lên Heap object. Unboxing trích xuất con trỏ từ Heap về Stack.\nChi phí: Cấp phát rác trên Gen 0, kích hoạt GC liên tục gây độ trễ micro-pause (Stop-the-World).",
            "int val = 42;\nobject obj = val; // Boxing: cấp phát Heap!\nint res = (int)obj; // Unboxing\n// Tối ưu: Dùng Generic để Zero Boxing\nList<int> list = new List<int>();\nlist.Add(val);",
            "Luôn dùng Generic Collections và Generic Constraints thay vì ép kiểu qua object.",
            "#CSharp #Performance #GarbageCollector #Boxing #DotNet"
        ),
        new(
            "String Immutability và Cơ chế String Interning trong CLR",
            "C# Nền tảng",
            "Tại sao String lại Bất biến (Immutable)? String Interning hoạt động ra sao và khi nào bắt buộc dùng StringBuilder?",
            "String trong .NET là Immutable: Khi đã tạo thì không thể sửa đổi. Nối chuỗi s += 'a' tạo chuỗi mới trên Heap.\nString Interning: CLR duy trì bảng băm chứa string literals để tái sử dụng ô nhớ.",
            "string s1 = \"hello\";\nstring s2 = \"hello\";\nConsole.WriteLine(object.ReferenceEquals(s1, s2)); // True (Interning)\n\nvar sb = new StringBuilder(1000);\nfor(int i=0; i<100; i++) sb.Append(i);\nstring final = sb.ToString();",
            "StringBuilder cấp phát buffer mảng char nội bộ tự mở rộng, tránh tạo rác trên Heap.",
            "#CSharp #String #StringBuilder #Interning #Memory"
        ),
        new(
            "Struct vs Class: Bộ quy tắc vàng khi thiết kế từ Microsoft",
            "C# Nền tảng",
            "Khi nào nên tạo Struct thay vì Class? Tại sao struct lớn hơn 16 bytes lại làm giảm hiệu năng?",
            "Class là Reference type (truyền con trỏ 8 bytes), Struct là Value type (copy toàn bộ giá trị khi truyền qua hàm).\nQuy tắc Microsoft: Chỉ dùng Struct khi kích thước <= 16 bytes, đại diện cho giá trị đơn lẻ (Money, Point, Vector), có tính bất biến (readonly struct).",
            "public readonly struct Coordinate {\n    public double Lat { get; }\n    public double Lng { get; }\n    public Coordinate(double lat, double lng) => (Lat, Lng) = (lat, lng);\n}",
            "Nhắc tới `readonly ref struct` (chỉ nằm trên Stack, không bị boxing như Span<T>) để ghi điểm Senior.",
            "#StructVsClass #DesignGuidelines #Performance #CSharp"
        ),
        new(
            "Record Types trong C# 9+: Value-based Equality và with Expression",
            "C# Nền tảng",
            "Record khác Class thông thường thế nào? Toán tử 'with' hoạt động ra sao?",
            "Record tự động triển khai Value-based Equality: 2 record có cùng giá trị các trường thì bằng nhau (== trả về true).\nNon-destructive mutation: Toán tử `with` tạo bản sao mới với một số trường thay đổi mà giữ nguyên gốc.",
            "public record UserDto(Guid Id, string FullName, string Email);\nvar u1 = new UserDto(Guid.NewGuid(), \"Nam\", \"nam@social.com\");\nvar u2 = u1 with { FullName = \"Nguyen Hoang Nam\" };",
            "Record cực kỳ phù hợp cho DTOs, API Request/Response models, CQRS Commands/Queries.",
            "#Records #CSharp #Immutability #CleanCode #DTO"
        ),
        new(
            "Interface vs Abstract Class: Bản chất và Default Interface Methods",
            "OOP & Design",
            "Phân biệt Interface và Abstract Class? Kể từ C# 8, Default Interface Method có thay thế được Abstract Class không?",
            "Interface định nghĩa Contract (Can-Do), hỗ trợ đa kế thừa hành vi, không chứa instance fields (state).\nAbstract class định nghĩa Identity (Is-A), đơn kế thừa, có thể lưu trữ trạng thái và constructor.",
            "public interface IPaymentService {\n    Task ProcessAsync(decimal amount);\n    void Log(string msg) => Console.WriteLine(msg); // DIM C# 8\n}\npublic abstract class BaseEntity {\n    public Guid Id { get; protected set; } = Guid.NewGuid();\n}",
            "Dùng Interface cho loose coupling, DI và Unit Test. Dùng Abstract class cho chia sẻ mã nguồn cốt lõi.",
            "#OOP #Interface #AbstractClass #DesignPatterns #CSharp"
        ),
        new(
            "Bản chất async/await và Cạm bẫy chết người của async void",
            "Async & Concurrency",
            "State Machine của async/await hoạt động thế nào? Tại sao async void lại làm crash toàn bộ server?",
            "Compiler dịch method async thành struct IAsyncStateMachine. Luồng thread được trả về ThreadPool khi gặp I/O.\nAsync void không trả về Task, unhandled exception sẽ bắn thẳng vào SynchronizationContext và CRASH PROCESS!",
            "// ❌ NGUY HIỂM: Crash ứng dụng\npublic async void BadFireAndForget() {\n    await Task.Delay(100);\n    throw new Exception(\"Crash server!\");\n}\n// ✅ ĐÚNG: Luôn trả về Task\npublic async Task SafeAsync() {\n    await Task.Delay(100);\n}",
            "Quy tắc: Luôn trả về Task hoặc Task<T>. Ngoại lệ duy nhất cho async void là UI Event Handlers.",
            "#AsyncAwait #Threading #Crash #BestPractices #DotNet"
        ),
        new(
            "Deadlock trong async/await: Cạm bẫy .Result và ThreadPool Starvation",
            "Async & Concurrency",
            "Tại sao gọi task.Result hoặc task.Wait() lại gây Deadlock và ThreadPool Starvation?",
            "Khi gọi .Result, thread hiện tại bị block cứng. Khi task con xong, nó cần quay lại SynchronizationContext đang bị block -> Deadlock!\nTrên ASP.NET Core, gọi .Result gây ThreadPool Starvation: server cạn thread pool khi có lượng tải lớn.",
            "// ❌ SAI: Gây ThreadPool Starvation\npublic IActionResult GetData() {\n    return Ok(FetchAsync().Result);\n}\n// ✅ ĐÚNG: Async all the way\npublic async Task<IActionResult> GetDataAsync() {\n    return Ok(await FetchAsync());\n}",
            "Trong class library, luôn dùng ConfigureAwait(false) để không cần chuyển ngữ cảnh và tăng hiệu năng.",
            "#Deadlock #ThreadPool #Async #HighLoad #DotNet"
        ),
        new(
            "Task vs ValueTask: Bí quyết Zero-Allocation cho Hot Path",
            "Async & Concurrency",
            "ValueTask ra đời nhằm giải quyết vấn đề gì? Khi nào nên trả về ValueTask thay vì Task?",
            "Task là Class (Reference Type) nên mỗi lần trả về đều cấp phát Heap.\nValueTask là Struct (Value Type). Nếu kết quả có thể hoàn thành ĐỒNG BỘ (lấy từ Cache), ValueTask KHÔNG CẤP PHÁT BỘ NHỚ TRÊN HEAP (0 bytes).",
            "public ValueTask<int> GetCachedPointsAsync(Guid id) {\n    if (_cache.TryGetValue(id, out int val)) return new ValueTask<int>(val);\n    return new ValueTask<int>(FetchDbAsync(id));\n}",
            "Chỉ dùng ValueTask cho hot-path có xác suất hoàn thành đồng bộ cao (>90%).",
            "#ValueTask #Performance #ZeroAllocation #CSharp #Optimization"
        ),
        new(
            "Garbage Collector: Cơ chế Gen 0, Gen 1, Gen 2 và Large Object Heap",
            "Memory & CLR",
            "Trình bày thuật toán dọn rác (GC) của CLR? LOH là gì và tại sao LOH lại dễ bị phân mảnh bộ nhớ?",
            "GC chia Heap thành 3 thế hệ: Gen 0 (đối tượng mới), Gen 1 (vùng đệm), Gen 2 (đối tượng sống lâu, Full GC tốn CPU).\nLOH (Large Object Heap): Chứa các đối tượng >= 85,000 bytes. Mặc định không được nén, dễ phân mảnh gây OutOfMemoryException.",
            "// Tránh cấp phát LOH bằng ArrayPool\nusing var rented = System.Buffers.MemoryPool<byte>.Shared.Rent(90000);\nMemory<byte> memory = rented.Memory;",
            "Nhắc tới Server GC vs Workstation GC và Pinned Object Heap (POH trong .NET 5+).",
            "#GarbageCollector #LOH #MemoryLeak #CLR #Performance"
        ),
        new(
            "Span<T> và ReadOnlySpan<T>: Kỹ thuật cắt mảng và chuỗi không tốn RAM",
            "High Performance",
            "Span<T> là gì? Tại sao Span là ref struct và không thể đặt lên Heap?",
            "Span<T> là ref struct đại diện cho khối bộ nhớ liên tục trên Stack, Heap hoặc Native memory.\nKhi cắt chuỗi bằng ReadOnlySpan, không có byte nào được copy hay cấp phát Heap (Zero-allocation).",
            "string header = \"Bearer eyJhbGci...\";\nReadOnlySpan<char> span = header.AsSpan();\nReadOnlySpan<char> token = span.Slice(7); // 0 bytes allocated!",
            "Span<T> là nền tảng đưa ASP.NET Core lên top đầu các web framework nhanh nhất thế giới.",
            "#Span #ZeroAllocation #CSharpCore #HighPerformance #TechEmpower"
        ),
        new(
            "IEnumerable vs ICollection vs IList vs IQueryable: Khác nhau cốt lõi",
            "Collections & LINQ",
            "Phân biệt IEnumerable, ICollection, IList và IQueryable? Khi nào câu lệnh SQL thực sự chạy?",
            "IEnumerable: Duyệt 1 chiều, deferred execution.\nICollection: Thêm Count, Add, Remove.\nIList: Truy xuất theo chỉ số [i], Insert, RemoveAt.\nIQueryable: Dùng Expression Trees dịch thành câu lệnh SQL chạy trên database server khi gọi ToListAsync().",
            "// ✅ Dịch thành SQL SELECT TOP 10 ... WHERE IsActive = 1\nIQueryable<User> q = db.Users;\nvar res = await q.Where(u => u.IsActive).Take(10).ToListAsync();",
            "Luôn giữ IQueryable càng lâu càng tốt trong tầng Repository, chỉ ToListAsync ở bước cuối.",
            "#LINQ #IQueryable #Collections #EFCore #Database"
        ),
        new(
            "Vòng đời Service trong DI: Transient, Scoped, Singleton và Lỗi Captive Dependency",
            "ASP.NET Core",
            "Phân biệt Transient, Scoped, Singleton? Hiện tượng Captive Dependency là gì và cách phòng tránh?",
            "Transient: Tạo mới mỗi khi gọi. Scoped: 1 instance cho mỗi HTTP request. Singleton: 1 instance cho toàn ứng dụng.\nCaptive Dependency: Inject Scoped vào Singleton khiến Scoped bị bắt cóc thành Singleton, gây lỗi DbContext concurrency.",
            "public class SingletonWorker {\n    private readonly IServiceScopeFactory _scopeFactory;\n    public SingletonWorker(IServiceScopeFactory sf) => _scopeFactory = sf;\n    public async Task Run() {\n        using var scope = _scopeFactory.CreateScope();\n        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();\n    }\n}",
            "ASP.NET Core có ValidateScopes = true trong Development để bắt lỗi này khi khởi động.",
            "#DependencyInjection #ASPNETCore #CaptiveDependency #Architecture"
        ),
        new(
            "Thứ tự Middleware Pipeline trong ASP.NET Core quan trọng thế nào?",
            "ASP.NET Core",
            "Trình bày thứ tự chuẩn của Middleware Pipeline? Tại sao xếp sai vị trí lại gây lỗi bảo mật?",
            "Middleware hoạt động theo mô hình búp bê Nga hai chiều.\nThứ tự chuẩn: ExceptionHandler -> Hsts -> HttpsRedirection -> StaticFiles -> Routing -> CORS -> Authentication -> Authorization -> Endpoints.\nĐặt UseAuthorization trước UseAuthentication sẽ khiến [Authorize] luôn từ chối.",
            "app.UseExceptionHandler(\"/error\");\napp.UseRouting();\napp.UseCors(\"Policy\");\napp.UseAuthentication(); // 1. Bạn là ai?\napp.UseAuthorization();  // 2. Bạn được làm gì?\napp.MapControllers();",
            "Nguyên tắc: Authentication phải luôn đứng TRƯỚC Authorization trong HTTP pipeline.",
            "#Middleware #Pipeline #Security #ASPNETCore #WebAPI"
        ),
        new(
            "Vấn nạn N+1 Query Problem trong Entity Framework Core: 3 Cách Giải Quyết",
            "Entity Framework Core",
            "N+1 Query là gì? Cho ví dụ code gây lỗi và 3 giải pháp khắc phục trong thực tế?",
            "Xảy ra khi query 1 danh sách cha (N phần tử) và duyệt vòng lặp query tiếp các phần tử con -> Bắn N + 1 câu SQL.\n3 Cách khắc phục: 1. Eager Loading (.Include). 2. DTO Projection (.Select sang DTO - Tối ưu nhất). 3. Split Query (.AsSplitQuery).",
            "// ✅ DTO Projection tối ưu 1 query duy nhất:\nvar dtos = await db.Blogs.Select(b => new {\n    b.Title,\n    CommentCount = b.Comments.Count()\n}).ToListAsync();",
            "DTO Projection qua .Select() là cách tốt nhất vì chỉ lấy đúng các cột cần thiết từ database.",
            "#EFCore #NPlus1Query #SQLOptimization #Performance #Database"
        ),
        new(
            "Clean Architecture trong .NET: Tại sao Domain Layer phải độc lập hoàn toàn?",
            "Kiến trúc hệ thống",
            "Dependency Rule trong Clean Architecture là gì? Tại sao Domain Layer không được phép phụ thuộc bất kỳ package nào?",
            "Mọi phụ thuộc chỉ được phép hướng từ ngoài vào trong: API -> Infrastructure -> Application -> Domain.\nDomain Layer chứa nghiệp vụ cốt lõi, độc lập tuyệt đối với cơ sở dữ liệu, framework và thư viện bên thứ 3.",
            "namespace Domain.Entities;\npublic class BankAccount {\n    public decimal Balance { get; private set; }\n    public void Withdraw(decimal amount) {\n        if (amount > Balance) throw new DomainException(\"Số dư không đủ!\");\n        Balance -= amount;\n    }\n}",
            "Phân biệt Anemic Domain Model (chỉ có getter/setter) với Rich Domain Model (nghiệp vụ đóng gói chặt chẽ trong Entity).",
            "#CleanArchitecture #DomainDrivenDesign #DDD #SoftwareEngineering"
        ),
        new(
            "Outbox Pattern: Giải quyết bài toán Dual-Write trong Microservices",
            "Kiến trúc hệ thống",
            "Dual-write problem là gì? Tại sao không nên publish event lên RabbitMQ ngay trong DbContext transaction?",
            "Dual-write problem: Lưu DB thành công nhưng gửi message thất bại dẫn đến dữ liệu không nhất quán.\nOutbox Pattern: Lưu Entity và Event vào bảng Outbox trong CÙNG 1 DATABASE TRANSACTION. Background Worker riêng đọc bảng Outbox và publish lên Message Broker.",
            "using var tx = await _db.Database.BeginTransactionAsync();\n_db.Orders.Add(order);\n_db.OutboxMessages.Add(new OutboxMessage {\n    Id = Guid.NewGuid(),\n    Type = \"OrderCreated\",\n    Payload = JsonSerializer.Serialize(new OrderCreatedEvent(order.Id))\n});\nawait _db.SaveChangesAsync();\nawait tx.CommitAsync();",
            "Đề cập đến MassTransit Outbox để chứng minh kinh nghiệm thực tế.",
            "#OutboxPattern #Microservices #RabbitMQ #EventDriven #Architecture"
        ),
        new(
            "Cache Stampede (Dogpiling): Thảm họa sập server khi Cache Key hết hạn",
            "Kiến trúc hệ thống",
            "Cache Stampede là gì? Làm sao để xử lý khi 50,000 req/s cùng ập vào database khi cache hết hạn?",
            "Khi cache key của một sản phẩm hot vừa hết hạn, hàng ngàn request đồng thời thấy Cache MISS và cùng ùa vào database query, làm database 100% CPU và sập server.\nGiải pháp: Distributed Lock (Mutex), Probabilistic Early Expiration (XFetch), hoặc HybridCache trong .NET 9.",
            "await _semaphore.WaitAsync();\ntry {\n    var cached = await _cache.GetStringAsync(key);\n    if (cached != null) return cached;\n    var data = await _db.GetDataAsync();\n    await _cache.SetStringAsync(key, data, TimeSpan.FromMinutes(10));\n    return data;\n} finally { _semaphore.Release(); }",
            "Trong .NET 9, Microsoft đã giới thiệu HybridCache tự động giải quyết Cache Stampede.",
            "#Redis #CacheStampede #SystemDesign #HighLoad #ASPNETCore"
        ),
        new(
            "Idempotent API: Thiết kế API thanh toán an toàn chống trừ tiền 2 lần",
            "Kiến trúc hệ thống",
            "Idempotency trong REST API là gì? Thiết kế Idempotency Key cho API thanh toán như thế nào?",
            "Một API là Idempotent nếu gọi 1 lần hay nhiều lần với cùng dữ liệu đều tạo ra kết quả và trạng thái giống nhau.\nGiải pháp: Client gửi header Idempotency-Key (UUID). Server lưu key vào Redis với trạng thái Processing. Nếu nhận request trùng lặp, server trả về kết quả cũ mà không trừ tiền lại.",
            "var key = Request.Headers[\"Idempotency-Key\"].ToString();\nvar cached = await _redis.GetAsync(key);\nif (cached != null) return Ok(cached); // Đã xử lý, trả kết quả cũ!\n\nawait _redis.SetAsync(key, \"Processing\", TimeSpan.FromMinutes(5));\nvar result = await ProcessPaymentAsync();\nawait _redis.SetAsync(key, result, TimeSpan.FromHours(24));\nreturn Ok(result);",
            "Đây là câu hỏi tiêu chuẩn cho các vị trí Middle/Senior Backend tại các công ty FinTech và E-Commerce.",
            "#Idempotency #FinTech #PaymentGateway #API #SystemDesign"
        ),
        new(
            "CQRS và MediatR: Tách biệt luồng Đọc và Ghi trong .NET",
            "Kiến trúc hệ thống",
            "CQRS (Command Query Responsibility Segregation) là gì? Lợi ích khi kết hợp MediatR Pipeline Behaviors?",
            "CQRS tách biệt hành vi thay đổi dữ liệu (Command) và hành vi truy vấn đọc dữ liệu (Query).\nCommands tập trung vào logic nghiệp vụ và bảo vệ tính toàn vẹn.\nQueries tối ưu hóa tốc độ đọc (Dapper, AsNoTracking, Read Replicas).\nMediatR Pipeline Behaviors hoạt động như Middleware cho Application layer, xử lý Validation, Logging, Caching.",
            "public record CreateOrderCommand(Guid CustomerId, decimal Total) : IRequest<Guid>;\npublic class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid> {\n    public async Task<Guid> Handle(CreateOrderCommand cmd, CancellationToken ct) {\n        return Guid.NewGuid();\n    }\n}",
            "CQRS không bắt buộc phải có 2 database riêng biệt. Tách biệt 2 luồng code trên 1 database đã mang lại giá trị rất lớn.",
            "#CQRS #MediatR #CleanArchitecture #DesignPatterns #DotNet"
        )
    };

    private static readonly (string Prefix, string Intro)[] Formats =
    {
        ("🎯 [Góc Phỏng Vấn .NET Backend]", "Một câu hỏi kỹ thuật rất hay xuất hiện trong các buổi phỏng vấn tại các công ty công nghệ lớn:"),
        ("🔥 [Deep Dive C# & CLR Internals]", "Bản chất kiến trúc bên dưới hệ sinh thái .NET mà mọi lập trình viên cần nắm vững để bứt phá trình độ:"),
        ("⚠️ [Bài Học Thực Chiến Production]", "Chia sẻ lại một sự cố kinh điển (Incident Post-mortem) mà team chúng tôi từng giải quyết trên hệ thống chịu tải cao:"),
        ("💡 [ASP.NET Core Best Practices]", "Tổng hợp các quy tắc vàng và kinh nghiệm thiết kế ứng dụng backend chuẩn mực theo chuẩn quốc tế:"),
        ("🧩 [C# Code Challenge & Brainteaser]", "Hãy thử thách bản thân với một tình huống code thực tế. Đố bạn đoạn code sau đây sẽ hoạt động ra sao và giải thích lý do:"),
        ("🏛️ [Tình Huống Phỏng Vấn System Design]", "Bài toán thiết kế hệ thống backend chịu tải lớn thường gặp tại các kỳ lân công nghệ:"),
        ("⚡ [Tối Ưu Hiệu Năng & Bộ Nhớ .NET]", "Bí quyết giúp giảm thiểu mức sử dụng RAM và tối đa hóa thông lượng xử lý request cho hệ thống backend:"),
        ("💬 [Góc Chia Sẻ Tech Lead]", "Những quan sát và kinh nghiệm chân thành sau nhiều năm phỏng vấn và đánh giá ứng viên vị trí Backend C#:")
    };

    private static readonly string[] Perspectives =
    {
        "Dưới góc nhìn của một Senior Engineer:",
        "Kinh nghiệm thực chiến khi xử lý hệ thống 10,000 req/s:",
        "Điểm khác biệt cốt lõi giữa Junior và Senior khi tiếp cận bài toán này:",
        "Trong quá trình refactor một monolith lớn sang microservices:",
        "Khi đối mặt với bài kiểm tra Benchmark hiệu năng thực tế:",
        "Bài học rút ra từ một đợt review mã nguồn (Code Review) khắt khe:"
    };

    public static async Task<int> SeedDotNetPostsAsync(
        SocialDbContext context,
        ILogger logger,
        int countToGenerate = 1000,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bắt đầu sinh và nạp {Count} bài viết chuyên sâu về .NET, C# và Backend...", countToGenerate);

        var authorIds = await context.Users.Select(u => u.Id).ToListAsync(cancellationToken);
        if (authorIds.Count == 0)
        {
            logger.LogWarning("Chưa có User nào trong database! Vui lòng nạp User trước.");
            return 0;
        }

        var random = new Random(2026);
        var now = DateTime.UtcNow;

        var batchSize = 100;
        var totalInserted = 0;
        var postList = new List<Post>();
        var interestList = new List<PostInterest>();

        for (int i = 0; i < countToGenerate; i++)
        {
            var postId = Guid.NewGuid();
            var authorId = authorIds[random.Next(authorIds.Count)];
            var topic = Topics[i % Topics.Length];
            var (prefix, intro) = Formats[random.Next(Formats.Length)];
            var perspective = Perspectives[random.Next(Perspectives.Length)];

            var content = $"{prefix} #{i + 1}: {topic.Title}\n\n" +
                          $"{intro}\n{perspective}\n\n" +
                          $"❓ {topic.Question}\n\n" +
                          $"🔍 PHÂN TÍCH KỸ THUẬT CHUYÊN SÂU:\n{topic.Analysis}\n\n" +
                          $"💻 MÃ NGUỒN MINH HỌA (C#):\n```csharp\n{topic.Code}\n```\n\n" +
                          $"💡 BÍ QUYẾT KHI ĐI PHỎNG VẤN:\n{topic.InterviewTip}\n\n" +
                          $"{topic.Hashtags} #BaiViet{i + 1}";

            if (content.Length > 4800)
            {
                content = content.Substring(0, 4800) + "...";
            }

            var mediaUrls = new List<string>();
            if (random.NextDouble() < 0.65)
            {
                mediaUrls.Add(ImagePool[random.Next(ImagePool.Length)]);
                if (random.NextDouble() < 0.20)
                {
                    mediaUrls.Add(ImagePool[random.Next(ImagePool.Length)]);
                }
            }

            var minutesAgo = random.Next(5, 60 * 24 * 60);
            var createdAt = now.AddMinutes(-minutesAgo);

            var likes = random.Next(15, 520);
            var comments = random.Next(1, 45);
            var views = random.Next(likes * 3, likes * 10 + 120);

            var post = new Post
            {
                Id = postId,
                AuthorId = authorId,
                Content = content,
                MediaUrls = mediaUrls,
                Status = PostStatus.Published,
                LikeCount = likes,
                CommentCount = comments,
                ViewCount = views,
                CreatedAtUtc = createdAt,
                UpdatedAtUtc = createdAt
            };
            postList.Add(post);

            interestList.Add(new PostInterest
            {
                PostId = postId,
                InterestId = ProgrammingInterestId,
                Confidence = Math.Round(0.95 + random.NextDouble() * 0.04, 2),
                CreatedAtUtc = createdAt
            });

            interestList.Add(new PostInterest
            {
                PostId = postId,
                InterestId = TechInterestId,
                Confidence = Math.Round(0.88 + random.NextDouble() * 0.08, 2),
                CreatedAtUtc = createdAt
            });

            if (postList.Count >= batchSize || i == countToGenerate - 1)
            {
                context.Posts.AddRange(postList);
                context.PostInterests.AddRange(interestList);
                await context.SaveChangesAsync(cancellationToken);

                totalInserted += postList.Count;
                logger.LogInformation("Đã lưu batch: {Current}/{Total} bài viết .NET/C#...", totalInserted, countToGenerate);

                postList.Clear();
                interestList.Clear();
            }
        }

        logger.LogInformation("Hoàn tất nạp thành công {Count} bài viết .NET/C#/Backend vào database!", totalInserted);
        return totalInserted;
    }
}
