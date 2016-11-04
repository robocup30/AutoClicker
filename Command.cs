using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoClicker
{
    public enum CommandType
    {
        Wait,               // Do nothing for x ms                                  data0 = Wait time
        Click,              // Click location                                       data0 = coordinate, data1 = Wait time until next command
        IfColorGoToLabel,   // If color is found, jump to label                     data0 = Color coordinate, data1 = Color, data2 = Tolerance, data3 = Label
        WaitForColor,       // Wait for color before proceeding to next command     data0 = Color coordinate, data1 = Color, data2 = Tolerance
        Label,              // Label used for jumping                               data0 = Label
        JumpToLabel,        // Jump to label                                        data0 = Label
        IfVariableGoToLabel,// Jump To Label if variable is OP to number            data0 = Variable, data1 = OP, data2 = #, data3 = Label
        ChangeVariableBy,   // Change variable x by y                               data0 = Variable, data1 = Amount
        SetVariable,        // Set Variable x as y                                  data0 = Variable, data1 = Amount
    }

    public class Command
    {
        public CommandType commandType { get; set; }
        public string data0 { get; set; }
        public string data1 { get; set; }
        public string data2 { get; set; }
        public string data3 { get; set; }

        public Command()
        {
            this.commandType = CommandType.Wait;
            data1 = "";
        }

        public Command(CommandType type, string data)
        {
            this.commandType = type;
            this.data0 = data;
        }
    }
}
