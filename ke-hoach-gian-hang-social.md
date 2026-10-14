# Kế hoạch triển khai Gian hàng (Marketplace hàng số) cho dự án Social

> Viết theo góc nhìn của một senior .NET + React, dành cho sinh viên làm project một mình.
> Mục tiêu: **làm từng bước nhỏ, bước nào xong là chạy và kiểm tra được**, không viết một đống code rồi mới chạy thử.

---

## 0. Cách dùng tài liệu này

1. Làm **tuần tự** từ Giai đoạn M0 đến M9. Không nhảy cóc, vì giai đoạn sau dùng dữ liệu của giai đoạn trước.
2. Mỗi giai đoạn gồm: **Mục tiêu → Vì sao → Các bước → Cách tự kiểm tra → Lỗi hay gặp**.
3. Làm xong một bước thì `git commit` một lần (ví dụ `feat(shop): add Shop entity and migration`). Commit nhỏ giúp quay lui khi hỏng.
4. Mỗi giai đoạn nên làm trên **một nhánh riêng** (`feature/m1-shop`), xong và test tay đạt thì mới merge vào `main`.
5. Đừng cố hoàn hảo. Bản đầu chạy đúng luồng chính, bản sau mới thêm trường hợp biên.

---

## 1. Từ điển nhanh (đọc một lần cho quen)

| Từ | Nghĩa dễ hiểu |
|---|---|
| **Shop** | Gian hàng của một User. Một User có 1 Shop (MVP). |
| **Product** | Sản phẩm trưởng, ví dụ "Key phần mềm X". |
| **Variant** | Biến thể thực sự được bán, ví dụ "Key X - 1 năm". Giá, SKU, tồn kho nằm ở đây. |
| **SKU** | Mã nội bộ của Variant, unique trong một Shop. |
| **DigitalCode** | Một dòng = một mã / một tài khoản sẵn sàng bán. |
| **Cart** | Giỏ hàng: những thứ người mua *đang muốn* mua. |
| **Checkout** | Phiên thanh toán: bản chụp những thứ người mua *quyết định mua lần này*. |
| **ShopOrder** | Đơn của một Shop, chỉ sinh ra **sau khi đã trả tiền**. |
| **OrderItem** | Một dòng hàng trong đơn (có snapshot tên, giá). |
| **Payment** | Một lần thanh toán với cổng (tiền đã về hay chưa). |
| **Ledger (sổ cái)** | Sổ ghi tiền thuộc về ai. Chỉ thêm dòng mới, không sửa, không xóa. |
| **Payout** | Yêu cầu rút tiền của Shop. |
| **Complaint** | Khiếu nại của người mua. Khác với Refund (hoàn tiền). |
| **Idempotent** | Gọi nhiều lần vẫn cho kết quả như gọi một lần (chống bấm đúp, webhook trùng). |
| **Snapshot** | Chép giá trị tại thời điểm đó, sau này nguồn gốc đổi cũng không ảnh hưởng. |

**Mẹo nhớ nhanh:** Cart = *muốn mua* → Checkout = *chốt mua* → Payment = *tiền về* → Order = *đã bán* → Ledger = *tiền của ai*.

---

## 2. Quy trình chuẩn cho MỌI tính năng (học một lần, dùng mãi)

Với mỗi tính năng, luôn làm theo thứ tự này:

| Bước | Việc | Project |
|---|---|---|
| 1 | Viết **Entity** + quy tắc nghiệp vụ (method trong entity) | Domain |
| 2 | Viết **cấu hình EF** (khóa, index, ràng buộc) + **migration** | Infrastructure |
| 3 | Viết **Service/Handler** (use case) + DTO + validate | Application |
| 4 | Viết **Controller** (chỉ nhận request, gọi service, trả kết quả) | Api |
| 5 | Test API bằng Swagger/Postman/file `.http` | - |
| 6 | Viết **API client + hook** ở React, rồi **màn hình** | Frontend |
| 7 | Test cả luồng bằng tay, ghi lại lỗi, sửa, commit | - |

Nguyên tắc tầng (Clean Architecture):
- **Domain**: không phụ thuộc gì. Chứa entity, enum, quy tắc.
- **Application**: gọi Domain, định nghĩa interface (ví dụ `IAppDbContext`, `IEmailSender`).
- **Infrastructure**: cài đặt interface (EF Core, Brevo, Cloudinary).
- **Api**: controller, middleware, cấu hình DI. Không viết nghiệp vụ trong controller.

---

## 3. GIAI ĐOẠN M0 - Chuẩn bị nền (làm trước khi viết Shop)

### Mục tiêu
Có sẵn "đồ nghề" dùng chung để các giai đoạn sau không phải làm lại.

### Các bước

**M0.1 - Kiểm tra nền Auth hiện có**
- Có: đăng nhập JWT, refresh token, xác minh email.
- Cần thêm: **role Admin**. Thêm cột/bảng role cho User, tạo một tài khoản Admin bằng seed.
- Gắn `[Authorize(Roles = "Admin")]` cho các API admin. Test: gọi API admin bằng user thường phải ra `403`.

**M0.2 - Thêm thư mục module Marketplace**
```
Domain/Marketplace/Shops, Catalog, Orders, Payments, Ledger, Disputes, Payouts
Application/Marketplace/... (cùng tên)
Infrastructure/Marketplace/...
Api/Controllers/Marketplace/...
```

**M0.3 - Tạo các lớp dùng chung**
- `Result<T>` hoặc exception nghiệp vụ có mã lỗi (`ErrorCode`, `Message`) để API trả lỗi thống nhất (ProblemDetails).
- Lớp `Money` hoặc quy ước: **tiền là `long` (VND, không có số lẻ)**. Tuyệt đối không dùng `float/double`. Cột DB là `bigint`.
- `IClock` (lấy giờ hiện tại, dễ test) thay vì gọi `DateTime.UtcNow` khắp nơi.
- Base entity: `Id` (Guid), `CreatedAt`, `UpdatedAt`.

**M0.4 - Idempotency-Key**
- Tạo middleware/filter đọc header `Idempotency-Key` cho các POST nhạy cảm (checkout, rút tiền). Lưu bảng `IdempotencyRecord(Key, UserId, Endpoint, ResponseJson, CreatedAt)` với unique `(UserId, Key, Endpoint)`.
- Có thể làm sau ở M5, nhưng nên nghĩ trước.

**M0.5 - Thông báo (tối giản)**
- Bảng `Notification(UserId, Type, Title, Body, IsRead, CreatedAt)` + API `GET /notifications`.
- Email vẫn qua Brevo như hiện tại. Mục đích: khi Shop được duyệt/từ chối/bị khóa thì có chỗ báo.

**M0.6 - Kiểm tra môi trường**
- Dùng 2 project Supabase dev/prod như đã chốt. Mỗi giai đoạn: chạy migration trên **dev** trước.
- Tạo file `requests.http` (hoặc Postman collection) để lưu các request test.

**M0.7 - Memory Cache & Chiến lược giảm tải Database (IMemoryCache)**
- Đăng ký Memory Cache trong DI: `builder.Services.AddMemoryCache()`.
- Xây dựng tầng cache helper/service (`ICacheService` & `MemoryCacheService`) bọc quanh `IMemoryCache`:
  - Phương thức `GetOrCreateAsync<T>(key, factory, slidingExpiration, absoluteExpiration)`.
  - Phương thức `Remove(key)` và `RemoveByPrefix(prefix)` để chủ động xóa cache (Cache Invalidation).
- Quy ước đặt key rõ ràng: `categories:tree`, `categories:{id}:attributes`, `shop:{slug}:public`, `product:{slug}:detail`, `interests:all`.
- Chiến lược thời gian sống (TTL):
  - Dữ liệu tĩnh/rất ít đổi (Danh mục, Sở thích): Sliding 30 phút, Absolute 2 giờ.
  - Dữ liệu ít đổi (Hồ sơ Shop công khai, Chi tiết sản phẩm): Sliding 5 phút, Absolute 15-30 phút.
- **Nguyên tắc Invalidation:** Khi có thao tác Create/Update/Delete ở tầng Write, bắt buộc xóa cache key tương ứng ngay lập tức để người dùng không đọc phải dữ liệu cũ (stale data).

### Tự kiểm tra
- [x] Đăng nhập admin thấy được API admin, user thường bị 403 (Đã cấu hình `[Authorize(Roles = "Admin")]` và claim Role).
- [x] MemoryCache hoạt động: Tích hợp `ICacheService` & `MemoryCacheService`, cache danh sách sở thích giảm tải database.
- [x] Build backend và frontend không lỗi (dotnet build thành công 0 errors).

---

## 4. GIAI ĐOẠN M1 - Shop: tạo, duyệt, khóa

### Mục tiêu
User tạo gian hàng → Admin duyệt hoặc từ chối → Shop hoạt động; Admin có thể phạt (sanction).

### Vì sao thiết kế như vậy
Nếu để một cột `Status` chung cho mọi thứ, sau này sẽ rối (đang chờ duyệt? bị khóa? tự đóng?). Nên **tách** thành: `ApprovalStatus` (kết quả duyệt), `OperatingStatus` (chủ shop mở/đóng), `ShopSanction` (Admin phạt). Trạng thái hiển thị **tính ra khi đọc**.

### Các bước

