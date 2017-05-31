using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sandbox.tools
{
    public static class Retry
    {
        public async static Task InvokeAsync(Func<Task> asyncAction, int retryCount = -1, int delay = 0, Func<Exception, bool> filter = null)
        {
            //a list to aggregate excpetions caught on retry attempts
            //note this only gets initialize if we're not retrying infintely
            List<Exception> innerExs = retryCount >= 0 ? new List<Exception>() : null;

            while (true)
            {
                try
                {
                    //await the completion asyncAction
                    await asyncAction();

                    //if asyncAction failed due to exception we will jump down to the catch
                    //we only get here if asyncAction ran successfully so we break out of the loop
                    return;
                }
                catch (Exception e) when (filter == null || filter(e))
                {
                    //only do any exception handling if we're not retrying infinitely
                    if (retryCount >= 0)
                    {
                        innerExs.Add(e);

                        //if we don't have any retries left throw the aggregate exception
                        //note we use retryCount-- as opposed to --retryCount because we want to check if the original value was zero
                        if (retryCount-- == 0)
                        {
                            throw new AggregateException("The specified action failed {0} times.  See InnerExceptions for specific failures.", innerExs);
                        }
                    }
                }

                //if a valid delay was specified delay before retry
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
            }
        }

        public async static Task<T> InvokeAsync<T>(Func<Task<T>> asyncAction, int retryCount = -1, int delay = 0, Func<Exception, bool> filter = null)
        {
            //a list to aggregate excpetions caught on retry attempts
            //note this only gets initialize if we're not retrying infintely
            List<Exception> innerExs = retryCount >= 0 ? new List<Exception>() : null;

            while (true)
            {
                try
                {
                    //await the completion asyncAction
                    //if asyncAction failed due to exception we will jump down to the catch
                    //we only return here if asyncAction ran successfully so we break out of the loop
                    return await asyncAction();
                }
                catch (Exception e) when (filter == null || filter(e))
                {
                    //only do any exception handling if we're not retrying infinitely
                    if (retryCount >= 0)
                    {
                        innerExs.Add(e);

                        //if we don't have any retries left throw the aggregate exception
                        //note we use retryCount-- as opposed to --retryCount because we want to check if the original value was zero
                        if (retryCount-- == 0)
                        {
                            throw new AggregateException("The specified action failed {0} times.  See InnerExceptions for specific failures.", innerExs);
                        }
                    }
                }

                //if a valid delay was specified delay before retry
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
            }
        }

        public async static Task InvokeAsync(Action action, int retryCount = -1, int delay = 0, Func<Exception, bool> filter = null)
        {
            //a list to aggregate excpetions caught on retry attempts
            //note this only gets initialize if we're not retrying infintely
            List<Exception> innerExs = retryCount >= 0 ? new List<Exception>() : null;

            while (true)
            {
                try
                {
                    //run the action
                    action();

                    //if action failed due to exception we will jump down to the catch
                    //we only get here if action ran successfully so break out of the loop
                    return;
                }
                catch (Exception e) when (filter == null || filter(e))
                {
                    //only do any exception handling if we're not retrying infinitely
                    if (retryCount >= 0)
                    {
                        innerExs.Add(e);

                        //if we don't have any retries left throw the aggregate exception
                        //note we use retryCount-- as opposed to --retryCount because we want to check if the original value was zero
                        if (retryCount-- == 0)
                        {
                            throw new AggregateException("The specified action failed {0} times.  See InnerExceptions for specific failures.", innerExs);
                        }
                    }
                }

                //NOTE: if no delay is specified or an invalid delay is specified this method will exectute syncronously
                //if a valid delay was specified delay before retry
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
            }
        }

        public static void Invoke(Action action, int retryCount = -1, int delay = 0, Func<Exception, bool> filter = null)
        {
            //a list to aggregate excpetions caught on retry attempts
            //note this only gets initialize if we're not retrying infintely
            List<Exception> innerExs = retryCount >= 0 ? new List<Exception>() : null;

            while(true)
            {
                try
                {
                    //run the action
                    action();

                    //if action failed due to exception we will jump down to the catch
                    //we only get here if action ran successfully so break out of the loop
                    return;
                }
                catch (Exception e) when (filter == null || filter(e))
                {
                    //only do any exception handling if we're not retrying infinitely
                    if (retryCount >= 0)
                    {
                        innerExs.Add(e);

                        //if we don't have any retries left throw the aggregate exception
                        //note we use retryCount-- as opposed to --retryCount because we want to check if the original value was zero
                        if (retryCount-- == 0)
                        {
                            throw new AggregateException("The specified action failed {0} times.  See InnerExceptions for specific failures.", innerExs);
                        }
                    }
                }

                //if a valid delay was specified delay before retry
                if (delay > 0)
                {
                    Task.Delay(delay).Wait();
                }
            }
        }

        public async static Task InvokeWithRetryAsync(this Func<Task> asyncAction, int retryCount = -1, int delay = 0, Func<Exception, bool> filter = null)
        {
            await InvokeAsync(asyncAction, retryCount, delay, filter);
        }

        public async static Task<T> InvokeWithRetryAsync<T>(this Func<Task<T>> asyncAction, int retryCount = -1, int delay = 0, Func<Exception, bool> filter = null)
        {
            return await InvokeAsync(asyncAction, retryCount, delay, filter);
        }

        public async static Task InvokeWithRetryAsync(this Action action, int retryCount = -1, int delay = 0, Func<Exception, bool> filter = null)
        {
            await InvokeAsync(action, retryCount, delay, filter);
        }

        public static void InvokeWithRetry(this Action action, int retryCount = -1, int delay = 0, Func<Exception, bool> filter = null)
        {
            Invoke(action, retryCount, delay, filter);
        }

    }
}
