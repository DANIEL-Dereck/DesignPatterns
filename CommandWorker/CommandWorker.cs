using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommandWorker
{
    /// <summary>
    /// This class manages a Worker task wich goal is to execute each task from a command queue.
    /// It is a producer / consumer pattern, just enqueue task then the worker will execute one by one in FIFO order.
    /// </summary>
    public class CommandWorker
    {
        #region Members

        /// <summary>
        /// A list that stores all the commands that has been called before the service instanciation.
        /// When the service is ready, we replay these commands.
        /// </summary>
        private LinkedList<Action> mCommandQueue;

        /// <summary>
        /// Global lock on the action list.
        /// </summary>
        private readonly object mListLock;

        /// <summary>
        /// Global lock on the worker start/stop methods.
        /// </summary>
        private readonly object mWorkerLock;

        /// <summary>
        /// Monitor for multi-thread management.
        /// </summary>
        private ManualResetEvent mTaskHandler;

        /// <summary>
        /// The unique high priority command. 
        /// </summary>
        private Action HeadCommand;

        /// <summary>
        /// The componenent's name that produces commands.
        /// </summary>
        private string mProducer;

        #endregion

        #region Constructors

        /// <summary>
        /// The CancellationTokenSource and ManualResetEvent from the caller class or from this class references the same storage in memory.
        /// Even if them referenced values change they will point out the same object.
        /// </summary>
        /// <param name="producer">The componenent's name that produces commands.</param>
        public CommandWorker(string producer)
        {
            mProducer = producer;
            HeadCommand = null;
            mListLock = new object();
            mWorkerLock = new object();
            mCommandQueue = new LinkedList<Action>();
            CancellationTokenSource = new CancellationTokenSource();
            mTaskHandler = new ManualResetEvent(false);
            AsyncWorkerTask = null;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Add a command to the spool. Then, signal the consumer task to execute it.
        /// </summary>
        /// <remarks>
        /// Any action enqueued in the command worker must not be time consuming and must not wait actions from UI thread. 
        /// This could block the deactivated/suspended App life cycle event as it is waiting for the last running command to finish.
        /// Also beware of the <see cref="System.Net.HttpWebRequest.BeginGetResponse"/> method in silverlight, 
        /// it will hang the command worker as it is performing some work in the UI thread, it must be executed in a dedicated thread.
        /// </remarks>
        /// <param name="command">The new command</param>
        public void Enqueue(Action command)
        {
            lock (mListLock)
            {
                mCommandQueue.AddFirst(command);

                /* Awake the worker task. */
                if (!CancellationTokenSource.IsCancellationRequested)
                    mTaskHandler.Set();
            }
        }

        /// <summary>
        /// Remove a command from the spool. Signal the consumer to wait if there is no more commands available in the spool.
        /// </summary>
        private Action GetNextAction()
        {
            Action command = null;
            lock (mListLock)
            {
                /* Get the Head action if there is one. */
                if (HeadCommand != null)
                {
                    command = HeadCommand;
                    HeadCommand = null;
                }

                /* Or get next action from list. */
                else
                {
                    command = mCommandQueue.Last.Value;
                    mCommandQueue.RemoveLast();
                }

                /* Signal that there are no more actions to perform. */
                if (mCommandQueue.Count == 0 && HeadCommand == null)
                    mTaskHandler.Reset();
                return command;
            }
        }

        /// <summary>
        /// Set the current head command or replace it if it was already set.
        /// The head command is of the highest priority.
        /// </summary>
        /// <param name="command">The new command</param>
        public void AddHeadAction(Action command)
        {
            lock (mListLock)
            {
                HeadCommand = command;

                /* Awake the worker task. */
                if (!CancellationTokenSource.IsCancellationRequested)
                    mTaskHandler.Set();
            }
        }

        /// <summary>
        /// Remove all commands from the spool.
        /// </summary>
        private void Clear()
        {
            lock (mListLock)
            {
                mCommandQueue.Clear();
                HeadCommand = null;
            }
        }

        /// <summary>
        /// The command consumer task.
        /// </summary>
        protected void AsyncWorker(CancellationToken cancelToken, ManualResetEvent taskHandler)
        {
            /* The pending commands. */
            while (cancelToken.IsCancellationRequested == false)
            {
                /* Wait for command producer if needed. */
                taskHandler.WaitOne();

                /* Get and execute a pending command if cancellation was not requested during wait time. */
                if (!cancelToken.IsCancellationRequested)
                    Execute(GetNextAction());
            }

            /* Clear the command spool. */
            Clear();
        }

        /// <summary>
        /// Start or restart the worker thread.
        /// </summary>
        public void Start()
        {
            lock (mWorkerLock)
            {
                /* There is no worker yet, initialize it. */
                if (AsyncWorkerTask == null)
                    AsyncWorkerTask = Task.Factory.StartNew(() => AsyncWorker(CancellationTokenSource.Token, mTaskHandler), CancellationTokenSource.Token);

                /* There is a worker already. */
                else
                {
                    /* ReStart worker if it was cancelled. */
                    if (CancellationTokenSource.IsCancellationRequested)
                    {
                        /* Initialize new thread management stuff. */
                        CancellationTokenSource = new CancellationTokenSource();
                        mTaskHandler = new ManualResetEvent(mCommandQueue.Count > 0);

                        /* Wait till last task ends */
                        try
                        {
                            AsyncWorkerTask.Wait();
                        }
                        catch (Exception e)
                        {
                            /* Task may already been cancelled. Nothing to do. */
                            Console.WriteLine(mProducer, "Worker task released already: ", e);
                        }

                        /* Continue with a new worker. */
                        AsyncWorkerTask = Task.Factory.StartNew(() => AsyncWorker(CancellationTokenSource.Token, mTaskHandler), CancellationTokenSource.Token);
                    }
                }
                Console.WriteLine(mProducer, "Command worker started.");
            }
        }

        /// <summary>
        /// Stop the worker.
        /// </summary>
        public void Stop()
        {
            lock (mWorkerLock)
            {
                /* Stop worker if it was not already cancelled. */
                if (!CancellationTokenSource.IsCancellationRequested)
                {
                    /* Try to free the task so it can finish if it was waiting. */
                    try
                    {
                        /* Cancel task. */
                        CancellationTokenSource.Cancel();
                        mTaskHandler.Set();
                    }
                    catch
                    {
                        /* Task handler already disposed so the task is successfully cancelled. Nothing to do. */
                    }

                    try
                    {
                        /* Wait till the worker finishes its last command. */
                        AsyncWorkerTask.Wait();

                        /* Dispose resource. */
                        mTaskHandler.Dispose();
                    }
                    catch (Exception e)
                    {
                        /* Task may already been released. Nothing to do. */
                        Console.WriteLine(mProducer, "Worker task released already: ", e);
                    }
                }
                Console.WriteLine(mProducer, "Command worker successfully stopped.");
            }
        }

        /// <summary>
        /// Execute a command and catch exceptions.
        /// </summary>
        /// <param name="command">Command</param>
        protected void Execute(Action command)
        {
            if (command != null)
            {
                /* Execute the command. */
                try
                {
                    command();
                }

                /* If a command produces an Exception, trace its message. */
                catch (Exception exception)
                {
                    Trace.WriteLine(exception.Message);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get the command worker task.
        /// </summary>
        /// <value>
        /// The worker task.
        /// </value>
        public Task AsyncWorkerTask
        {
            get;
            private set;
        }

        /// <summary>
        /// Task cancellation token.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource
        {
            get;
            private set;
        }

        #endregion
    }
}