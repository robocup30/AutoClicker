using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoClicker
{
    public enum CommandType
    {
        Wait,           // Do nothing for x ms
        Click,          // Click location
        WaitForColor,   // Wait for color before proceeding to next command
        Label,          // Label used for jumping
        JumpToLabel,    // Jump to label

    }

    public class Command
    {
        public CommandType commandType { get; set; }
        public string data { get; set; }

        public Command()
        {
            this.commandType = CommandType.Wait;
            data = "";
        }

        public Command(CommandType type, string data)
        {
            this.commandType = type;
            this.data = data;
        }
    }
}
