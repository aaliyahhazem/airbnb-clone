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
                var entity = await _uow.Messages.CreateAsync(senderId, model.ReceiverId, model.Content, DateTime.UtcNow, false);
                // map back to vm
                var mapped = _mapper.Map<CreateMessageVM>(entity);
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
    }
}