**M1.1 - Entity & enum (Domain)**
```csharp
public enum ApprovalStatus { Pending, Approved, Rejected }
public enum OperatingStatus { Open, Closed }

public class Shop : BaseEntity
{
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? Description { get; private set; }
    public string ContactEmail { get; private set; }
    public ApprovalStatus ApprovalStatus { get; private set; } = ApprovalStatus.Pending;
    public OperatingStatus OperatingStatus { get; private set; } = OperatingStatus.Open;
    public DateTime? DeletedAt { get; private set; }

    public void Approve() { /* chỉ từ Pending → Approved */ }
    public void Reject()  { /* chỉ từ Pending → Rejected */ }
    public void Close()   { OperatingStatus = OperatingStatus.Closed; }
    public void Open()    { OperatingStatus = OperatingStatus.Open; }
}
```
Thêm `ShopReview` và `ShopSanction` với các cột:
- `ShopReview`: ShopId, SubmissionNo, SnapshotJson (bản chụp thông tin shop lúc nộp), Decision, ReasonCode, ReasonNote, CanResubmit, ReviewedByAdminId, ReviewedAt.
- `ShopSanction`: ShopId, Type, Reason, StartAt, EndAt?, CreatedByAdminId, LiftedAt?, LiftedByAdminId?.

> Quy tắc đặt trong **method của entity** (Approve/Reject), không rải trong controller. Vì vậy `private set`.

**M1.2 - Cấu hình EF + migration (Infrastructure)**
```csharp
builder.HasIndex(s => s.OwnerUserId).IsUnique();               // 1 user = 1 shop (tạm thời)
builder.HasIndex(s => s.Slug).IsUnique();
```
- `ShopReview`: unique `(ShopId, SubmissionNo)`.
- Chạy: `dotnet ef migrations add AddShops` → `dotnet ef database update` (trên dev).

> **Vì sao có unique index mà vẫn có config `MaxShopsPerUser`?** Config là *chính sách* (sau này chỉnh được). Unique index là *lớp bảo vệ cuối* của database, chống người dùng bấm tạo 2 lần cùng lúc. Khi mở rộng nhiều shop thì bỏ index này.

**M1.3 - Hàm trạng thái hiển thị (Application)**
```csharp
public enum ShopEffectiveStatus { PendingReview, Rejected, Suspended, Closed, UserUnavailable, Active, Deleted }

public static ShopEffectiveStatus Compute(User u, Shop s, bool hasActiveSanction) =>
    s.DeletedAt != null ? Deleted :
    u.Status != UserStatus.Active ? UserUnavailable :
    s.ApprovalStatus == Pending ? PendingReview :
    s.ApprovalStatus == Rejected ? Rejected :
    hasActiveSanction ? Suspended :
    s.OperatingStatus == Closed ? Closed : Active;
```
Khi User bị khóa, **không** sửa dữ liệu Shop. Mở khóa User thì Shop tự trở lại.

**M1.4 - Service tạo shop**
Điều kiện: email đã xác minh; chưa vượt `MaxShopsPerUser`.
```csharp
// Bắt DbUpdateException do unique index để trả lỗi dễ hiểu khi bấm tạo 2 lần
```
Quy trình: tạo Shop (Pending) + tạo `ShopReview` SubmissionNo = 1 với snapshot.

**M1.5 - Service duyệt**
- Chỉ một submission đang mở tại một thời điểm (dùng unique partial index hoặc kiểm tra trong transaction).
- Từ chối **bắt buộc có lý do**. Giới hạn nộp lại `MaxResubmissions=3`, chờ `ResubmitCooldownHours=24`.
- Mỗi lần duyệt/từ chối: tạo `Notification` cho chủ shop.

**M1.6 - Service sanction**
- Admin tạo sanction (có thể có hạn `EndAt`) và gỡ sanction. Sanction hiệu lực = `LiftedAt == null && (EndAt == null || EndAt > now)`.

**M1.7 - Controller**
| API | Ai |
|---|---|
| `POST /api/shops` | User |
| `GET /api/shop` (shop của tôi, kèm EffectiveStatus + lý do) | User |
| `PUT /api/shop` | User |
| `POST /api/shop/resubmit` | User |
| `POST /api/shop/close`, `POST /api/shop/open` | User |
| `GET /api/admin/shops?status=pending` | Admin |
| `GET /api/admin/shops/{id}` (kèm snapshot) | Admin |
| `POST /api/admin/shops/{id}/approve` / `reject` | Admin |
| `POST /api/admin/shops/{id}/sanctions`, `DELETE .../sanctions/{sid}` | Admin |

**M1.8 - Frontend**
Các màn hình:
1. `/shop/create`: form tạo shop (react-hook-form + zod nếu bạn đã dùng).
2. `/shop/manage`: trang trạng thái shop. Hiện EffectiveStatus bằng badge màu, lý do từ chối/khóa, nút "Nộp lại", "Đóng/Mở shop".
3. `/admin/shops`: bảng chờ duyệt → `/admin/shops/:id` xem chi tiết → nút Duyệt/Từ chối (hộp thoại nhập lý do).
4. `/admin/sanctions`: danh sách sanction.
5. Chuông thông báo trên header (gọi `GET /notifications`).

Gợi ý: tạo `src/features/marketplace/shops/` chứa `api.ts`, `hooks.ts`, `components/`, `pages/`.

### Tự kiểm tra (test tay)
- [ ] Tạo shop lần 2 bị chặn.
- [ ] Bấm tạo shop 2 lần liên tiếp rất nhanh: chỉ một shop được tạo.
- [ ] Admin từ chối không có lý do → báo lỗi.
- [ ] Khóa User → shop báo "không khả dụng"; mở khóa → shop trở lại đúng trạng thái trước.
- [ ] Shop bị sanction vẫn đăng nhập Social bình thường.

### Lỗi hay gặp
- Quên kiểm tra quyền sở hữu: user A sửa shop của user B. **Luôn lọc theo `OwnerUserId = currentUserId`.**
- Viết `if (status == ...)` rải rác. Hãy gom vào hàm `Compute` ở một chỗ.

---

## 5. GIAI ĐOẠN M2 - Danh mục & thuộc tính

### Mục tiêu
Admin quản lý cây danh mục; mỗi danh mục có bộ thuộc tính (Required/Optional) để sản phẩm nhập đúng và lọc được.

### Phiên bản tối giản cho MVP (khuyến nghị)
Bạn chỉ bán hàng số nên chỉ cần vài danh mục. **Seed sẵn** thay vì làm giao diện phức tạp: Tài khoản, Key/License, Khóa học, Tài liệu/File, Source code. Phần thuộc tính làm gọn.

### Các bước

**M2.1 - Entity**
- `Category(Id, ParentId?, Name, Slug, Path, Depth, IsActive, ProductType, ModerationPolicy, CommissionRateBp?)`.
- `AttributeDefinition(Id, Code, Name, DataType, Unit?, Min?, Max?)` với DataType: `Enum, Number, Text, Boolean`.
- `AttributeOption(Id, AttributeId, Value, Slug, IsActive)`.
- `CategoryAttribute(CategoryId, AttributeId, Requirement, IsFilterable, IsVariantAxis)` với Requirement: `Required, Recommended, Optional`.
- `CategoryRequest(Id, ShopId, Name, SuggestedParentId?, Status, ReasonNote?, ResolvedCategoryId?)`.

**M2.2 - Quy tắc**
- Tối đa 3 cấp (cấu hình). Product chỉ gắn vào **danh mục lá**.
- Hoa hồng: Category có thể ghi đè, nếu null thì dùng mặc định toàn sàn (lưu config).
- Thuộc tính kế thừa từ danh mục cha: khi lấy thuộc tính của một danh mục, gộp cả của tổ tiên.
- **Quy tắc Caching (Memory Cache):** Do cây danh mục và thuộc tính được đọc ở hầu hết mọi trang (menu sàn, bộ lọc tìm kiếm, form đăng bán sản phẩm), bắt buộc lưu cache `categories:tree` và `categories:{id}:attributes`.

**M2.3 - API**
- Công khai:
  - `GET /api/categories/tree`: Đọc từ Memory Cache trước; nếu cache miss mới truy vấn DB và ghi cache.
  - `GET /api/categories/{id}/attributes`: Cache theo `categoryId`.
- Seller: `POST /api/category-requests`.
- Admin: CRUD category, attribute, gán thuộc tính vào danh mục, xử lý request (tạo mới / ánh xạ vào danh mục có sẵn / từ chối kèm lý do).
- **Cache Invalidation:** Mỗi khi Admin thêm/sửa/xóa danh mục hoặc gán/bỏ thuộc tính, gọi `_cache.Remove("categories:tree")` và xóa key thuộc tính liên quan để dữ liệu mới cập nhật tức thì.

**M2.4 - Seed dữ liệu**
Viết `CategorySeeder` chạy lúc khởi động (chỉ thêm nếu chưa có).

**M2.5 - Frontend**
- Component `CategoryPicker` (chọn cây) dùng ở form sản phẩm.
- Admin: trang cây danh mục (có thể làm tối giản: bảng + form thêm/sửa).

### Tự kiểm tra
- [x] `GET /api/categories/tree` trả cây đúng thứ tự và phân cấp (Depth: 0, 1, 2).
- [x] Memory Cache hoạt động: Cache `categories:tree` và `categories:{id}:attributes` giảm tải tối đa số lần đọc database.
- [x] Cache Invalidation: Admin sửa/xóa danh mục hoặc gán/bỏ thuộc tính thì cache lập tức được purge để cập nhật dữ liệu mới.
- [x] Seed dữ liệu hoàn chỉnh qua `CategorySeeder` cho sản phẩm số (Tài khoản, Key bản quyền, Khóa học, Source code).
- [x] Seller có API gửi đề xuất mở danh mục mới `POST /api/category-requests` và Admin xét duyệt `POST /api/admin/category-requests/{id}/review`.

