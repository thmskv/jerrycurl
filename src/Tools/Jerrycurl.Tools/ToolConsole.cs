using System;
using System.Threading.Tasks;

namespace Jerrycurl.Tools
{
    public class ToolConsole
    {
        public virtual void Write(string message, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void WriteLine(string message, ConsoleColor color = ConsoleColor.White)
            => this.Write(message + "\n", color);

        public async Task<T> RunAsync<T>(string action, Func<Task<T>> asyncTask)
        {
            this.Write($"> {action}...");

            try
            {
                T result = await asyncTask();

                this.WriteLine(" ✓", ConsoleColor.Green);

                return result;
            }
            catch
            {
                this.WriteLine(" ×", ConsoleColor.Red);
                throw;
            }
        }

        public T Run<T>(string action, Func<T> task)
        {
            this.Write($"> {action}...");

            try
            {
                T result = task();

                this.WriteLine(" ✓", ConsoleColor.Green);

                return result;
            }
            catch
            {
                this.WriteLine(" ×", ConsoleColor.Red);
                throw;
            }
        }


        public async Task RunAsync(string action, Func<Task> asyncTask) => await this.RunAsync(action, async () =>
        {
            await asyncTask();

            return 0;
        });

        public void Run(string action, Action task) => this.Run(action, () =>
        {
            task();

            return 0;
        });
    }
}
