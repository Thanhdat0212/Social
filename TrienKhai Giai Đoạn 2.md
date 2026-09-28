# 1. Mục tiêu của hệ thống

Khi user mới đăng ký:

```
```

```
Register
   ↓
Chọn Interests
   ↓
Tạo User Preference ban đầu
   ↓
Home Feed
```

Nhưng Home **không phải**:

```
```

```
Technology → chỉ hiện Technology
Gaming    → chỉ hiện Gaming
```

Mà:

```
```

```
                  HOME FEED
                     │
       ┌─────────────┼─────────────┐
       ▼             ▼             ▼
 Personalization  Exploration   Discovery
      70%             20%           10%
```

Các tỷ lệ trên chỉ là **điểm khởi đầu**, không phải tỷ lệ cố định vĩnh viễn.

Mục tiêu là:

> **Ban đầu dựa vào sở thích explicit → sau đó dần chuyển trọng tâm sang hành vi thực tế của user.**

---

# 2. Kiến trúc tổng thể

Anh đề xuất:

```
```

```
                         USER
                          │
                          ▼
                    ┌───────────┐
                    │  FRONTEND │
                    └─────┬─────┘
                          │
                          ▼
                    ASP.NET API
                          │
             ┌────────────┼────────────┐
             ▼            ▼            ▼
        User/Profile     Post       Interaction
             │            │            │
             └────────────┼────────────┘
                          ▼
                Recommendation Engine
                          │
          ┌───────────────┼────────────────┐
          ▼               ▼                ▼
     Candidate         AI/Gemini       User Profile
     Generation        Analysis         & Behavior
          │               │                │
          └───────────────┼────────────────┘
                          ▼
                       Ranking
                          │
                          ▼
                     Diversification
                          │
                          ▼
                         FEED
```

---

# 3. Chia trách nhiệm Backend và AI

Đây là phần quan trọng nhất.

## Backend chịu trách nhiệm

```
```

```
Backend
├── User
├── Interest
├── Post
├── Follow
├── Like
├── Comment
├── View
├── Watch Time
├── User Interaction
├── Candidate Generation
├── Recommendation Score
├── Ranking
├── Diversity
└── Feed API
```

## AI/Gemini chịu trách nhiệm

```
```

```
Gemini
├── Hiểu nội dung Post
├── Phân loại Topic
├── Tạo Embedding
└── Semantic Similarity
```

Không nên để:

```
```

```
Frontend → Gemini → "hãy chọn 20 bài cho user"
```

vì như vậy recommendation logic bị đẩy sang AI và rất khó kiểm soát.

---

# 4. Database cần bổ sung

Anh đề xuất tối thiểu:

```
```

```
User
Interest
UserInterest

Post
PostInterest

UserInteraction

PostEmbedding
UserPreference
```

---

## `Interest`

```
```

```
Id
Name
Slug
```

Ví dụ:

```
```

```
1  Technology
2  Programming
3  AI
4  Gaming
5  Football
6  Music
```

---

## `UserInterest`

User chọn lúc onboarding:

```
```

```
UserId
InterestId
CreatedAt
```

Đây là **explicit preference**.

---

# 5. PostInterest

Một bài có thể thuộc nhiều chủ đề:

```
```

```
PostId
InterestId
Confidence
```

Ví dụ:

```
```

```
Post #100

Programming    0.95
Technology     0.82
AI             0.40
```

`Confidence` có thể là kết quả từ Gemini.

---

# 6. UserInteraction

Đây sẽ là **nguồn dữ liệu quan trọng nhất cho recommendation**.

```
```

```
UserId
PostId
InteractionType
Value
CreatedAt
```

Interaction:

```
```

```
VIEW
LIKE
COMMENT
SHARE
SAVE
FOLLOW
SKIP
NOT_INTERESTED
```

Nếu là video:

```
```

```
ViewDuration
ContentDuration
CompletionRate
```

---

# 7. UserPreference

Đây là nơi hệ thống dần xây dựng "hồ sơ sở thích".

Ví dụ:

```
```

```
User A

Programming     0.91
Technology      0.84
AI              0.78
Gaming          0.65
Football        0.12
```

Ban đầu:

```
```

```
Programming = user chọn
AI = user chọn
Gaming = user chọn
```

Sau một thời gian:

```
```

```
Programming = 0.91
AI = 0.78
Gaming = 0.65
```

Tức là:

> **Interest ban đầu chỉ là seed. UserPreference mới là thứ recommendation system thực sự dùng về sau.**

---

# 8. AI xử lý Post như thế nào?

Khi user đăng bài:

```
```

```
POST /api/posts
```