---

## 6. GIAI ĐOẠN M3 - Product, Variant, trang bán hàng công khai

> Lưu ý: trong file chat, phần vòng đời Product (Câu 3) chưa có đáp án được ghi lại. Dưới đây là **đề xuất mặc định**; bạn chỉnh nếu muốn khác.

### Mục tiêu
Seller tạo sản phẩm (Draft → đăng bán); khách xem được trang shop, danh sách, chi tiết.

### Vì sao thiết kế như vậy
- Product chỉ có `ShopId`, **không** có `OwnerUserId` (chủ shop suy ra qua Shop).
- **Giá, SKU, tồn kho nằm ở Variant**, không ở Product, để tránh hai nguồn sự thật.
- "Product đang có trạng thái gì" ≠ "Product có được bán lúc này không". Cái sau tính ra bằng `IsPubliclyVisible`.

### Vòng đời đề xuất
`Draft → (PendingReview, nếu danh mục yêu cầu duyệt) → Listed ⇄ Hidden → Archived`.
Hết hàng **không** phải một status: sản phẩm hết hàng khi mọi variant còn 0.

`IsPubliclyVisible` = Product Listed **và** Shop Active (dùng `ShopEffectiveStatus`) **và** Category + tổ tiên Active **và** không có `ProductTakedown` hiệu lực.

Khi Admin khóa Shop: **không** đổi status từng Product, chỉ cần `IsPubliclyVisible` ra `false`.

### Các bước

**M3.1 - Entity**
- `Product(Id, ShopId, CategoryId?, Name, Slug, Description, Status, MinPriceCache, ...)`.
- `ProductOption(Id, ProductId, Name, Position)`, `ProductOptionValue(Id, OptionId, Value, Position)`.
- `ProductVariant(Id, ProductId, ShopId, Sku, SkuNormalized, CombinationKey, Price, CompareAtPrice?, DeliveryType, TrackInventory, DeletedAt)`.
  - `DeliveryType`: `CodePool` (key/tài khoản), `File`, `Course`, `Manual` (để dành).
- `VariantOptionValue(VariantId, OptionValueId)`.
- `ProductAttributeValue(ProductId, AttributeId, OptionId?, TextValue?, NumberValue?, BoolValue?)`.
- `ProductImage(ProductId, Url, Position)`; `ProductTakedown(ProductId, Reason, AdminId, CreatedAt, LiftedAt?)`.

**M3.2 - Ràng buộc DB (rất quan trọng)**
```csharp
b.HasIndex(v => new { v.ShopId, v.SkuNormalized })
 .IsUnique().HasFilter("deleted_at IS NULL");        // SKU unique trong Shop
b.HasIndex(v => new { v.ProductId, v.CombinationKey }).IsUnique();
// CHECK (price > 0)
```
Tiền là `long`.

**M3.3 - Quy tắc nghiệp vụ & Cache**
- Sản phẩm không có biến thể vẫn có **đúng 1 variant mặc định** (để giỏ hàng/đơn hàng luôn làm việc với `VariantId`).
- `CombinationKey` ví dụ `goi:1nam|loai:pro` (sắp xếp ổn định) để chặn trùng tổ hợp.
- SKU: Seller nhập hoặc để trống để hệ thống tự sinh (`P{số}-{số thứ tự}`). Chuẩn hóa: trim + viết hoa.
- **Publish** (server kiểm, trả 422 theo từng field): đã có CategoryId, danh mục lá + Active, đủ thuộc tính Required, đúng DataType, mỗi variant có giá > 0, có ít nhất 1 ảnh.
- Draft lưu tự do, không kiểm thiếu.
- Sửa Product đang Listed: kiểm lại như publish.
- Xóa mềm (`DeletedAt`) khi đã có đơn.
- **Chiến lược Memory Cache trang công khai:**
  - `GET /api/products/{slug}`: Cache chi tiết sản phẩm (`product:{slug}:detail`) với TTL 10-15 phút.
  - `GET /api/shops/{slug}`: Cache thông tin shop công khai (`shop:{slug}:public`) với TTL 10-15 phút.
  - **Cache Invalidation:** Xóa cache key sản phẩm/shop khi Seller cập nhật sản phẩm, cập nhật variant/giá, đóng/mở shop, hoặc khi Admin takedown/sanction.
  - **Bất biến quan trọng:** Khi **Tạo Checkout (M5.3) và Thanh toán**, **BẮT BUỘC ĐỌC GIÁ VÀ TỒN KHO TRỰC TIẾP TỪ DATABASE** (không lấy từ cache) để đảm bảo tính toàn vẹn tiền tệ và số lượng.

**M3.4 - Service & API**
Seller (yêu cầu Shop của mình ở trạng thái cho phép tạo sản phẩm):
- `GET/POST /api/shop/products`, `GET/PUT/DELETE /api/shop/products/{id}`
- `PUT /api/shop/products/{id}/variants` (sinh ma trận variant từ option)
- `POST /api/shop/products/{id}/publish`, `.../hide`, `.../archive`
- `POST /api/shop/products/{id}/images` (upload qua Cloudinary)

Công khai:
- `GET /api/products?categoryId=&q=&page=` (chỉ trả `IsPubliclyVisible`)
- `GET /api/products/{slug}`, `GET /api/shops/{slug}`, `GET /api/shops/{slug}/products`

Admin: `POST /api/admin/products/{id}/takedown`, `DELETE .../takedown`.

> **Tìm kiếm MVP:** dùng `ILIKE '%từ khóa%'` là đủ. Đừng dựng Elasticsearch.

**M3.5 - Frontend**
- Seller: `/shop/products` (bảng), wizard tạo sản phẩm theo 5 bước: Thông tin → Danh mục & thuộc tính → Option/Variant → Giá & SKU → Ảnh → Đăng bán.
- Công khai: `/shops/:slug`, `/products`, `/products/:slug` (chọn variant, hiện giá, nút "Thêm giỏ"/"Mua ngay" sẽ nối ở M5).

### Tự kiểm tra
- [x] Ràng buộc DB & SKU: Unique `(ShopId, SkuNormalized)` và `(ProductId, CombinationKey)` trên DB. Hai shop khác nhau dùng cùng SKU được; trong cùng shop thì bị chặn.
- [x] Vòng đời Product: `Draft -> (PendingReview nếu danh mục yêu cầu) -> Listed <-> Hidden -> Archived` kết hợp `IsPubliclyVisible` động.
- [x] Caching trang công khai: Cache chi tiết sản phẩm (`product:{slug}:detail`) và shop (`shop:{slug}:public`) với TTL 15 phút; Cache Invalidation tự động khi Seller cập nhật hoặc Admin takedown.
- [x] Quản trị kiểm duyệt: Admin có API Takedown và dỡ bỏ lệnh khóa sản phẩm vi phạm.
- [x] Migration & Database: Đã tạo và áp dụng migration `20261011104753_AddProductsAndCatalog` lên PostgreSQL.

### Lỗi hay gặp
- Lưu giá ở cả Product lẫn Variant. Chỉ lưu ở Variant; `MinPriceCache` do hệ thống tự cập nhật.
- Tin giá từ client. Lúc checkout luôn đọc giá từ DB.

---

## 7. GIAI ĐOẠN M4 - Kho hàng số (mã, tài khoản, file, khóa học)

### Mục tiêu
Seller nạp hàng số vào kho; hệ thống biết còn bao nhiêu, và lưu **an toàn**.

### Các bước

**M4.1 - Entity**
```
DigitalCode(Id, ShopId, VariantId, ValueEncrypted, ValueHash,
            Status: Available|Reserved|Delivered|Invalid,
            ReservedByCheckoutId?, OrderItemId?, DeliveredAt?)
  unique (ShopId, VariantId, ValueHash)       -- chặn nạp trùng
DigitalFile(VariantId, StoragePath, MaxDownloads?)
CourseAccess(Id, UserId, VariantId, OrderItemId, GrantedAt, RevokedAt?)
```
- Tồn kho `CodePool` = số dòng `Available`.
- `File` và `Course`: `TrackInventory = false` (bán không giới hạn số lượng).

**M4.2 - Mã hóa**
- Mã hóa `Value` trước khi lưu (AES-GCM). **Khóa để ngoài DB** (biến môi trường / secret trên Render).
- `ValueHash` = hash (SHA-256 kèm "muối" cấu hình) dùng để kiểm tra trùng mà không cần giải mã.
- Không ghi mã vào log, không gửi qua email. Admin mặc định không thấy bản rõ.

> Mất khóa mã hóa = mất toàn bộ kho. Sao lưu khóa ở nơi riêng.

**M4.3 - Nạp mã hàng loạt**
- `POST /api/shop/variants/{id}/codes:bulk` nhận danh sách (dán text, mỗi dòng một mã, hoặc CSV).
- Giới hạn số dòng mỗi lần (ví dụ 1000). Trả báo cáo: thêm được bao nhiêu, trùng bao nhiêu, lỗi định dạng.

**M4.4 - Lưu file**
- File bán (tài liệu, source code) lưu ở nơi **không công khai**. Bạn đã dùng Cloudinary; với file riêng tư có thể dùng kiểu upload private/authenticated, hoặc Supabase Storage bucket private. Giao cho người mua bằng **link ký chữ ký, hết hạn ngắn**.

