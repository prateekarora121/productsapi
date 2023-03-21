namespace aws.lambda.product.api.DTO
{
    public class Response
    {
        public Response()
        {

        }
        public Boolean IsSuccess { get; set; }
        public object Data { get; set; }
        public string ErrorMessage { get; set; }

        public Response(Boolean IsSuccess, Object Data, string ErrorMessage)
        {
            this.IsSuccess = IsSuccess;
            this.Data = Data;
            this.ErrorMessage = ErrorMessage;
        }
    }
}
