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

        public virtual void Error(string message)
        {
            Console.Error.Write(message);
        }

        public void WriteLine(string message, ConsoleColor color = ConsoleColor.White)
            => this.Write(message + "\n", color);
    }
}