**M4.5 - Khóa học & Tài khoản học tập (Đã chốt thiết kế)**
- **Quyết định đã chốt**: Khóa học thực chất là hình thức chia sẻ **thông tin tài khoản và mật khẩu** (account credentials/license access).
- Hệ thống **KHÔNG** làm video player, bài học hay hệ thống LMS phức tạp.
- Khóa học dùng chung 100% cơ chế kho số bảo mật `CodePool` (mỗi bản ghi `DigitalCode` chứa thông tin theo định dạng `Tài khoản | Mật khẩu | Hướng dẫn`).
- Mua xong, hệ thống giải mã và hiển thị thông tin tài khoản cho người mua trong trang đơn hàng, tương tự như Key/License phần mềm. Tiết kiệm 80% công sức và đồng nhất luồng kho hàng.

**M4.6 - Giao diện Seller**
- Trang kho của variant: tổng/còn/đã bán, nút "Nạp mã", bảng mã (ẩn ký tự, chỉ hiện 3 ký tự cuối), đánh dấu mã `Invalid`.
- Upload file cho variant loại File.

### Tự kiểm tra
- [x] Mã hóa & Băm bảo mật: Mã hóa AES-256-GCM cho `ValueEncrypted`, băm HMAC-SHA256 cho `ValueHash`. Cột `ValueEncrypted` trong PostgreSQL hoàn toàn không đọc được bằng mắt thường.
- [x] Chống nạp trùng mã: Unique index `(ShopId, VariantId, ValueHash)`. Nạp trùng mã trong batch hoặc đã có trong DB đều được phát hiện và báo cáo chính xác số lượng trùng.
- [x] API nạp mã hàng loạt: `POST /api/shop/variants/{id}/codes/bulk` hỗ trợ dán text nhiều dòng hoặc mảng JSON, tối đa 1000 mã/lần.
- [x] Che giấu dữ liệu nhạy cảm: `GET /api/shop/variants/{id}/codes` chỉ trả về `MaskedValue` (`***-XXXX`), tuyệt đối không gửi `ValueEncrypted` cho client.
- [x] Quản lý file số: Đã có thực thể `DigitalFile` và API cấu hình tệp tin cho variant loại File.
- [x] Migration & Database: Đã tạo và áp dụng migration `20261011121424_AddDigitalInventory` lên PostgreSQL.

---

## 8. GIAI ĐOẠN M5 - Giỏ hàng, Checkout, Đơn hàng (payment giả) [ĐÃ HOÀN THÀNH BACKEND]

### Mục tiêu
Mua hàng chạy end-to-end: chọn hàng → thanh toán (giả) → nhận mã. Chưa cần cổng thật.
*(Backend: Đã hoàn thiện Domain Entities, EF Core Migrations, Repositories, Atomic Reserve FOR UPDATE SKIP LOCKED, Checkout Expiration Worker, Order Fulfillment & Decryption, CartController, CheckoutsController, OrdersController, ShopOrdersController, DevCheckoutsController)*

### Vì sao làm payment giả trước
Phần khó nhất là **kho + giữ hàng + tạo đơn đồng thời**. Làm với payment giả giúp bạn test nhanh, không bị cổng thanh toán và giấy phép làm chậm.

### Các bước

**M5.1 - Giỏ hàng**
- `Cart(UserId)`, `CartItem(CartId, VariantId, Qty)`, unique `(CartId, VariantId)`.
- Giỏ **không giữ hàng, không lưu giá**; giá và khả năng bán luôn tính lại từ DB.
- Chặn mua hàng của chính shop mình; giới hạn số lượng mỗi lần mua theo variant.
- API: `GET /cart`, `POST /cart/items`, `PATCH /cart/items/{id}`, `DELETE /cart/items/{id}`.
- Frontend: trang giỏ có ô chọn từng món (chỉ món được chọn mới vào Checkout) + nút "Mua ngay" (bỏ qua giỏ).

**M5.2 - Entity Checkout & Order**
```
Checkout(Id, CheckoutNo, BuyerUserId, Status: Open|Paid|Expired|Failed,
         ItemsTotal, DiscountTotal, GrandTotal, IdempotencyKey, ExpiresAt, PaidAt?)
CheckoutItem(Id, CheckoutId, ShopId, VariantId, Qty, UnitPrice, NameSnapshot,
             CommissionRateBp, CommissionAmount, ...)
ShopOrder(Id, OrderNo, CheckoutId, ShopId, BuyerUserId, Status: Delivered|Completed|Cancelled,
          Total, DeliveredAt?, ComplaintWindowEndsAt?, CompletedAt?)
OrderItem(Id, ShopOrderId, VariantId, ProductId, CategoryId, ProductName, VariantLabel,
          Sku, UnitPrice, Qty, LineTotal, ShopGross, CommissionRateBp, CommissionAmount,
          ShopNet, DeliveryType, RefundedQty)
OrderStatusHistory(ShopOrderId, FromStatus, ToStatus, ActorType, ActorId, Reason, At)
Fulfillment(Id, ShopOrderId, Status, DeliveredAt)
FulfillmentItem(FulfillmentId, OrderItemId, Qty)
```
Unique `(BuyerUserId, IdempotencyKey)` trên Checkout.

**M5.3 - Tạo Checkout (bước khó nhất, đọc kỹ)**
`POST /api/checkouts` (có `Idempotency-Key`). Trong **một transaction**:
1. Đọc các CheckoutItem được chọn, kiểm tra `IsPubliclyVisible`, lấy giá từ DB, chặn mua của chính mình.
2. Tính hoa hồng từng dòng: `Commission = round(ShopGross * RateBp / 10000)`. Thứ tự tỷ lệ: ghi đè theo shop > danh mục > mặc định.
3. Tạo `Checkout (Open, ExpiresAt = now + 15 phút)` + `CheckoutItem`.
4. Với mỗi variant **sắp xếp theo VariantId tăng dần** (để tránh deadlock) giữ mã:
```sql
UPDATE digital_codes
SET status = 'Reserved', reserved_by_checkout_id = @checkoutId
WHERE id IN (
  SELECT id FROM digital_codes
  WHERE variant_id = @variantId AND status = 'Available'
  ORDER BY id
  LIMIT @qty
  FOR UPDATE SKIP LOCKED
);
-- số dòng bị ảnh hưởng < @qty  →  hết hàng  →  ROLLBACK, báo rõ món nào hết
```
Trong EF Core dùng `Database.ExecuteSqlInterpolatedAsync(...)` và đọc số dòng trả về.

> Vì sao an toàn? `FOR UPDATE SKIP LOCKED`: hai người mua cùng lúc sẽ nhận hai nhóm mã khác nhau, không chờ nhau, không trùng mã.

**M5.4 - Xử lý "thanh toán thành công" (dùng chung cho mock và thật)**
Viết **một** service `CompleteCheckoutPaymentAsync(checkoutId, providerTxnId)`:
1. Nếu `ProviderTxnId` đã xử lý rồi → bỏ qua (idempotent).
2. Trong transaction: `Checkout Open → Paid`; sinh `ShopOrder` + `OrderItem` (sao chép snapshot từ CheckoutItem); mã `Reserved → Delivered` (gắn `OrderItemId`); file thì cấp link; khóa học thì tạo `CourseAccess`; tạo `Fulfillment` + `FulfillmentItem`; `ShopOrder → Delivered`, đặt `ComplaintWindowEndsAt = now + 48h` (cấu hình).
3. Xóa các dòng đã mua khỏi giỏ; tạo thông báo cho người mua và shop.
4. **Trường hợp biên:** tiền về sau khi Checkout đã hết hạn → thử giữ chỗ lại; còn mã thì xử lý bình thường, hết thì đánh dấu "cần hoàn tiền" (làm hẳn ở M7).

**M5.5 - Payment giả (chỉ môi trường dev)**
- `POST /api/dev/checkouts/{id}/mock-pay?result=success|fail` gọi service ở M5.4 / hủy Checkout.
- Bọc bằng `if (env.IsDevelopment())` để **không lọt lên production**.

**M5.6 - Giải phóng mã khi hết hạn**
- Job nền (xem lưu ý Render ở mục 14): Checkout `Open` quá hạn → `Expired`; mã `Reserved` → `Available`.

**M5.7 - API còn lại**
`GET /api/checkouts/{id}` (kèm QR/đếm ngược), `GET /api/orders`, `GET /api/orders/{id}` (kèm mã/link đã giao, **chỉ người mua xem được**), `GET /api/shop/orders` (shop chỉ thấy đơn đã trả tiền của mình).

**M5.8 - Frontend**
- `/checkout/:id`: tóm tắt đơn, đếm ngược 15 phút, (dev) nút "Thanh toán thử".
- `/orders`, `/orders/:id`: danh sách đơn, xem mã đã nhận (nút "Hiện/Ẩn", "Sao chép").
- Poll trạng thái Checkout mỗi vài giây (hoặc SSE sau này) để chuyển trang khi đã trả.

### Tự kiểm tra (bắt buộc làm)
- [ ] Còn 1 mã, hai người mua cùng lúc: một người thành công, một người báo hết hàng.
- [ ] Viết một test tích hợp dùng `Task.WhenAll` gọi tạo Checkout song song 20 lần cho kho 10 mã → đúng 10 thành công, không trùng mã.
- [ ] Gọi `CompleteCheckoutPayment` hai lần cùng `providerTxnId` → chỉ tạo một đơn.
- [ ] Bấm "Mua" hai lần (cùng Idempotency-Key) → một Checkout.

### Lỗi hay gặp
- Giữ mã theo kiểu "SELECT đếm → rồi UPDATE" (dễ bán trùng). Phải gộp trong **một** câu lệnh như trên.
- Quên rollback khi một dòng hết hàng.
- Lấy giá từ client.

---