Backend:

```
```

```
PostController
       ↓
PostService
       ↓
Save Post
       ↓
GeminiService
       ↓
Analyze Content
```

Ví dụ:

> "Hôm nay mình chia sẻ cách xây dựng Clean Architecture bằng ASP.NET Core."

Gemini trả:

```
```

```
{
  "topics": [
    {
      "name": "Programming",
      "confidence": 0.97
    },
    {
      "name": "Technology",
      "confidence": 0.85
    }
  ]
}
```

Backend kiểm tra:

```
```

```
Programming có tồn tại?
Technology có tồn tại?
```

Sau đó:

```
```

```
PostInterest
```

được lưu.

---

# 9. Gemini Embedding

Tiếp theo:

```
```

```
Post Content
      ↓
Gemini Embedding
      ↓
Vector
      ↓
PostEmbedding
```

Ví dụ:

```
```

```
PostId = 100

Embedding =
[0.123, -0.552, 0.882, ...]
```

Mục đích:

> Không chỉ biết bài thuộc category nào, mà biết **ý nghĩa của bài gần với những nội dung nào**.

---

# 10. User mới vào Feed

Ví dụ user chọn:

```
```

```
Programming
AI
Gaming
```

Backend tạo candidate từ nhiều nguồn.

### Candidate Source 1 — Interest

```
```

```
Programming
AI
Gaming
```

### Candidate Source 2 — Following

Các bài từ creator user follow.

### Candidate Source 3 — Trending

Các bài đang được nhiều user tương tác.

### Candidate Source 4 — Similar Content

Gemini Embedding tìm các bài có semantic similarity cao.

### Candidate Source 5 — Exploration

Các bài user chưa từng xem.

---

# 11. Đây là điểm rất quan trọng: Candidate Generation

Giả sử database có:

```
```

```
100,000 posts
```

Không thể ranking tất cả.

Backend lấy:

```
```

```
100,000
    ↓
Interest candidates       500
Following candidates      200
Trending candidates       100
Semantic candidates       200
Exploration candidates    100
    ↓
Merge
    ↓
~1,000 candidates
```

Sau đó mới ranking.

---

# 12. Ranking Algorithm

Mỗi candidate được tính điểm.

Anh đề xuất ban đầu:

```
```

```
FinalScore =
    InterestScore
  + BehaviorScore
  + SemanticScore
  + FreshnessScore
  + PopularityScore
```

Chuẩn hóa về 0 → 100.

Sau đó:

```
```

```
FinalScore =
    0.30 × InterestScore
  + 0.30 × BehaviorScore
  + 0.20 × SemanticScore
  + 0.10 × FreshnessScore
  + 0.10 × PopularityScore
```

**Đây chỉ là initial configuration**, không hard-code trong code.

Nên đưa vào config:

```
```

```
{
  "Recommendation": {
    "InterestWeight": 0.30,
    "BehaviorWeight": 0.30,
    "SemanticWeight": 0.20,
    "FreshnessWeight": 0.10,
    "PopularityWeight": 0.10
  }
}
```

Sau này chỉnh không cần sửa code.

---

# 13. BehaviorScore mới là phần làm hệ thống "học"

Ví dụ:

```
```

```
LIKE          +5
COMMENT       +8
SHARE         +10
SAVE          +7
LONG_VIEW     +5
VIEW          +1
SKIP          -2
NOT_INTERESTED -10
```

Nhưng điểm này phải được tính **theo user**.

Ví dụ:

```
```

```
User A

Programming:
LIKE × 10
COMMENT × 4
VIEW × 20

Football:
SKIP × 15
```

Hệ thống sẽ dần học:

```
```

```
Programming ↑
Football ↓
```

---

# 14. Watch Time

Nếu app của em có video, anh sẽ đưa cái này vào từ sớm.

Ví dụ:

```
```

```
Video = 60 seconds

User xem = 58 seconds

CompletionRate = 96.7%
```

Đây là tín hiệu rất mạnh.

Ngược lại:

```
```

```
Video = 60 seconds

User xem = 2 seconds

CompletionRate = 3.3%
```

→ tín hiệu không quan tâm.

---

# 15. Feedback Loop

Đây mới là "linh hồn" của hệ thống.

```
```

```
             FEED
               │
               ▼
             USER
               │
       ┌───────┼────────┐
       ▼       ▼        ▼
      Like    View     Skip
       │       │        │
       └───────┼────────┘
               ▼
       UserInteraction
               │
               ▼
       Update Preference
               │
               ▼
       Recommendation
               │
               ▼
            New Feed
               │
               └──────────────┐
                              │
                              ▼
                             USER
```

