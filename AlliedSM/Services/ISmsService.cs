namespace AlliedSM.Services
{
    // Abstraction so the rest of the app never talks to a specific SMS provider directly.
    // Swap FakeSmsService for a real Twilio/local-provider implementation once that
    // paid account is set up — nothing else in the app needs to change.
    public interface ISmsService
    {
        Task SendAsync(string mobileNumber, string message);
    }

    // Default implementation: just logs. Replace registration in Program.cs with a real
    // provider (e.g. TwilioSmsService) when the paid SMS/WhatsApp account is ready.
    public class FakeSmsService : ISmsService
    {
        private readonly ILogger<FakeSmsService> _logger;

        public FakeSmsService(ILogger<FakeSmsService> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string mobileNumber, string message)
        {
            _logger.LogInformation("[SMS-STUB] To {Mobile}: {Message}", mobileNumber, message);
            return Task.CompletedTask;
        }
    }
}
