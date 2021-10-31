using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class KyleTaskRunner : TaskRunnerBase
    {
        /// <summary>
        /// 開多執行續，Degree使用Every Step max concurrent count
        /// </summary>
        /// <param name="tasks"></param>
        public override void Run(IEnumerable<MyTask> tasks)
        {
            var totalStepAmount = PracticeSettings.TASK_STEPS_CONCURRENT_LIMIT.Sum();

            tasks.AsParallel().WithDegreeOfParallelism(totalStepAmount).ForAll((t) =>
            {
                Task.Run(() =>
                {
                    t.DoStepN(1);
                    t.DoStepN(2);
                    t.DoStepN(3);
                }).Wait();
            });
        }
    }
    /// <summary>
    /// 使用Q的方式解偶每個Step
    /// 拿AndrewPipelineTaskRunner3調整細部(Task.Wait =>Task.WhenAll，效能好一些，也比較不會發生有執行續阻塞)
    /// </summary>
    public class KylePipeline : TaskRunnerBase
    {
        public override void Run(IEnumerable<MyTask> tasks)
        {
            Task t1 = this.DoAllStepNAsync(1);
            Task t2 = this.DoAllStepNAsync(2);
            Task t3 = this.DoAllStepNAsync(3);

            var initQ1 = Task.WhenAll(tasks.Select(t => Task.Run(async () =>
            {
                await this.queues[1].Writer.WriteAsync(t);
            })));

            initQ1.Wait();
            queues[1].Writer.Complete();

            var processAll = Task.WhenAll(t1, t2, t3);
            processAll.Wait();
        }

        private Channel<MyTask>[] queues = new Channel<MyTask>[3 + 1]
        {
            null,
            Channel.CreateBounded<MyTask>(5),
            Channel.CreateUnbounded<MyTask>(),
            Channel.CreateUnbounded<MyTask>(),
        };

        private async Task DoAllStepNAsync(int step)
        {
            bool last = (step == 3);
            List<Task> ts = new List<Task>();
            while (await this.queues[step].Reader.WaitToReadAsync())
            {
                while (this.queues[step].Reader.TryRead(out MyTask task))
                {
                    ts.Add(Task.Run(async () =>
                    {
                        task.DoStepN(step);

                        if (!last) await this.queues[step + 1].Writer.WriteAsync(task);
                    }));
                }
            }
            var processStep = Task.WhenAll(ts);
            processStep.Wait();
            if (!last) this.queues[step + 1].Writer.Complete();
        }
    }
}