## 9. GIAI ĐOẠN M6 - Thanh toán thật + Sổ cái (Ledger) [ĐÃ HOÀN THÀNH BACKEND]

### Mục tiêu
Nhận tiền qua cổng/QR thật; ghi nhận **tiền thuộc về ai** bằng sổ cái. Không có cột `Shop.Balance`.
*(Backend: Đã hoàn thiện Domain Entities Payments & Double-Entry Ledger, EF Core Migrations, Repositories, Bất biến Debit == Credit, 2-Phase Webhook Transaction, Worker tự động giải phóng tiền 48h FundReleaseWorker, PaymentsController, PaymentWebhooksController, ShopFinanceController)*

### Vì sao sổ cái
`Shop.Balance += Order.Total` rất dễ sai khi có hoàn tiền, hoa hồng, webhook trùng, rút tiền. Sổ cái chỉ thêm dòng, số dư = tổng các dòng, truy vết được từng đồng.

### Các bước

**M6.1 - Chốt cổng thanh toán**
- Việc này **chưa được quyết** trong chat. Chốt trước khi làm: cổng hỗ trợ QR/chuyển khoản, có webhook, hoàn tiền một phần hay không, phí bao nhiêu, và điều kiện pháp lý với mô hình sàn. Đọc tài liệu của nhà cung cấp trước khi thiết kế.
- Viết interface `IPaymentGateway` (`CreatePayment`, `VerifyWebhook`, `Refund`) để thay nhà cung cấp mà không đụng nghiệp vụ.

**M6.2 - Entity Payment**
```
Payment(Id, CheckoutId, Provider, ProviderTxnId, Method, Amount, Currency,
        Status: Initiated|Succeeded|Failed|Expired, PaidAt?, RawPayloadRef)
  unique (Provider, ProviderTxnId)
  unique (CheckoutId) WHERE Status = 'Succeeded'
```
Payment gắn với **Checkout**, không gắn Shop/ShopOrder (việc chia tiền cho shop do sổ cái làm).

**M6.3 - Sổ cái**
```
LedgerAccount(Id, Code, OwnerType, OwnerId?)
  -- GatewayClearing, UnallocatedPayments, ShopPending, ShopAvailable,
  -- PayoutPending, BankClearing, PlatformRevenue, PlatformExpense, RefundPayable
LedgerTransaction(Id, Type, RefType, RefId, IdempotencyKey UNIQUE, Memo, CreatedAt)
LedgerEntry(Id, TransactionId, AccountId, Debit, Credit)       -- append-only
```
- **Chỉ một cửa ghi sổ**: `LedgerService.Post(...)`. Nơi khác cấm ghi vào `LedgerEntry`.
- `Post` luôn kiểm: **tổng Nợ = tổng Có**, rồi mới lưu, trong cùng transaction DB.
- Chặn UPDATE/DELETE trên `ledger_entries` (trigger hoặc phân quyền DB).
```csharp
public async Task PostAsync(string type, string idempotencyKey, IEnumerable<Line> lines)
{
    if (lines.Sum(l => l.Debit) != lines.Sum(l => l.Credit))
        throw new InvalidOperationException("Ledger không cân");
    // nếu IdempotencyKey đã tồn tại → return (đã ghi rồi)
    // tạo LedgerTransaction + LedgerEntry
}
```

**M6.4 - Webhook thanh toán (hai giao dịch tách biệt)**
`POST /api/webhooks/payments` (verify chữ ký của cổng, **không** dùng `[Authorize]` kiểu user):
- **Giao dịch A (luôn phải thành công, idempotent):** `Payment → Succeeded`; ghi sổ `Nợ GatewayClearing / Có UnallocatedPayments`.
- **Giao dịch B:** gọi `CompleteCheckoutPaymentAsync` (M5.4), rồi ghi sổ phân bổ:
```
Nợ UnallocatedPayments  300.000
  Có ShopPending[Shop X]      90.000
  Có ShopPending[Shop Y]     180.000
  Có PlatformRevenue          30.000
```
- A xong mà B lỗi: tiền nằm ở `UnallocatedPayments` (thấy được khi đối soát), job nền thử lại; hết lượt thì hoàn tiền.
- Người mua quét QR hai lần: lần thành công thứ hai **tự hoàn**, không ghi doanh thu.

**M6.5 - Giải phóng tiền cho Shop**
Khi `ShopOrder → Completed` (hết cửa sổ khiếu nại, không có khiếu nại mở): `Nợ ShopPending / Có ShopAvailable` (key `release:{shopOrderId}`).

**M6.6 - Số dư (cache)**
- `ShopBalanceCache(ShopId, Pending, Available, PayoutPending, Version)` trong database được cập nhật **trong cùng transaction** với `LedgerService.Post` (bảng read-model giúp xem nhanh số dư mà không phải `SUM` toàn bộ bảng sổ cái).
- Cache lệch thì job sửa **cache từ sổ cái**, không bao giờ sửa sổ cái cho khớp cache.
- **Tầng Memory Cache cho Dashboard tài chính:** Bọc kết quả API `GET /api/shop/balance` trong Memory Cache với TTL ngắn (30-60 giây) để giảm tải DB khi chủ shop refresh liên tục; xóa cache ngay khi phát sinh giao dịch sổ cái liên quan đến shop đó.
- **Quy tắc an toàn khi Rút tiền (M8):** Thao tác kiểm tra số dư và trừ tiền khi rút **bắt buộc đọc từ database với `FOR UPDATE`**, tuyệt đối không đọc từ Memory Cache.

**M6.7 - Frontend**
- `/shop/finance`: 4 con số (Đang giữ / Có thể rút / Đang chuyển / Đã rút) + bảng sao kê lấy từ ledger.
- Trang thanh toán thật: hiển thị QR từ cổng.

### Tự kiểm tra (viết thành test)
- [ ] Mọi giao dịch sổ cái: tổng Nợ = tổng Có.
- [ ] Gửi cùng một webhook 5 lần → chỉ ghi sổ một lần.
- [ ] Ví dụ: Shop X 100.000, Shop Y 200.000, hoa hồng 10% → X net 90.000, Y net 180.000, sàn 30.000.
- [ ] Bất biến: `Payment + PlatformDiscount = ShopNet + Commission + SellerDiscount`.

---

## 10. GIAI ĐOẠN M7 - Khiếu nại & hoàn tiền [ĐÃ HOÀN THÀNH BACKEND]

### Mục tiêu
Chính sách "không hoàn tiền trừ khi hàng lỗi": người mua khiếu nại có bằng chứng; shop và Admin xử lý.
*(Backend: Đã hoàn thiện Domain Entities Complaints & Refunds, EF Core Migrations, Repositories, Bút toán sổ cái hoàn tiền Nợ ShopPending + Nợ PlatformRevenue / Có RefundPayable, Worker tự động chuyển Admin khi Shop im lặng 24h ComplaintEscalationWorker, Khóa giải phóng tiền khi có khiếu nại mở trong FundReleaseWorker, ComplaintsController, ShopComplaintsController, AdminComplaintsController)*

### Ba vòng đời tách nhau (nhớ kỹ)
`Complaint` (có tranh chấp) → `Refund` (phát sinh nghĩa vụ trả tiền) → hoàn tiền thật qua cổng (Gateway refund).

### Các bước

**M7.1 - Entity**
```
Complaint(Id, ShopOrderId, OrderItemId, ReasonCode, Description, Status, Resolution?,
          ShopDeadlineAt, ResolvedByAdminId?, ResolvedAt?)
ComplaintEvidence(Id, ComplaintId, FileUrl, Note)
Refund(Id, PaymentId, ShopOrderId, OrderItemId?, Amount, Bearer: Shop|Platform|Shared,
       ReasonCode, ComplaintId?, Status, ProviderRefundId?)
BuyerRiskFlag(UserId, Type, Note, CreatedAt)
```

**M7.2 - Quy tắc**
- Khiếu nại **theo từng OrderItem**, trong `ComplaintWindowEndsAt`, **bắt buộc có bằng chứng**.
- Lý do hợp lệ: mã sai / mã đã bị dùng, file hỏng / sai mô tả, không đăng nhập được tài khoản đã giao.
- Luồng: Buyer gửi → Shop có 24h phản hồi → Shop đồng ý (thay mã mới từ kho hoặc hoàn) thì hệ thống tự xử lý → Shop từ chối hoặc **im lặng quá 24h** thì chuyển Admin quyết định (**không tự hoàn**).
- Mã bị xác định lỗi → `Invalid` (không trả lại kho).
- Hoàn theo quantity: 3 mã lỗi 1 → hoàn đúng giá 1 mã.
- Khi còn Complaint mở: ShopOrder chưa được `Completed`, tiền dòng đó tiếp tục ở `ShopPending`.

**M7.3 - Ghi sổ khi hoàn (ví dụ đơn 300.000, hoa hồng 10%, hoàn 50.000 do shop lỗi)**
```
Nợ ShopPending[Shop]   45.000      (shop chịu)
Nợ PlatformRevenue      5.000      (sàn hoàn lại hoa hồng phần đó)
  Có RefundPayable             50.000
-- khi cổng xác nhận đã hoàn:
Nợ RefundPayable       50.000
  Có GatewayClearing           50.000
```
Lỗi do hệ thống (thu trùng, giao sai do lỗi sàn): sàn chịu đủ 50.000.
Nếu hoàn sau khi tiền đã sang `ShopAvailable` thì có thể âm: xem M8.

**M7.4 - API**
- Buyer: `POST /api/orders/{id}/complaints`, `GET /api/complaints`.
- Shop: `GET /api/shop/complaints`, `POST /api/shop/complaints/{id}/respond`.
- Admin: `GET /api/admin/complaints`, `POST /api/admin/complaints/{id}/resolve`.

