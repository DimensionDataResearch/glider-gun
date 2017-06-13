namespace DD.Research.GliderGun.Api.Models
{
    public class ErrorResponse
    {
        public string ErrorCode {get;set;}
        public string Message {get;set;}
        
        public string StackTrace {get;set;}
    }
}