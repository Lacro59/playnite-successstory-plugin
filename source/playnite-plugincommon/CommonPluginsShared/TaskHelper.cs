using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CommonPluginsShared
{
    // TODO Used?
    /*
    public class SystemTask
    {
        public CancellationTokenSource tokenSource { get; set; }
        public Task task { get; set; }
    }


    public class TaskHelper
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private List<SystemTask> SystemTask = new List<SystemTask>();


        public void Add(Task task, CancellationTokenSource tokenSource)
        {
            SystemTask.Add(new SystemTask { task = task, tokenSource = tokenSource });
        }

        public void Check()
        {
            try
            {
                List<SystemTask> TaskDelete = new List<SystemTask>();

                Common.LogDebug(true, $"SystemTask {SystemTask.Count}");

                // Check task status
                foreach (var taskRunning in SystemTask)
                {
                    if (taskRunning.task.Status != TaskStatus.RanToCompletion)
                    {
                        Common.LogDebug(true, $"Task {taskRunning.task.Id} ({taskRunning.task.Status}) is canceled");
                        
                        // Cancel task if not terminated
                        taskRunning.tokenSource.Cancel();
                    }
                    else
                    {
                        // Add for delete
                        TaskDelete.Add(taskRunning);
                    }
                }

                // Delete tasks
                foreach (var taskRunning in TaskDelete)
                {
                    SystemTask.Remove(taskRunning);

                    Common.LogDebug(true, $"Task {taskRunning.task.Id} ({taskRunning.task.Status}) is removed");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
            }
        }
    }
    */
}