**M7.5 - Frontend**
- Form khiếu nại có upload ảnh/video màn hình, trang theo dõi, nút phản hồi của Shop, hàng đợi của Admin.
- Hiển thị chính sách hoàn tiền rõ ở trang sản phẩm và bước thanh toán, kèm ô xác nhận "đã đọc".

### Tự kiểm tra
- [x] Shop im lặng quá 24h → khiếu nại hiện trong hàng đợi Admin qua ComplaintEscalationWorker, **không** tự hoàn.
- [x] Hoàn 50.000: Bút toán đảo Nợ ShopPending 45k + Nợ PlatformRevenue 5k / Có RefundPayable 50k, cân đối toàn vẹn sổ cái kép.
- [x] Còn khiếu nại mở thì đơn không chuyển `Completed` và FundReleaseWorker bị chặn giải phóng tiền.

---

## 11. GIAI ĐOẠN M8 - Rút tiền (Payout)

### Mục tiêu
Shop Owner rút được tiền trong `ShopAvailable`, an toàn khi bấm đúp hoặc gửi song song.

### Gợi ý cho MVP của sinh viên
Làm **payout thủ công**: Shop tạo yêu cầu → Admin chuyển khoản tay → Admin bấm "Đã chuyển". Sổ cái và trạng thái vẫn đúng như bản tự động; phần gọi API ngân hàng làm sau khi có đủ điều kiện pháp lý.

### Các bước

**M8.1 - Entity**
```
ShopBankAccount(Id, ShopId, BankCode, AccountNumberEncrypted, AccountHolderName,
                Status: Pending|Verified|Disabled, LockedUntil?)
Payout(Id, ShopId, BankAccountId, Amount, Fee, Status, RequestIdempotencyKey UNIQUE,
       RequestedBy, ApprovedBy?, RejectReason?, ProviderPayoutId?, FailureCode?, ...)
PayoutStatusHistory(...)
```
Ràng buộc: tối đa một payout đang mở mỗi shop:
`unique (ShopId) WHERE Status IN ('Requested','Approved','Processing','Unknown')`.

**M8.2 - Quy tắc**
- Chỉ **Shop Owner** rút. Admin chỉ duyệt/giữ/từ chối.
- Tài khoản nhận tiền phải đăng ký trước và trùng tên chủ shop. Đổi tài khoản thì khóa rút 24-72h.
- Tối thiểu 100.000đ (cấu hình), có hạn mức mỗi ngày.
- Shop đang bị sanction hoặc chưa KYC thì không rút.
- Yêu cầu OTP khi tạo yêu cầu rút.

**M8.3 - Tạo yêu cầu rút (chống rút trùng)**
Trong **một transaction**:
```
1. SELECT ... FROM shop_balance_cache WHERE shop_id=@s FOR UPDATE;   -- khóa dòng
2. Kiểm tra Available >= Amount, Amount >= Minimum, Available > 0
3. Tạo Payout (Requested)
4. LedgerService.Post:  Nợ ShopAvailable / Có PayoutPending   (key payout:{id}:requested)
5. Cập nhật cache
COMMIT
```
Hai request song song: request thứ hai thấy số dư đã bị trừ.

**M8.4 - Vòng đời**
`Requested → Approved → Processing → Succeeded | Failed | Unknown`; `Requested/Approved → Rejected/Cancelled`.
- Thành công: `Nợ PayoutPending / Có BankClearing`.
- Rejected/Cancelled/Failed rõ ràng: **bút toán đảo** `Nợ PayoutPending / Có ShopAvailable`. Không sửa dòng cũ.
- **Timeout là `Unknown`, không phải Failed.** Không tự đảo tiền; đối soát trước, tránh shop nhận hai lần.

**M8.5 - Số dư âm**
Nếu có hoàn tiền sau khi tiền đã release, `Available` có thể âm = shop đang nợ sàn: chặn rút; các lần release sau tự bù vào số âm; hiện banner cho shop.

**M8.6 - API & Frontend**
- Shop: `GET /api/shop/balance`, `GET /api/shop/ledger`, `POST /api/shop/bank-accounts`, `POST /api/shop/payouts` (kèm Idempotency-Key + OTP), `GET /api/shop/payouts`, `POST /api/shop/payouts/{id}/cancel`.
- Admin: `GET /api/admin/payouts`, `POST .../approve|reject|hold|mark-paid`.
- Giao diện: trang số dư, thêm tài khoản ngân hàng, form rút, lịch sử; Admin có hàng đợi payout.

### Tự kiểm tra
- [x] Gửi 2 request rút song song với cùng số dư: partial unique filter index trên database `UNIQUE ("ShopId") WHERE "Status" IN ('Requested','Approved','Processing','Unknown')` chặn tuyệt đối việc tạo đúp.
- [x] Hủy payout hoặc Admin từ chối → Available quay lại đúng số tiền trong cache, sổ cái kép được ghi nhận bút toán đảo `Nợ PayoutPending / Có ShopAvailable`.
- [x] Rút tiền yêu cầu tối thiểu 100.000đ, kiểm tra số dư khả dụng, mã hóa AES-256-GCM số tài khoản và khóa rút 24h sau khi cập nhật tài khoản nhận tiền.
- [x] Khi Admin chuyển khoản thành công (`mark-paid`), ghi bút toán `Nợ PayoutPending / Có BankClearing`, trừ dứt điểm số dư chờ rút.
- [x] Khi lỗi mạng/chưa rõ (`mark-unknown`), ghi nhận trạng thái Unknown chờ tra soát, TUYỆT ĐỐI không đảo tiền để chống shop nhận 2 lần.

---

## 12. GIAI ĐOẠN M9 - Hoàn thiện & an toàn

**M9.1 - Job nền (Đã hoàn tất)**
- [x] `CheckoutExpirationWorker`: Tự động hủy checkout quá hạn và giải phóng kho hàng số sau 15 phút.
- [x] `FundReleaseWorker`: Tự động hoàn tất ShopOrder và giải phóng tiền tạm giữ sang `ShopAvailable` sau 48h khi không có khiếu nại mở.
- [x] `ComplaintEscalationWorker`: Tự động chuyển khiếu nại lên Admin phân xử khi Shop im lặng quá 24h.
- [x] `DailyReconciliationWorker`: Chạy nền định kỳ (mỗi 6h / ban đêm) đối soát toàn hệ thống và tự động sửa lệch cache số dư (`Self-healing`).

**M9.2 - Đối soát sổ cái & Tự kiểm tra tính toàn vẹn (Đã hoàn tất)**
- [x] Kiểm tra cân đối toàn diện: Tổng Nợ (`Debit`) = Tổng Có (`Credit`) trên toàn bộ sổ cái.
- [x] Đối soát tài khoản đối ứng `PayoutPending` khớp 100% với tổng số tiền các lệnh rút Payout đang active (`Requested`, `Approved`, `Processing`, `Unknown`).
- [x] Kiểm tra khớp từng dòng `ShopBalanceCache` với tổng hợp thực tế từ các tài khoản Ledger của từng Shop.
- [x] API Admin Audit: `GET /api/admin/audit/reconciliation` (chạy đối soát tức thì) và `POST /api/admin/audit/fix-balance/{shopId}` (tự động tính lại từ sổ cái bất biến để sửa cache).

**M9.3 - Chống lạm dụng (Đã hoàn tất)**
- [x] Rate Limiting built-in của ASP.NET Core: `checkout-policy` (15 req/phút), `report-policy` (10 req/phút), `global-policy` (60 req/phút), chặn 429 Too Many Requests khi có hành vi spam / bot.
- [x] Tính năng Báo cáo vi phạm sản phẩm / gian hàng: Entity `ProductReport`, chống spam báo cáo trùng lặp `UNIQUE(ReporterUserId, ProductId) WHERE Status = 'Pending'`.
- [x] API Báo cáo: Người mua gửi báo cáo vi phạm `POST /api/products/{id}/report`; Quản trị viên phân xử `GET/POST /api/admin/reports` (gỡ sản phẩm vi phạm, áp dụng chế tài phạt gian hàng hoặc bác bỏ).
- [x] Chỉ số hiệu suất gian hàng & Tỷ lệ đơn lỗi (`ShopPerformance.DefectRate %`): Tự động tính tỷ lệ khiếu nại hợp lệ trên tổng đơn, phân tầng xếp hạng (Excellent, Good, Fair, HighRisk) và cảnh báo cờ rủi ro cao khi `DefectRate > 5%` để gợi ý Admin xử phạt.

**M9.4 - Tuân thủ (cần hỏi chuyên gia)**
- Danh mục cấm hoặc cần duyệt trước (hàng giả, vi phạm bản quyền, tài khoản lấy cắp).
- Thuế khấu trừ cho người bán, giấy phép trung gian thanh toán, KYC. Các nội dung này **cần xác nhận với kế toán hoặc luật sư**, không tự suy ra.

**M9.5 - Triển khai**
Backend Docker lên Render, frontend Vercel (proxy `/api`). Đặt biến môi trường: khóa mã hóa, secret webhook, thông tin cổng thanh toán. Chạy migration prod có sao lưu trước.

---

## 13. Bộ test nên có (ưu tiên theo mức rủi ro mất tiền)

