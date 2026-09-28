using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class BulkPostSeeder
{
    private static readonly (Guid Id, string Name, string Slug, string Icon)[] Interests =
    {
        (Guid.Parse("00000000-0000-0000-0000-000000000101"), "Công nghệ", "technology", "💻"),
        (Guid.Parse("00000000-0000-0000-0000-000000000102"), "Lập trình", "programming", "👨‍💻"),
        (Guid.Parse("00000000-0000-0000-0000-000000000103"), "Trí tuệ nhân tạo (AI)", "ai", "🤖"),
        (Guid.Parse("00000000-0000-0000-0000-000000000104"), "Trò chơi (Gaming)", "gaming", "🎮"),
        (Guid.Parse("00000000-0000-0000-0000-000000000105"), "Thể thao & Bóng đá", "sports", "⚽"),
        (Guid.Parse("00000000-0000-0000-0000-000000000106"), "Âm nhạc", "music", "🎵"),
        (Guid.Parse("00000000-0000-0000-0000-000000000107"), "Phim ảnh & Điện ảnh", "movies", "🎬"),
        (Guid.Parse("00000000-0000-0000-0000-000000000108"), "Thiết kế & Nghệ thuật", "design", "🎨"),
        (Guid.Parse("00000000-0000-0000-0000-000000000109"), "Kinh doanh & Khởi nghiệp", "business", "💼"),
        (Guid.Parse("00000000-0000-0000-0000-000000000110"), "Khoa học & Vũ trụ", "science", "🔬"),
        (Guid.Parse("00000000-0000-0000-0000-000000000111"), "Du lịch & Trải nghiệm", "travel", "✈️"),
        (Guid.Parse("00000000-0000-0000-0000-000000000112"), "Ẩm thực & Nấu ăn", "food", "🍜"),
        (Guid.Parse("00000000-0000-0000-0000-000000000113"), "Sức khỏe & Fitness", "fitness", "🏋️"),
        (Guid.Parse("00000000-0000-0000-0000-000000000114"), "Nhiếp ảnh", "photography", "📷"),
        (Guid.Parse("00000000-0000-0000-0000-000000000115"), "Anime & Manga", "anime", "⛩️"),
        (Guid.Parse("00000000-0000-0000-0000-000000000116"), "Sách & Tri thức", "books", "📚")
    };

    private static readonly Dictionary<string, string[]> ImagePool = new()
    {
        ["tech"] = new[]
        {
            "https://images.unsplash.com/photo-1518770660439-4636190af475?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1519389950473-47ba0277781c?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=1000&auto=format&fit=crop&q=80"
        },
        ["code"] = new[]
        {
            "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1607799279861-4dd421887fb3?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1542838132-92c53300491e?w=1000&auto=format&fit=crop&q=80"
        },
        ["ai"] = new[]
        {
            "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1620712943543-bcc4688e7485?w=1000&auto=format&fit=crop&q=80"
        },
        ["gaming"] = new[]
        {
            "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1598550476439-6847785fcea6?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1000&auto=format&fit=crop&q=80"
        },
        ["sports"] = new[]
        {
            "https://images.unsplash.com/photo-1508098682722-e99c43a406b2?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1452626038306-9aae5e071dd3?w=1000&auto=format&fit=crop&q=80"
        },
        ["music"] = new[]
        {
            "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1520523839898-507125cd53c1?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=1000&auto=format&fit=crop&q=80"
        },
        ["movies"] = new[]
        {
            "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=1000&auto=format&fit=crop&q=80"
        },
        ["design"] = new[]
        {
            "https://images.unsplash.com/photo-1507238691740-187a5b1d37b8?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1581291518857-4e27b48ff24e?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1561070791-2526d30994b5?w=1000&auto=format&fit=crop&q=80"
        },
        ["business"] = new[]
        {
            "https://images.unsplash.com/photo-1556761175-5973dc0f32e7?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?w=1000&auto=format&fit=crop&q=80"
        },
        ["science"] = new[]
        {
            "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=1000&auto=format&fit=crop&q=80"
        },
        ["travel"] = new[]
        {
            "https://images.unsplash.com/photo-1528127269322-539801943592?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=1000&auto=format&fit=crop&q=80"
        },
        ["food"] = new[]
        {
            "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=1000&auto=format&fit=crop&q=80"
        },
        ["fitness"] = new[]
        {
            "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=1000&auto=format&fit=crop&q=80"
        },
        ["photography"] = new[]
        {
            "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?w=1000&auto=format&fit=crop&q=80"
        },
        ["anime"] = new[]
        {
            "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?w=1000&auto=format&fit=crop&q=80"
        },
        ["books"] = new[]
        {
            "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=1000&auto=format&fit=crop&q=80",
            "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=1000&auto=format&fit=crop&q=80"
        }
    };

    private static readonly Dictionary<string, (string[] Templates, string ImageKey)> TopicTemplates = new()
    {
        ["Công nghệ"] = (new[]
        {
            "💻 Đánh giá công nghệ tuần qua: Xu hướng thiết bị chip ARM và màn hình gập đang thực sự định hình lại trải nghiệm người dùng. Tốc độ xử lý tăng gấp đôi trong khi nhiệt lượng và điện năng tiêu thụ giảm đáng kể.",
            "🚀 Các tính năng phần cứng mới nhất năm nay cho thấy trọng tâm đã chuyển dịch từ nâng cấp camera sang tối ưu hóa NPU chuyên dụng cho trí tuệ nhân tạo.",
            "⚡ Cloud Native và hạ tầng Serverless tiếp tục là xu hướng không thể đảo ngược. Giúp các đội ngũ công nghệ giảm thiểu tới 60% chi phí vận hành máy chủ truyền thống.",
            "📱 Một chiếc laptop hoàn hảo cho năm 2026: Pin 18 tiếng, màn hình OLED chống lóa, bàn phím gõ êm và cổng kết nối Thunderbolt 5 tốc độ cao. Anh em ưu tiên tiêu chí nào nhất?",
            "🔒 Bảo mật dữ liệu cá nhân trong kỷ nguyên số: 3 thói quen đơn giản giúp bạn bảo vệ tài khoản mạng xã hội trước các cuộc tấn công phishing tinh vi."
        }, "tech"),

        ["Lập trình"] = (new[]
        {
            "👨‍💻 5 nguyên lý Clean Code giúp bạn viết mã nguồn dễ đọc, dễ bảo trì và giảm thiểu tối đa bug tiềm ẩn trong tương lai. Code cho người đọc trước, cho máy chạy sau!",
            "🔥 So sánh hiệu năng giữa Rust và Go trong việc xử lý hàng triệu kết nối mạng thời gian thực: Điểm mạnh của từng ngôn ngữ và bài học khi thiết kế hệ thống phân tán.",
            "🛠️ Tips tối ưu hóa truy vấn Database với Entity Framework Core và PostgreSQL: Đừng quên index đúng cột và luôn sử dụng AsNoTracking cho các câu lệnh đọc dữ liệu!",
            "💡 Debugging không chỉ là tìm lỗi, mà là quá trình thấu hiểu cách máy tính thực thi từng dòng suy nghĩ của bạn. Mỗi khi fix xong một bug hóc búa là một lần trình độ nâng tầm.",
            "🌐 Kiến trúc Microservices vs Modular Monolith: Khi nào bạn thực sự cần chia nhỏ hệ thống và cái giá phải trả cho độ phức tạp vận hành."
        }, "code"),

        ["Trí tuệ nhân tạo (AI)"] = (new[]
        {
            "🤖 Tổng quan về kiến trúc RAG (Retrieval-Augmented Generation): Cách kết hợp Vector Database và mô hình ngôn ngữ lớn để trả lời câu hỏi chuyên sâu không bị ảo giác.",
            "🧠 Fine-tuning mô hình ngôn ngữ mã nguồn mở: Trải nghiệm huấn luyện mô hình 8B tham số phục vụ riêng cho ngôn ngữ tiếng Việt chuyên ngành y tế và pháp luật.",
            "✨ AI đang chuyển từ 'chat tương tác' sang 'Agent tự hành' (Autonomous Agent). Chúng có thể tự lên kế hoạch, gọi API, kiểm tra kết quả và tự sửa sai khi gặp lỗi.",
            "📊 Thị giác máy tính (Computer Vision) thế hệ mới: Ứng dụng phân tích hình ảnh vệ tinh để cảnh báo sớm sạt lở đất và bão lũ tại các vùng đồi núi Việt Nam.",
            "🔮 Tương lai của lập trình viên trong kỷ nguyên AI: AI không cướp việc của bạn, mà người lập trình viên biết tận dụng AI sẽ thay thế người không dùng nó."
        }, "ai"),

        ["Trò chơi (Gaming)"] = (new[]
        {
            "🎮 Trải nghiệm tựa game thế giới mở mới nhất: Thế giới rộng lớn với hàng trăm nhiệm vụ phụ được lồng ghép cốt truyện sâu sắc, không hề có cảm giác nhàm chán!",
            "🏆 Esports Việt Nam đang ngày càng khẳng định vị thế trên đấu trường quốc tế. Tinh thần kỷ luật, chiến thuật bài bản và tâm lý thi đấu kiên cường.",
            "🕹️ Hoài niệm tuổi thơ với những tựa game 8-bit và 16-bit kinh điển. Đôi khi đồ họa đơn sơ nhưng gameplay cuốn hút lại mang lại cảm xúc khó quên nhất.",
            "🔥 Review tay cầm chơi game công thái học: Phản hồi rung haptic chân thực, cần analog chống trôi Hall Effect và độ trễ gần như bằng không qua kết nối không dây 2.4GHz.",
            "👾 Boss fight đáng nhớ nhất trong lịch sử chơi game của bạn là gì? Cảm giác thử lại hơn 30 lần và cuối cùng hạ gục boss trong gang tấc thật khó tả!"
        }, "gaming"),

        ["Thể thao & Bóng đá"] = (new[]
        {
            "⚽ Trận cầu tâm điểm rạng sáng nay: Cú lội ngược dòng ngoạn mục ở phút bù giờ thứ 94! Chiến thuật pressing tầm cao và sự thay người chuẩn xác đã xoay chuyển hoàn toàn thế trận.",
            "🏃‍♂️ Chạy bộ buổi sáng tại công viên: Hít thở không khí trong lành, kích hoạt nguồn năng lượng tích cực cho cả ngày dài làm việc năng suất.",
            "🏸 Cầu lông phong trào ngày càng phát triển mạnh mẽ. Bộ môn rèn luyện phản xạ nhanh, sức bền tim mạch và tạo không gian giao lưu gắn kết tuyệt vời.",
            "🎾 Tinh thần thể thao đích thực: Không chỉ là việc giành chiến thắng, mà là cách chúng ta tôn trọng đối thủ và không ngừng vượt qua giới hạn của chính mình.",
            "🚴 Thử thách đạp xe 50km cuối tuần qua những cung đường ngoại thành xanh mát. Cảm giác gió lướt qua tai và ngắm nhìn cảnh sắc đồng quê yên ả."
        }, "sports"),

        ["Âm nhạc"] = (new[]
        {
            "🎵 Những giai điệu Lo-fi êm dịu cho một buổi chiều mưa làm việc tập trung. Âm thanh mộc mạc của tiếng mưa rơi kết hợp cùng tiếng đàn Rhodes ấm áp.",
            "🎸 Tự tập một bản nhạc guitar cổ điển: Đôi ngón tay chai sạn nhưng khi từng nốt nhạc ngân vang tròn trịa, cảm xúc tràn ngập niềm vui thuần khiết.",
            "🎧 Khám phá làn sóng âm nhạc Indie Việt: Những góc nhìn đời thường, ca từ mộc mạc giàu chất thơ và giai điệu bắt tai đến từ các nghệ sĩ trẻ độc lập.",
            "🎹 Âm nhạc có khả năng chữa lành những vết thương tâm hồn mà ngôn từ không thể diễn tả. Hãy dành cho mình 30 phút mỗi ngày để thả hồn vào những nốt nhạc.",
            "🥁 Buổi hòa nhạc trực tiếp (Live Concert) cuối tuần qua thật bùng nổ! Năng lượng cuồng nhiệt từ hàng ngàn khán giả cùng hòa chung một điệu hát."
        }, "music"),

        ["Phim ảnh & Điện ảnh"] = (new[]
        {
            "🎬 Phân tích cú máy one-shot dài 6 phút trong bộ phim đoạt giải Oscar: Sự kết hợp kỳ công giữa diễn xuất, ánh sáng và chuyển động máy quay không một vết cắt.",
            "🍿 Top 3 bộ phim trinh thám với kịch bản plot twist nghẹt thở: Bạn sẽ không thể đoán được hung thủ thực sự cho đến 10 phút cuối cùng của tác phẩm!",
            "📽️ Nghệ thuật sử dụng màu sắc trong điện ảnh: Tông màu ấm biểu thị nỗi nhớ và hoài niệm, trong khi sắc xanh lạnh lùng thể hiện sự cô đơn lạc lõng của nhân vật.",
            "🎞️ Phim tài liệu về thiên nhiên hoang dã: Những thước phim 4K siêu chậm ghi lại khoảnh khắc săn mồi ngoạn mục dưới đáy đại dương sâu thẳm.",
            "🎭 Diễn xuất đỉnh cao là khi diễn viên không cần thoại, chỉ qua một ánh mắt nhìn nghiêng cũng đủ để khán giả cảm nhận được nỗi đau giằng xé bên trong."
        }, "movies"),

        ["Thiết kế & Nghệ thuật"] = (new[]
        {
            "🎨 Nguyên tắc phối màu 60-30-10 trong thiết kế nội thất và giao diện người dùng: Giúp tổng thể bố cục luôn cân bằng, hài hòa và có điểm nhấn thị giác rõ ràng.",
            "📐 Nghệ thuật Typography: Lựa chọn font chữ có chân (Serif) hay không chân (Sans-serif) ảnh hưởng trực tiếp đến cá tính và thông điệp mà thương hiệu muốn truyền tải.",
            "✨ Phong cách tối giản (Minimalism): Loại bỏ những chi tiết rườm rà để tôn vinh vẻ đẹp của khoảng trống và những đường nét cốt lõi tinh tế nhất.",
            "🖌️ Vẽ ký họa đường phố mỗi sáng chủ nhật: Một cuốn sổ tay, cây bút mực đen và góc nhìn quan sát đời sống mộc mạc xung quanh bạn.",
            "🖼️ Trải nghiệm triển lãm nghệ thuật thị giác đương đại: Những tác phẩm sắp đặt ánh sáng đa chiều mang lại nhiều suy ngẫm sâu sắc về mối quan hệ giữa con người và thiên nhiên."
        }, "design"),

        ["Kinh doanh & Khởi nghiệp"] = (new[]
        {
            "💼 3 chỉ số tài chính quan trọng nhất mà mọi nhà sáng lập startup cần theo dõi hàng tuần: Burn Rate, CAC (Chi phí thu hút khách hàng) và LTV (Giá trị vòng đời khách hàng).",
            "📈 Bí quyết xây dựng mối quan hệ đối tác bền vững: Luôn đặt lợi ích đôi bên cùng có lợi (Win-Win) lên hàng đầu và thực hiện cam kết đúng hẹn.",
            "🎯 Nghệ thuật đàm phán trong kinh doanh: Lắng nghe nhiều hơn nói, thấu hiểu động cơ sâu xa của đối phương trước khi đưa ra đề xuất giải pháp hợp tác.",
            "🚀 Khởi nghiệp tinh gọn (Lean Startup): Đưa sản phẩm MVP ra thị trường sớm nhất có thể để kiểm chứng giả thuyết bằng phản hồi thực tế từ người dùng trả phí.",
            "👥 Quản trị nhân sự: Lãnh đạo không phải là người ra lệnh, mà là người dọn đường và truyền cảm hứng để đội ngũ phát huy tối đa tiềm năng sáng tạo."
        }, "business"),

        ["Khoa học & Vũ trụ"] = (new[]
        {
            "🔬 Khám phá mới về năng lượng nhiệt hạch (Fusion Energy): Bước tiến mang tính lịch sử đưa nhân loại tiến gần hơn tới nguồn năng lượng sạch vô tận trong tương lai.",
            "🌌 Bức xạ tàn dư vũ trụ (CMB): Dấu vết ánh sáng cổ xưa nhất còn sót lại từ thời điểm 380.000 năm sau Big Bang đang kể cho chúng ta câu chuyện về nguồn gốc vũ trụ.",
            "🪐 Những phát hiện chấn động về đại dương ngầm dưới lớp băng dày của mặt trăng Europa: Nơi tiềm năng nhất trong hệ Mặt Trời có thể tồn tại sự sống vi sinh ngoài Trái Đất.",
            "🧬 Công nghệ chỉnh sửa gen CRISPR và y học cá nhân hóa: Mở ra cơ hội chữa dứt điểm các bệnh di truyền nan y từng là nỗi ám ảnh của nhân loại.",
            "⚡ Thuyết tương đối rộng của Einstein: Thời gian thực sự trôi chậm hơn ở những nơi có trường trọng lực cực mạnh như gần rìa lỗ đen siêu khối lượng."
        }, "science"),

        ["Du lịch & Trải nghiệm"] = (new[]
        {
            "✈️ Khám phá cung đường ven biển đẹp nhất miền Trung: Một bên là vách núi dựng đứng, một bên là sóng biển xanh biếc vỗ bờ cát trắng mịn.",
            "🏕️ Cắm trại qua đêm trên đồi thông Đà Lạt: Thưởng thức ly trà nóng bên bếp lửa bập bùng, ngắm nhìn thung lũng đèn lồng lung linh trong sương sớm.",
            "🎒 Kinh nghiệm du lịch tự túc tiết kiệm mà trọn vẹn: Đặt vé trước 2 tháng, săn homestay bản địa và học vài câu chào hỏi bằng tiếng địa phương thân thiện.",
            "🗺️ Hành trình trekking chinh phục đỉnh Fansipan 3.143m: Vượt qua những đoạn dốc đứng trong rừng trúc bạt ngàn để chạm tay vào cột mốc nóc nhà Đông Dương.",
            "🌅 Hoàng hôn trên đảo Phú Quốc: Bầu trời nhuộm sắc cam hồng rực rỡ, mặt trời tròn xoe từ từ chìm xuống làn nước biển êm đềm như một giấc mơ."
        }, "travel"),

        ["Ẩm thực & Nấu ăn"] = (new[]
        {
            "🍜 Hướng dẫn làm món Bún chả Hà Nội nướng than hoa chuẩn vị: Thịt ba chỉ ướp nước hàng, hành tỏi và nước mắm cốt đậm đà, nướng xèo xèo thơm nức mũi.",
            "🍳 Bữa sáng giàu dinh dưỡng chỉ mất 10 phút chuẩn bị: Bánh mì nướng ăn kèm quả bơ tươi nghiền, trứng ốp la lòng đào và rắc thêm chút tiêu đen thơm lừng.",
            "☕ Nghệ thuật thưởng thức cà phê Pour-over: Từng giọt cà phê nhỏ giọt chiết xuất trọn vẹn hương vị hoa quả nhiệt đới thanh tao và hậu vị ngọt sâu lắng.",
            "🥘 Canh chua cá lóc miền Tây: Vị chua dịu của me chín, vị ngọt thanh của cá đồng tươi ngon và mùi thơm thoang thoảng của rau ngò gai, bắp chuối.",
            "🍰 Tự tay làm bánh bông lan trứng muối mềm xốp tại nhà: Lớp sốt phô mai béo ngậy kết hợp cùng vị mặn bùi của trứng muối tạo nên hương vị khó cưỡng."
        }, "food"),

        ["Sức khỏe & Fitness"] = (new[]
        {
            "🏋️‍♂️ 4 bài tập Compound cốt lõi cho người mới bắt đầu: Squat, Bench Press, Deadlift và Overhead Press. Giúp xây dựng nền tảng sức mạnh toàn diện nhất.",
            "🥗 Chế độ ăn Eat Clean không hề nhàm chán: Ưu tiên thực phẩm nguyên bản, cân bằng tỷ lệ Carbs phức tạp, Protein nạc và chất béo tốt từ các loại hạt.",
            "💤 Giấc ngủ sâu là chìa khóa của sự phục hồi: Tắt màn hình điện thoại 1 tiếng trước khi ngủ và giữ phòng ngủ mát mẻ để cải thiện chất lượng giấc ngủ tối đa.",
            "💧 Tầm quan trọng của việc bù nước và điện giải trong khi tập luyện thể thao cường độ cao. Uống từng ngụm nhỏ đều đặn thay vì uống ừng ực khi đã quá khát.",
            "🧘 Tư thế ngồi làm việc công thái học: Điều chỉnh độ cao ghế sao cho khuỷu tay vuông góc với bàn làm việc, giảm 80% áp lực lên cột sống cổ và vai gáy."
        }, "fitness"),

        ["Nhiếp ảnh"] = (new[]
        {
            "📷 Làm chủ ánh sáng tự nhiên trong nhiếp ảnh chân dung: Tận dụng khung giờ vàng (Golden Hour) lúc sáng sớm hoặc hoàng hôn để có làn da mịn màng rạng rỡ.",
            "📸 Quy tắc một phần ba (Rule of Thirds) và đường dẫn thị giác: Đặt chủ thể vào các điểm giao nhau để bức ảnh có chiều sâu và thu hút ánh nhìn hơn.",
            "🎞️ Nhiếp ảnh phim Analog và cảm xúc hoài niệm: Tiếng lên phim cơ học, hạt grain cổ điển và sự hồi hộp chờ đợi ngày tráng rửa cuộn phim đầu tay.",
            "🌆 Kỹ thuật chụp ảnh phong cảnh đêm (Long Exposure): Phơi sáng 30 giây để biến dòng xe cộ tấp nập thành những dải lụa ánh sáng uốn lượn tuyệt đẹp.",
            "🔍 Chụp ảnh Macro cận cảnh thế giới vi mô: Những giọt sương sớm đọng trên cánh hoa hồng lấp lánh như những viên ngọc pha lê tinh khiết."
        }, "photography"),

        ["Anime & Manga"] = (new[]
        {
            "⛩️ Phân tích triết lý nhân sinh trong các tác phẩm của Hayao Miyazaki (Studio Ghibli): Sự hòa hợp giữa con người với tự nhiên và vẻ đẹp của sự giản dị.",
            "📖 Những bộ manga kinh điển đã dạy chúng ta về tình bạn, lòng dũng cảm và tinh thần không bao giờ từ bỏ ước mơ dù nghịch cảnh có khắc nghiệt đến đâu.",
            "🔥 Sự trỗi dậy mạnh mẽ của anime điện ảnh toàn cầu: Doanh thu phòng vé kỷ lục và sự công nhận ngày càng lớn từ giới phê bình nghệ thuật hàn lâm.",
            "🌸 Văn hóa Cosplay và cộng đồng fan hâm mộ: Nơi những người trẻ thỏa sức sáng tạo, hóa thân thành những nhân vật mình yêu thích với niềm đam mê bất tận.",
            "🎨 Nét vẽ tay truyền thống kết hợp cùng kỹ xảo CGI hiện đại: Xu hướng làm phim hoạt hình tương lai giữ trọn linh hồn tác phẩm mà vẫn mãn nhãn thị giác."
        }, "anime"),

        ["Sách & Tri thức"] = (new[]
        {
            "📚 Cuốn sách 'Atomic Habits' (Thói quen nguyên tử) và bài học về sự tiến bộ 1% mỗi ngày: Thay đổi nhỏ, kết quả phi thường khi được duy trì đủ lâu.",
            "📖 Xây dựng thói quen đọc sách 20 trang mỗi sáng: Sau một năm bạn sẽ đọc xong hơn 30 cuốn sách giá trị và tích lũy được khối lượng tri thức khổng lồ.",
            "💡 'Tư duy nhanh và chậm' của Daniel Kahneman: Hiểu rõ hai hệ thống tư duy trong não bộ để tránh các bẫy tâm lý sai lầm khi đưa ra quyết định quan trọng.",
            "☕ Một buổi sáng cuối tuần bên ly cà phê thơm và cuốn sách yêu thích: Không gian tĩnh lặng để nhìn nhận lại chặng đường đã qua và nạp đầy năng lượng mới.",
            "📝 Phương pháp ghi chép Zettelkasten: Cách các nhà khoa học và tư tưởng vĩ đại kết nối các ý tưởng rời rạc thành một mạng lưới tri thức sống động."
        }, "books")
    };

    public static async Task<int> SeedBulkPostsAsync(
        SocialDbContext context,
        ILogger logger,
        int countToGenerate = 1000,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bắt đầu sinh và nạp hàng loạt {Count} bài viết vào database...", countToGenerate);

        // 1. Lấy danh sách tác giả hiện có
        var authorIds = await context.Users.Select(u => u.Id).ToListAsync(cancellationToken);
        if (authorIds.Count == 0)
        {
            logger.LogWarning("Chưa có User nào trong database! Vui lòng nạp User trước.");
            return 0;
        }

        var random = new Random(42); // Seed cố định để dữ liệu đẹp và có thể tái hiện
        var now = DateTime.UtcNow;

        var batchSize = 100;
        var totalInserted = 0;
        var postList = new List<Post>();
        var interestList = new List<PostInterest>();

        var topicKeys = TopicTemplates.Keys.ToList();

        for (int i = 0; i < countToGenerate; i++)
        {
            var postId = Guid.NewGuid();
            var authorId = authorIds[random.Next(authorIds.Count)];

            // Chọn ngẫu nhiên 1 chủ đề chính
            var primaryTopicName = topicKeys[random.Next(topicKeys.Count)];
            var (templates, imgKey) = TopicTemplates[primaryTopicName];
            var template = templates[random.Next(templates.Length)];

            // Tạo biến thể nội dung độc nhất
            var contentVariation = $"{template}\n\n#SocialPlatform #{primaryTopicName.Replace(" ", "").Replace("&", "").Replace("(", "").Replace(")", "")} #ChiaSe{i + 1}";

            // 70% có ảnh, 30% bài viết chỉ có chữ
            var mediaUrls = new List<string>();
            if (random.NextDouble() < 0.70 && ImagePool.TryGetValue(imgKey, out var images) && images.Length > 0)
            {
                var imgCount = random.Next(1, 3);
                for (int m = 0; m < imgCount; m++)
                {
                    mediaUrls.Add(images[random.Next(images.Length)]);
                }
            }

            // Phân bổ thời gian ngẫu nhiên từ 45 ngày trước đến 5 phút trước
            var minutesAgo = random.Next(5, 45 * 24 * 60);
            var createdAt = now.AddMinutes(-minutesAgo);

            var likes = random.Next(5, 450);
            var comments = random.Next(0, 35);
            var views = random.Next(likes * 2, likes * 10 + 100);

            var post = new Post
            {
                Id = postId,
                AuthorId = authorId,
                Content = contentVariation,
                MediaUrls = mediaUrls,
                Status = PostStatus.Published,
                LikeCount = likes,
                CommentCount = comments,
                ViewCount = views,
                CreatedAtUtc = createdAt,
                UpdatedAtUtc = createdAt
            };
            postList.Add(post);

            // Gắn 1 - 2 chủ đề phù hợp
            var primaryInterest = Interests.First(x => x.Name == primaryTopicName);
            interestList.Add(new PostInterest
            {
                PostId = postId,
                InterestId = primaryInterest.Id,
                Confidence = Math.Round(0.85 + random.NextDouble() * 0.14, 2),
                CreatedAtUtc = createdAt
            });

            // 40% có thêm chủ đề phụ liên quan
            if (random.NextDouble() < 0.40)
            {
                var secondaryInterest = Interests[random.Next(Interests.Length)];
                if (secondaryInterest.Id != primaryInterest.Id)
                {
                    interestList.Add(new PostInterest
                    {
                        PostId = postId,
                        InterestId = secondaryInterest.Id,
                        Confidence = Math.Round(0.70 + random.NextDouble() * 0.15, 2),
                        CreatedAtUtc = createdAt
                    });
                }
            }

            // Lưu theo từng batch 100 bài
            if (postList.Count >= batchSize || i == countToGenerate - 1)
            {
                context.Posts.AddRange(postList);
                context.PostInterests.AddRange(interestList);
                await context.SaveChangesAsync(cancellationToken);

                totalInserted += postList.Count;
                logger.LogInformation("Đã lưu batch: {Current}/{Total} bài viết...", totalInserted, countToGenerate);

                postList.Clear();
                interestList.Clear();
            }
        }

        logger.LogInformation("Hoàn tất nạp thành công {Count} bài viết vào database!", totalInserted);
        return totalInserted;
    }
}
