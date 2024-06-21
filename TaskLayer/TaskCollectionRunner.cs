using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskLayer;

namespace CMD
{
    public class TaskCollectionRunner
    {
        public List<BaseResultAnalyzerTask> AllTaskList { get; }

        public TaskCollectionRunner(List<BaseResultAnalyzerTask> allTasks)
        {
            AllTaskList = allTasks;
        }

        public void Run() => AllTaskList.ForEach(p => p.Run());
    }
}