| Nhóm | Test tối thiểu |
|---|---|
| Kho | 20 request song song cho 10 mã → đúng 10 thành công, không trùng |
| Checkout | Cùng Idempotency-Key → một Checkout |
| Webhook | Gửi trùng 5 lần → một đơn, một bút toán |
| Ledger | Mọi giao dịch cân Nợ/Có; không sửa/xóa được entry |
| Hoàn tiền | Ví dụ 300k/50k khớp số |
| Payout | Rút song song chỉ một thành công; Unknown không đảo tiền |
| Quyền | User A không đọc/sửa dữ liệu của User B/Shop B |

Dùng xUnit + Testcontainers (PostgreSQL thật) cho test có khóa/transaction; không nên giả lập DB bằng in-memory vì hành vi khóa khác PostgreSQL.

---

## 14. Lưu ý riêng cho Render free + Supabase

- Service free trên Render có thể **ngủ khi không có truy cập**, nên job nền chạy trong tiến trình (`BackgroundService`) có thể không chạy đúng giờ. Cách giảm rủi ro: dùng `pg_cron` trên Supabase hoặc dịch vụ cron ngoài gọi endpoint nội bộ có khóa bí mật; đồng thời thiết kế để thao tác "dọn dẹp" an toàn khi chạy trễ.
- Webhook từ cổng có thể đến lúc service đang ngủ: kiểm tra cơ chế thử lại của cổng.
- Dùng connection string đúng chế độ pooling của Supabase và thử với transaction/`FOR UPDATE`; kiểm tra tài liệu Supabase về chế độ pooler nào hỗ trợ.

---

## 15. Lộ trình gợi ý (ước lượng của người viết, tùy tốc độ của bạn)

| Giai đoạn | Ước lượng |
|---|---|
| M0 Nền | 3-5 ngày |
| M1 Shop | 1-1,5 tuần |
| M2 Danh mục | 3-5 ngày |
| M3 Product/Variant | 1,5-2 tuần |
| M4 Kho hàng số | 1 tuần |
| M5 Cart/Checkout/Order | 2 tuần |
| M6 Payment + Ledger | 2 tuần |
| M7 Khiếu nại/hoàn tiền | 1,5 tuần |
| M8 Rút tiền | 1-1,5 tuần |
| M9 Hoàn thiện | 1-2 tuần |

Tổng khoảng 3 đến 4 tháng nếu làm đều. Có thể **dừng ở M5 + bán thử** (payment giả cho nhóm bạn dùng thử) để kiểm chứng ý tưởng trước khi đầu tư M6-M8.

---

## 16. Checklist trước khi nói "xong một giai đoạn"

- [ ] Migration chạy được từ DB trống.
- [ ] Mọi API có kiểm quyền (đúng người, đúng vai trò).
- [ ] Validate ở server, không chỉ ở form React.
- [ ] Các trường hợp lỗi trả thông báo rõ ràng, màn hình không bị trắng.
- [ ] Test các tình huống "bấm đúp" và "hai người cùng lúc" ở những chỗ liên quan tiền/kho.
- [ ] Đã kiểm tra cơ chế Cache Invalidation cho các API dùng Memory Cache (dữ liệu mới cập nhật ngay, không bị dính cache cũ).
- [ ] Đã commit, đã ghi chú những gì làm tạm (TODO) vào README.

---

## 17. Những điểm kỹ thuật & nghiệp vụ đã chốt

1. **Vòng đời Product**: `Draft → Listed ⇄ Hidden → Archived` (hết hàng do Variant kiểm soát).
2. **Cơ chế giao hàng số**: Thống nhất dùng kho `DigitalCode` cho Key, Tài khoản phần mềm, và Khóa học. Dùng `DigitalFile` cho file tải về có link chữ ký.
3. **Khóa học [ĐÃ CHỐT]**: Chỉ chia sẻ thông tin tài khoản và mật khẩu (Account credentials/license access). Giao tự động từ kho dữ liệu `DigitalCode`, không làm video hay hệ thống bài học/LMS.
4. **Convention Codebase**:
   - `BaseEntity`: Dùng `CreatedAtUtc`, `UpdatedAtUtc` (khớp với codebase hiện có).
   - `User`: Mở rộng thêm `Role` (Admin/Member) và `Status` (Active/Suspended) trước khi chạy module Shop.
5. **Cổng thanh toán & Sổ cái**: Triển khai Mock Payment ở giai đoạn mua hàng trước; cổng thanh toán thật (PayOS/VietQR) và sổ cái hoàn tất ở giai đoạn sau.
6. **KYC và thuế**: Giai đoạn MVP chỉ làm Payout thủ công (Admin duyệt và chuyển khoản tay).
7. **Chiến lược Memory Cache (`IMemoryCache`)**:
   - Dùng Memory Cache (`ICacheService`) cho các dữ liệu đọc nhiều, ít biến động (Sở thích Interests, Cây danh mục Categories, Thuộc tính Category Attributes, Chi tiết sản phẩm công khai, Hồ sơ shop công khai).
   - Cơ chế Cache Invalidation chủ động: Bắt buộc xóa key cache liên quan khi có thao tác Write/Update/Delete để đảm bảo tính nhất quán dữ liệu.
   - Nguyên tắc bất biến về giao dịch: Giỏ hàng, Checkout giữ mã (`FOR UPDATE SKIP LOCKED`), Ghi sổ cái Ledger, và Rút tiền Payout luôn truy vấn trực tiếp từ PostgreSQL trong Transaction an toàn, cấm dùng Memory Cache.

---

## 18. GIAI ĐOẠN FRONTEND - Hiện trạng & Kế hoạch hoàn thiện chi tiết

### A. Những thành phần đã hoàn tất (Đã hoạt động tốt)
1. **Design System & Theme (`marketplace.css`):** Giao diện Dark mode, Glassmorphism, chuẩn WCAG AA, responsive.
2. **Khám phá Chợ số (`MarketplacePage.tsx` - `/marketplace`):** Danh mục, tìm kiếm realtime, grid sản phẩm, phân trang.
3. **Chi tiết Sản phẩm (`ProductDetailPage.tsx` - `/marketplace/products/:slug`):** Gallery ảnh, chọn biến thể, giá & tồn kho, Thêm giỏ / Mua ngay, Báo cáo vi phạm.
4. **Giỏ hàng cơ bản (`CartPage.tsx` - `/cart`):** Zustand sync, gom nhóm theo Shop, tăng giảm số lượng, tính tổng.
5. **Thanh toán VietQR PayOS (`CheckoutPage.tsx` - `/checkout/:id`):** Mã QR PayOS NAPAS 247, sao chép 1 chạm, đếm ngược 15 phút, polling thanh toán.
6. **Đơn mua & Nhận mã (`MyOrdersPage.tsx` - `/orders`):** Danh sách đơn mua, giải mã AES mã bản quyền, gửi khiếu nại cơ bản.
7. **Hồ sơ Shop công khai (`ShopProfilePage.tsx` - `/marketplace/shops/:slug`):** Thông tin shop, tìm kiếm và grid sản phẩm trong shop.
8. **Admin Dashboard nền tảng (`AdminDashboardPage.tsx` - `/admin`):** Duyệt shop, xử phạt, hàng đợi Payout, phán quyết khiếu nại, xử lý báo cáo, kiểm toán sổ cái kép, phân quyền user.
9. **Thông báo hợp nhất (`NotificationBell.tsx`):** Kết hợp thông báo hệ thống API Backend và SignalR realtime.

---

### B. Danh mục các công việc Frontend cần triển khai hoàn thiện (Chi tiết từng bước)

#### 🔹 Nhóm F1: Kênh Người Bán (Seller Portal - `SellerPortalPage.tsx`)
- [x] **F1.1 - Wizard/Form Tạo & Đăng bán sản phẩm mới:**
  - Nút "+ Thêm sản phẩm mới" tại Tab Sản phẩm.
  - Wizard 4 bước: (1) Tên, Slug, Danh mục lá (tree), Mô tả; (2) Biến thể, SKU, Giá bán, Giá gốc; (3) Ảnh sản phẩm; (4) Xem lại & Đăng bán (`POST /api/shop/products` + `.../publish`).
  - Menu thao tác trên từng sản phẩm: Đăng bán, Tạm ẩn (`/hide`), Bỏ ẩn (`/unhide`), Lưu trữ (`/archive`).
- [x] **F1.2 - Tab Quản lý Đơn hàng bán của Shop:**
  - Thêm Tab thứ 5 "Đơn hàng của shop" (`orders`).
  - Gọi API `GET /api/shop/orders`, hiển thị bảng: Mã đơn, Thời gian, Mặt hàng, Số lượng, Doanh thu, Hoa hồng sàn, Thực nhận (`ShopNet`), Trạng thái giao mã.
- [x] **F1.3 - Chi tiết Kho mã & Hủy mã lỗi (Invalidate):**
  - Nút "Xem kho mã" bên cạnh nút "+ Nạp kho" ở mỗi biến thể.
  - Modal xem bảng mã `***-XXXX`, trạng thái (Available, Reserved, Delivered, Invalid).
  - Nút đánh dấu mã hỏng (`Invalidate`) qua `POST /api/shop/variants/{id}/codes/{codeId}/invalidate`.
- [x] **F1.4 - Bảng Sao kê Sổ cái Tài chính (Ledger Statement):**
  - Tích hợp API `GET /api/shop/finance/statement`.
  - Hiển thị bảng sao kê biến động Nợ/Có (tiền vào, giải phóng tiền 48h, phí hoa hồng, hoàn tiền, rút tiền).
- [x] **F1.5 - Cài đặt Shop & Điều khiển Trạng thái:**
  - Nút Đóng / Mở shop (`/api/shop/close`, `/api/shop/open`).
  - Form chỉnh sửa thông tin shop (`PUT /api/shop`).
  - Hiển thị lý do từ chối và nút "Nộp lại hồ sơ xét duyệt" (`/api/shop/resubmit`) khi shop bị Rejected.

