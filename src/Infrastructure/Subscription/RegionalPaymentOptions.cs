namespace Infrastructure.Subscription;

public sealed class RegionalPaymentOptions
{
    public const string SectionName = "Payments";

    public bool MockMode { get; set; } = true;
    public string PublicBaseUrl { get; set; } = "http://localhost:5173";
    public string WebhookBaseUrl { get; set; } = "http://localhost:5000";
    public ProviderOptions Click { get; set; } = new();
    public ProviderOptions Payme { get; set; } = new();
    public ProviderOptions Uzum { get; set; } = new();

    public sealed class ProviderOptions
    {
        public string BaseUrl { get; set; } = "";
        public string MerchantId { get; set; } = "";
        public string ServiceId { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string CheckoutUrl { get; set; } = "";
        public string MerchantUserId { get; set; } = "";
    }
}