Feed **không cố định**.

Nó thay đổi theo user.

---

# 16. Exploration — phần em yêu cầu

Anh hoàn toàn đồng ý với yêu cầu này.

Không nên:

```
```

```
User thích AI
      ↓
100% AI
```

Vì như vậy thuật toán không bao giờ học được user có thể thích cái gì mới.

Thay vào đó:

```
```

```
Personalized
    │
    ├── Programming
    ├── AI
    ├── Technology
    │
Exploration
    │
    ├── Cybersecurity
    ├── Startup
    └── Design
```

Nếu user bắt đầu tương tác với Cybersecurity:

```
```

```
Cybersecurity
      ↓
BehaviorScore ↑
      ↓
UserPreference ↑
      ↓
Cybersecurity xuất hiện nhiều hơn
```

Đây chính là **learning loop**.

---

# 17. Không nên fix cứng 70/20/10 mãi

Đây là chỗ anh muốn sửa kế hoạch trước.

Không nên code:

```
```

```
70% personalized
20% exploration
10% trending
```

rồi để vậy.

Ban đầu có thể dùng:

```
```

```
Personalized: 70%
Exploration: 20%
Trending: 10%
```

Nhưng sau này có thể điều chỉnh dựa trên:

```
```

```
User mới
User active
User ít interaction
User có nhiều interaction
```

Ví dụ:

### New User

```
```

```
Interest: 50%
Trending: 25%
Exploration: 25%
```

Vì chưa biết user.

### Established User

```
```

```
Personalized: 80%
Exploration: 15%
Trending: 5%
```

Vì đã có nhiều dữ liệu.

**Đây là adaptive recommendation**, không phải fixed feed.

---

# 18. Recommendation Service

Trong Clean Architecture:

```
```

```
Application/
└── Recommendation/
    ├── IRecommendationService.cs
    ├── RecommendationService.cs
    ├── CandidateGenerator.cs
    ├── RankingService.cs
    └── DiversityService.cs
```

Flow:

```
```

```
RecommendationService
       │
       ▼
CandidateGenerator
       │
       ├── InterestCandidate
       ├── FollowingCandidate
       ├── TrendingCandidate
       ├── SemanticCandidate
       └── ExplorationCandidate
       │
       ▼
RankingService
       │
       ▼
DiversityService
       │
       ▼
Feed
```

---

# 19. DiversityService

Cực kỳ cần.

Giả sử ranking trả:

```
```

```
1 Programming
2 Programming
3 Programming
4 Programming
5 Programming
6 Programming
```

Không tốt.

DiversityService có thể biến thành:

```
```

```
1 Programming
2 AI
3 Programming
4 Gaming
5 Technology
6 Programming
```

Không phải vì bài Programming kém hơn.

Mà vì:

> **Feed cần đa dạng.**

---

# 20. Gemini Service trong Clean Architecture

```
```

```
Infrastructure/
└── AI/
    ├── GeminiService.cs
    ├── GeminiEmbeddingService.cs
    └── GeminiOptions.cs
```

Application chỉ biết:

```
```

```
IAiContentAnalyzer
```

và:

```
```

```
IEmbeddingService
```

Application không biết Gemini API cụ thể.

Đây là đúng tinh thần Clean Architecture.

---

# 21. API cần triển khai

## Onboarding

```
```

```
GET /api/interests
```

```
```

```
POST /api/users/me/interests
```

```
```

```
GET /api/users/me/preferences
```

---

## Posts

```
```

```
POST /api/posts
GET /api/posts/{id}
```

Khi POST:

```
```

```
Save Post
 ↓
Gemini Analyze
 ↓
PostInterest
 ↓
Embedding
 ↓
PostEmbedding
```

---

## Interaction

```
```

```
POST /api/posts/{id}/view
POST /api/posts/{id}/like
POST /api/posts/{id}/save
POST /api/posts/{id}/share
POST /api/posts/{id}/not-interested
```

Video:

```
```

```
POST /api/posts/{id}/watch
```

Body:

```
```

```
{
  "duration": 52,
  "completionRate": 0.86
}
```

---

# 22. Feed API

Frontend chỉ cần:

```
```

```
GET /api/feed?page=1&pageSize=20
```

Backend tự quyết định:

```
```

```
User
 ↓
Candidate Generation
 ↓
Ranking
 ↓
Diversity
 ↓
20 posts
```

Frontend **không cần biết thuật toán**.

---

# 23. Database flow

```
```

