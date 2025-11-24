
using BLL.ModelVM.Payment;

namespace BLL.Services.Abstractions
{
    public interface IPaymentService
    {
        Task<Response<CreatePaymentVM>> InitiatePaymentAsync(Guid userId, int bookingId, decimal amount, string method);
        Task<Response<bool>> ConfirmPaymentAsync(int bookingId, string transactionId);
        Task<Response<bool>> RefundPaymentAsync(int paymentId);
        Task<Response<CreatePaymentIntentVm>> CreateStripePaymentIntentAsync(Guid userId, CreateStripePaymentVM model);
        Task<Response<bool>> HandleStripeWebhookAsync(string payload, string signature);
        Task<Response<bool>> CancelStripePaymentAsync(string paymentIntentId);
    }
}
