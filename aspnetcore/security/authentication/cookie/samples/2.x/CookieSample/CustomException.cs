using System;

namespace CookieSample
{
    public class CustomException : Exception
    {        
        public CustomException(Guid id) :
            base($"Testing {id}")
        {
            CustomExceptionId = id;
        }
        public Guid CustomExceptionId { get; }
    }
}