#### 🔹 Nhóm F2: Trải Nghiệm Khách Mua Hàng (Buyer Flow)
- [x] **F2.1 - Chọn từng món trong Giỏ hàng (`CartPage.tsx`):**
  - Checkbox "Chọn tất cả" và Checkbox từng sản phẩm/shop.
  - Chỉ tính tiền và đưa các món được tick chọn vào Checkout.
- [x] **F2.2 - Nút Thanh toán Thử nghiệm Dev (`CheckoutPage.tsx`):**
  - Bổ sung cụm nút "Thanh toán thử (Dev Mode)" gọi `POST /api/dev/checkouts/{id}/mock-pay` để test nhận mã số tức thì mà không phụ thuộc PayOS thật.
- [x] **F2.3 - Đính kèm Bằng chứng Khiếu nại (`MyOrdersPage.tsx`):**
  - Thêm ô nhập link ảnh/video bằng chứng gửi vào `evidenceUrls` của `CreateComplaintRequest`.

#### 🔹 Nhóm F3: Cổng Quản Trị Sàn (Admin Dashboard - `AdminDashboardPage.tsx`)
- [x] **F3.1 - Tab Quản lý Cây Danh mục & Thuộc tính (`AdminCategoriesTab.tsx`):**
  - Thêm Tab "Cây danh mục & Thuộc tính" (`categories`).
  - Quản lý cây danh mục (phân cấp cha/con, thêm/sửa/xóa, tỷ lệ hoa hồng riêng).
  - Quản lý từ điển thuộc tính (Enum, Number, Text, Boolean) & gán cấu hình thuộc tính vào danh mục.
  - Hàng đợi duyệt yêu cầu mở danh mục mới từ Seller (`CategoryRequest`) kèm modal phê duyệt/từ chối.
- [x] **F3.2 - Tab Rà soát & Takedown Sản phẩm toàn sàn (`AdminProductsTab.tsx`):**
  - Thêm Tab "Toàn bộ Sản phẩm & Takedown" (`products`).
  - Xem danh sách toàn bộ sản phẩm các shop kèm phân trang, lọc từ khóa, lọc theo trạng thái vòng đời và lọc sản phẩm vi phạm.
  - Thao tác trực tiếp: Nút Takedown kèm modal ghi nhận lý do vi phạm chính sách & Nút Dỡ bỏ lệnh khóa (Lift Takedown).

#### 🔹 Nhóm F4: Đồng Bộ Định Tuyến & Trải Nghiệm Khách Hàng (Routing & Discovery Flow)
- [x] **F4.1 - Đồng bộ Subpath URL cho Kênh Người Bán & Quản Trị Viên:**
  - Hỗ trợ deep-link và đồng bộ tab tự động khi điều hướng hoặc tải lại trang:
    - Kênh Seller (`/seller/overview`, `/seller/finance`, `/seller/products`, `/seller/orders`, `/seller/complaints`).
    - Kênh Admin (`/admin/shops`, `/admin/payouts`, `/admin/complaints`, `/admin/reports`, `/admin/audit`, `/admin/users`, `/admin/categories`, `/admin/products`).
  - Đường dẫn gian hàng `/marketplace/shops/:slug` trong Kênh người bán trở thành liên kết mở tab mới trực tiếp.
- [x] **F4.2 - Màn hình Chi tiết Đơn hàng độc lập (`OrderDetailPage.tsx` - `/orders/:id`):**
  - Màn hình tra cứu đơn mua riêng biệt theo ID: Mã đơn, Thời gian, Tên gian hàng đối tác, Trạng thái.
  - Hộp kho số bảo mật: Ẩn/Hiện mã số bản quyền đã giải mã AES, sao chép 1 chạm, tải tệp tin đính kèm bản quyền và hướng dẫn kích hoạt.
  - Tích hợp Modal gửi khiếu nại (báo mã lỗi) trong thời hạn bảo vệ 48 giờ.
  - Nút chuyển nhanh "Chi tiết đơn →" trên từng thẻ đơn hàng tại `MyOrdersPage.tsx`.
- [x] **F4.3 - Bộ Lọc Khoảng Giá & Sắp Xếp Nâng Cao (`MarketplacePage.tsx`):**
  - Thanh bộ lọc khoảng giá: Chip chọn nhanh (< 50k, 50k-200k, 200k-500k, > 500k) và nhập khoảng giá tùy chỉnh Min-Max VND.
  - Dropdown sắp xếp sản phẩm: Mới nhất (`newest`), Giá tăng dần (`price_asc`), Giá giảm dần (`price_desc`), Tên A-Z (`name`).
  - Nút "Đặt lại bộ lọc" thông minh khi có tiêu chí đang kích hoạt, phân trang (Trang trước/Trang sau).
- [x] **F4.4 - Điều hướng Thông minh từ Chuông Thông báo (`NotificationBell.tsx`) & Chia sẻ Sản phẩm:**
  - Click thông báo hệ thống tự động nhận diện loại và nội dung để điều hướng chính xác đến `/orders`, `/seller/orders`, `/seller/complaints`, `/seller/finance` hoặc `/seller/products`.
  - Nút "Chia sẻ liên kết" và liên kết trực tiếp tới Gian hàng từ `ProductDetailPage.tsx`.
  - Sửa lỗi sao chép URL bài viết trong `PostCard.tsx` (dùng permalink `/posts/:id`).

#### 🔹 Nhóm F5: Nâng Cấp Quản Trị Gian Hàng & Chỉnh Sửa Sản Phẩm (Seller Product Management & Customization)
- [x] **F5.1 - Modal Chỉnh Sửa Sản Phẩm Toàn Diện (`EditProductModal.tsx` - `/seller/products`):**
  - Bổ sung nút "Chỉnh sửa" trực tiếp trên từng thẻ sản phẩm trong tab Sản phẩm & Kho mã của Kênh người bán.
  - Modal chuyên sâu 3 tab:
    - (1) **Thông tin chung:** Cập nhật Tên sản phẩm, Danh mục phân cấp, Mô tả chi tiết qua API `PUT /api/shop/products/{id}`.
    - (2) **Bảng giá & Biến thể:** Cập nhật giá bán VND, giá gốc so sánh, mã SKU của từng biến thể hoặc thêm biến thể mới qua API `PUT /api/shop/products/{id}/variants`.
    - (3) **Bộ sưu tập hình ảnh:** Quản lý danh sách ảnh, đổi ảnh đại diện (Ảnh bìa) qua `PUT /api/shop/products/{id}/images/reorder`, xóa ảnh qua `DELETE /api/shop/products/{id}/images/{imageId}`, và tải thêm ảnh (Upload tệp từ máy tính lên Cloudinary hoặc dán liên kết URL).
- [x] **F5.2 - Nâng Cấp Quản Lý Đơn Mua & Trải Nghiệm Nhận Mã Số (`OrderDetailPage.tsx` & `MyOrdersPage.tsx`):**
  - Đồng bộ và chuẩn hóa 100% dữ liệu đơn hàng giữa Backend (`BuyerOrderDetailDto`) và Frontend: Hỗ trợ trích xuất kho mã số đã giải mã AES từ `deliveredCodes`, xử lý linh hoạt các trường doanh thu (`totalAmount` / `total`), đơn giá (`unitPrice` / `price`), và tên biến thể (`variantLabel` / `variantName`).
  - Thanh công cụ thao tác hàng loạt cho người mua: Nút **"Hiện / Ẩn tất cả mã"**, nút **"Sao chép tất cả mã"** vào clipboard một chạm, nút **"Tải tệp sao lưu (.txt)"** để lưu trữ offline dự phòng, và nút **"In biên lai"** tối ưu CSS `@media print`.
  - Đồng hồ đếm ngược bảo vệ 48 giờ trực tiếp: Hiển thị trạng thái bảo vệ hoàn tiền sống động, tính toán thời gian khiếu nại còn lại và hướng dẫn kích hoạt an toàn.
  - Bộ lọc & Tìm kiếm đơn mua tại `MyOrdersPage.tsx`: Hỗ trợ tìm kiếm realtime theo Mã đơn, Tên shop, Tên mặt hàng; bộ lọc nhanh theo tab trạng thái đơn (Tất cả, Đã giao mã, Hoàn tất, Đã hủy).
- [x] **F5.3 - Chuyển Đổi Hệ Thống Giao Diện sang Phong Cách Facebook Pure Meta (Light Mode & Rounded Corners):**
  - Chuyển đổi bảng màu toàn hệ thống (`index.css` & `marketplace.css`): Canvas xám sáng `#f0f2f5`, khối thẻ trắng thuần `#ffffff`, viền xám mềm mại `#e4e6eb`, màu nhấn xanh Meta `#1877f2`.
  - Chuẩn hóa hệ thống bo góc cong hiện đại: `--radius-md: 12px`, `--radius-lg: 16px`, `--radius-full: 9999px` cho ô tìm kiếm, chip danh mục, nút bấm và avatar.
  - Đảm bảo độ tương phản văn bản chuẩn WCAG AAA: Chữ chính `#050505`, chữ phụ `#65676b`, chữ mờ `#8a8d91`, đổ bóng ambient tự nhiên (`0 1px 2px rgba(0,0,0,0.08)`).
  - Hoàn thiện trải nghiệm trực quan cho toàn bộ Social Feed, Chợ số Marketplace, Thanh toán VietQR, Kênh Người bán và Hộp nhận mã bản quyền.
