# RequestRateLimiter
Code for a request-rate limiter, which tracks a number of requests and calculates a suitable delay to limit the number of requests during a time period. Includes a BizTalk sample.

A sample usage in C#:

RequestRateLimiter rateLimiter = new RequestRateLimiter(20, 10); // maximum 20 requests in 10 seconds
for(int i = 1; i <= 50; i++)
{
    TimeSpan waitTime = rateLimiter.GetWaitDuration();
    if (waitTime != TimeSpan.Zero)
    {
        Thread.Sleep(waitTime);
    }
    rateLimiter.Add();
    MyProxy.ServiceAPICall(); //make your call here
}
