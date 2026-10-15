    namespace Domain.Enums;

    public enum ProductType
    {
        Digital = 1,
        Service = 2
    }

    public enum CategoryModerationPolicy
    {
        DirectListing = 1,
        RequiresReview = 2
    }

    public enum AttributeDataType
    {
        Enum = 1,
        Number = 2,
        Text = 3,
        Boolean = 4
    }

    public enum AttributeRequirement
    {
        Required = 1,
        Recommended = 2,
        Optional = 3
    }

    public enum CategoryRequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
