using System;
using System.Collections;

namespace RequestRateLimiterLibrary
{
    /// <summary>
    /// Class to keep track of a number of requests (actually, only timestamps)
    /// in order to limit the number of requests made during a sliding time frame.
    /// A typical requirement is "maximum 20 requests during a 10-second interval".
    /// Call the Add() method immediately before transmitting a request.
    /// Before transmitting the next request, call the GetWaitDuration() to determine
    /// how long your thread must wait until you can transmit the request.
    /// It is safe to call GetWaitDuration() with an empty history.
    /// If you supply your own timestamp values, make sure you use UTC timestamps,
    /// otherwise you may have problems during switch to and from daylight savings
    /// time.
    /// Current implementation is not thread-safe. You must call this library from only
    /// one thread at a time.
    /// </summary>
    /// <example>
    ///     RequestRateLimiter hist = new RequestRateLimiter(20, 10);
    ///     for(int i = 1; i &lt;= 50; i++)
    ///     {
    ///         TimeSpan waitTime = hist.GetWaitDuration();
    ///         if (waitTime != TimeSpan.Zero)
    ///         {
    ///             Thread.Sleep(waitTime);
    ///         }
    ///         hist.Add();
    ///         //make your call here
    ///     }
    /// </example>
    [Serializable]
    public class RequestRateLimiter
    {
        private SortedList history;

        private int maxRequestCount;
        private TimeSpan timeframe;

        /// <summary>
        /// Initializes and configures the rate limiter.
        /// Typical scenario is "maximum 20 requests during 10 seconds".
        /// </summary>
        /// <exception cref="ArgumentException">Throws ArgumentException if any constructor parameter is smaller than 1.</exception>
        /// <param name="MaxRequestCount">The number of request timestamps to keep track of.</param>
        /// <param name="duringTimespanSeconds">The time span for which the MaxRequestCount must be maintained.</param>
        public RequestRateLimiter(int MaxRequestCount, int duringTimespanSeconds)
        {
            if (MaxRequestCount < 1 || duringTimespanSeconds < 1)
            {
                throw new ArgumentException("Both MaxRequestCount and duringTimespanSeconds must be greater than zero.");
            }
            this.history = new SortedList();
            this.maxRequestCount = MaxRequestCount;
            this.timeframe = new TimeSpan(0, 0, duringTimespanSeconds); // hours, minutes, seconds
        }

        /// <summary>
        /// Add a timestamp (of when the request is made).
        /// If the maximum number of requests is already reached,
        /// the oldest timestamp is removed.
        /// </summary>
        /// <param name="currentUTC">The timestamp for the request. If ignored or null, the computer's current UTC date and time will be used.</param>
        public void Add(DateTimeOffset? currentUTC = null)
        {
            if (this.history.Count >= this.maxRequestCount)
            {
                // index is zero-based
                this.history.RemoveAt(0);
            }
            if (currentUTC == null) { currentUTC = DateTimeOffset.UtcNow; }
            this.history.Add(currentUTC, currentUTC);
        }

        /// <summary>
        /// Add a timestamp of the computer's current UTC date and time.
        /// If the maximum number of requests is already reached,
        /// the oldest timestamp is removed.
        /// </summary>
        public void Add()
        {
            Add(null);
        }


        /// <summary>
        /// Calculates the waiting time to fulfill the requirement of
        /// a maximum number of requests during a sliding time span.
        /// If no waiting is necessary, Timespan.Zero is returned.
        /// It is safe to call GetWaitDuration() with an empty history.
        /// </summary>
        /// <param name="currentUTC">The UTC date and time for which to calculate the wait time. If ignored or null, the computer's current UTC date and time will be used.</param>
        /// <returns>A TimeSpan, possibly TimeSpan.Zero if no wait is needed</returns>
        public TimeSpan GetWaitDuration(DateTimeOffset? currentUTC = null)
        {
            if (this.history.Count < this.maxRequestCount)
            {
                return TimeSpan.Zero;
            }
            // index is zero-based
            DateTimeOffset newest = (DateTimeOffset)this.history.GetByIndex(this.history.Count - 1);
            DateTimeOffset oldest = (DateTimeOffset)this.history.GetByIndex(0);

            if (currentUTC == null) { currentUTC = DateTimeOffset.UtcNow; }

            // Back one time frame and see where we end up:
            // Type casting needed to cast from DateTimeOffset? to DateTimeOffset
            DateTimeOffset oneTimeframeBack = (DateTimeOffset)currentUTC - this.timeframe;
            // If we end up before the oldest, we have to wait until the oldest will be outside the time frame:
            //
            // ____I_I_I_I_I_I_I_I_I_I_I_I_I_I_I_I_I_I_I_I________________> t
            //     ^                                     ^  ^now      ^
            //     |                                     |            |
            //     |oldest                               |newest      |(oldest+timeframe)
            //
            if (oneTimeframeBack < oldest)
            {
                return oldest + this.timeframe - (DateTimeOffset)currentUTC;
            }
            else
            {
                // The oldest was older than a time frame, that means we can transmit immediately:
                return TimeSpan.Zero;
            }
        }


        /// <summary>
        /// Calculates the waiting time to fulfill the requirement of
        /// a maximum number of requests during a sliding time span.
        /// If no waiting is necessary, Timespan.Zero is returned.
        /// It is safe to call GetWaitDuration() with an empty history.
        /// </summary>
        /// <returns>A TimeSpan, possibly TimeSpan.Zero if no wait is needed</returns>
        public TimeSpan GetWaitDuration()
        {
            return GetWaitDuration(null);
        }
    }
}
