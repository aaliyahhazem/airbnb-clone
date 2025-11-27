namespace BLL.Services.Impelementation
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        public MessageService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Response<CreateMessageVM>> CreateAsync(CreateMessageVM model, Guid senderId)
        {
            try
            {
                // find receiver by user name
                var receiver = await _uow.Users.GetByUserNameAsync(model.ReceiverUserName);
                if (receiver == null) return Response<CreateMessageVM>.FailResponse("Receiver not found");

                var entity = await _uow.Messages.CreateAsync(senderId, receiver.Id, model.Content, DateTime.UtcNow, false);
                // map back to vm (include receiver username)
                var mapped = new CreateMessageVM { ReceiverUserName = receiver.UserName ?? string.Empty, Content = entity.Content };
                return Response<CreateMessageVM>.SuccessResponse(mapped);
            }
            catch (Exception ex)
            {
                return Response<CreateMessageVM>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<GetMessageVM>>> GetConversationAsync(Guid userId1, Guid userId2)
        {
            try
            {
                var result = await _uow.Messages.GetConversationAsync(userId1, userId2);
                var mapped = _mapper.Map<List<GetMessageVM>>(result);
                return new Response<List<GetMessageVM>>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return new Response<List<GetMessageVM>>(null, ex.Message, true);
            }
        }

        public async Task<Response<List<ConversationVM>>> GetConversationsAsync(Guid userId)
        {
            try
            {
                var result = await _uow.Messages.GetConversationsAsync(userId);
                var mapped = _mapper.Map<List<ConversationVM>>(result);
                return new Response<List<ConversationVM>>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return new Response<List<ConversationVM>>(null, ex.Message, true);
            }
        }

        public async Task<DAL.Entities.User?> GetUserByUserNameAsync(string userName)
        {
            return await _uow.Users.GetByUserNameAsync(userName);
        }

        public async Task<Response<GetMessageVM>> MarkAsReadAsync(int messageId, Guid userId)
        {
            try
            {
                var msg = await _uow.Messages.MarkAsReadAsync(messageId);
                if (msg == null) return Response<GetMessageVM>.FailResponse("Message not found");
                // ensure only receiver can mark as read (or allow sender marking?)
                if (msg.ReceiverId != userId) return Response<GetMessageVM>.FailResponse("Not authorized to mark this message");
                var mapped = _mapper.Map<GetMessageVM>(msg);
                return new Response<GetMessageVM>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return Response<GetMessageVM>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<int>> GetUnreadCountAsync(Guid receiverId)
        {
            try
            {
                var count = await _uow.Messages.GetUnreadCountAsync(receiverId);
                return Response<int>.SuccessResponse(count);
            }
            catch (Exception ex)
            {
                return Response<int>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<GetMessageVM>>> GetUnreadAsync(Guid receiverId)
        {
            try
            {
                var result = await _uow.Messages.GetUnreadMessagesAsync(receiverId);
                var mapped = _mapper.Map<List<GetMessageVM>>(result);
                return new Response<List<GetMessageVM>>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return new Response<List<GetMessageVM>>(null, ex.Message, true);
            }
        }

        public async Task<Response<int>> MarkAllAsReadAsync(Guid receiverId)
        {
            try
            {
                var count = await _uow.Messages.MarkAllAsReadAsync(receiverId);
                return Response<int>.SuccessResponse(count);
            }
            catch (Exception ex)
            {
                return Response<int>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<bool>> DeleteMessageAsync(int messageId, Guid userId)
        {
            try
            {
                var msg = await _uow.Messages.GetByIdAsync(messageId);
                if (msg == null) return Response<bool>.FailResponse("Message not found");
                // allow sender or receiver or admin to delete
                if (msg.SenderId != userId && msg.ReceiverId != userId)
                    return Response<bool>.FailResponse("Not authorized to delete this message");
                var ok = await _uow.Messages.DeleteAsync(messageId);
                return ok ? Response<bool>.SuccessResponse(true) : Response<bool>.FailResponse("Failed to delete");
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse(ex.Message);
            }
        }
    }
}