```
                    USER
                     │
          ┌──────────┴──────────┐
          ▼                     ▼
    UserInterest          UserInteraction
          │                     │
          └──────────┬──────────┘
                     ▼
               UserPreference
                     │
                     │
POST ────────────────┤
 │                   │
 ▼                   ▼
PostInterest      PostEmbedding
 │                   │
 └──────────┬────────┘
            ▼
    Recommendation Engine
            │
            ▼
           Feed
```

---

# 24. Plan triển khai thực tế

Anh sẽ chia thành **6 phase**.

### Phase 1 — Interest & Onboarding

```
```

```
Interest
UserInterest
Onboarding UI
Profile completion
```

Mục tiêu:

```
```

```
Register
→ Select interests
→ Home
```

---

### Phase 2 — Content Classification

Tích hợp Gemini:

```
```

```
Create Post
    ↓
Gemini
    ↓
Topics
    ↓
PostInterest
```

Mục tiêu:

> Không cần creator tự tag bài.

---

### Phase 3 — Interaction Tracking

```
```

```
Like
Comment
Share
View
Save
Follow
Skip
Not Interested
Watch Time
```

Mục tiêu:

> Thu thập dữ liệu để recommendation học.

---

### Phase 4 — Recommendation Engine

```
```

```
Candidate Generation
       ↓
Scoring
       ↓
Ranking
       ↓
Diversity
       ↓
Feed
```

Mục tiêu:

> Feed không còn fixed.

---

### Phase 5 — Gemini Embedding

```
```

```
Post
 ↓
Embedding
 ↓
Semantic Search
 ↓
Similar Content
 ↓
Recommendation
```

Mục tiêu:

> Recommendation hiểu **ý nghĩa nội dung**, không chỉ category.

---

### Phase 6 — Adaptive Recommendation

Theo dõi:

```
```

```
CTR
Watch Time
Completion Rate
Like Rate
Skip Rate
Not Interested Rate
```

Sau đó điều chỉnh:

```
```

```
Weights
Candidate ratio
Exploration ratio
```

Mục tiêu:

> Hệ thống thực sự **học từ user**, thay vì chỉ chạy một bộ rule cố định.

---

# 25. Còn Machine Learning?

**Chưa làm trong 6 phase đầu.**

Đầu tiên:

```
```

```
Gemini
+
Rule-based ranking
+
Behavior data
```

Sau khi có đủ dữ liệu:

```
```

```
Interactions
       ↓
Training Dataset
       ↓
ML Recommendation Model
       ↓
Predict engagement
       ↓
Ranking
```

Khi đó kiến trúc sẽ chuyển từ:

```
```

```
Rule-based Ranking
```

sang:

```
```

```
ML Ranking
```

hoặc tốt hơn:

```
```

```
Hybrid Ranking

Rule
+
ML
+
Semantic Similarity
```

---

# 26. Một vấn đề em cần đặc biệt lưu ý

**Đừng gọi Gemini mỗi lần user mở `/feed`.**

Ví dụ:

```
```

```
100 users
×
10 requests/minute
=
1,000 Gemini requests/minute
```

Rất lãng phí và latency cao.

Gemini nên được dùng chủ yếu khi:

```
```

```
Post được tạo
       ↓
Analyze một lần
       ↓
Lưu kết quả
```

và:

```
```

```
Post được tạo
       ↓
Embedding một lần
       ↓
Lưu vector
```

Recommendation đọc dữ liệu đã xử lý.

---

# 27. Kiến trúc cuối cùng anh đề xuất

```
```

```
                         ┌──────────────┐
                         │   FRONTEND   │
                         └──────┬───────┘
                                │
                                ▼
                         ┌──────────────┐
                         │ ASP.NET API  │
                         └──────┬───────┘
                                │
              ┌─────────────────┼─────────────────┐
              │                 │                 │
              ▼                 ▼                 ▼
           AUTH             POSTS           INTERACTION
              │                 │                 │
              │                 ▼                 │
              │             Gemini                │
              │                 │                 │
              │        ┌────────┴────────┐        │
              │        ▼                 ▼        │
              │     Topics           Embedding    │
              │        │                 │        │
              │        ▼                 ▼        │
              │   PostInterest     PostEmbedding  │
              │        │                 │        │
              └────────┼─────────────────┼────────┘
                       │                 │
                       ▼                 ▼
                 UserPreference   UserInteraction
                       │                 │
                       └────────┬────────┘
                                ▼
                    ┌──────────────────────┐
                    │ Recommendation Engine│
                    ├──────────────────────┤
                    │ Candidate Generation │
                    │ Scoring              │
                    │ Ranking              │
                    │ Diversity            │
                    │ Exploration          │
                    └──────────┬───────────┘
                               │
                               ▼
                              FEED
```