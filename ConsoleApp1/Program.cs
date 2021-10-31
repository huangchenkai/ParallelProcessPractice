using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskRunnerBase run = new KyleTaskRunner();
            //TaskRunnerBase run = new KylePipeline();
            run.ExecuteTasks(10);
        }
    }
}
