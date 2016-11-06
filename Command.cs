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
        Label,              // Label used for jumping                               data0 = Label
        JumpToLabel,        // Jump to label                                        data0 = Label
        IfColorGoToLabel,   // If color is found, jump to label                     data0 = Color coordinate, data1 = Color, data2 = Tolerance, data3 = Label
        WaitForColor,       // Wait for color before proceeding to next command     data0 = Color coordinate, data1 = Color, data2 = Tolerance
        SetVariable,        // Set Variable x as y                                  data0 = Variable, data1 = Amount
        ChangeVariableBy,   // Change variable x by y                               data0 = Variable, data1 = Amount
        IfVariableGoToLabel,// Jump To Label if variable is OP to number            data0 = Variable, data1 = OP, data2 = #, data3 = Label
        Drag,               // Click and drag cursor                                data0 = start coordinate, data1 = end coordinate, data2 = duration
        ScreenShot,         // Take screenshot                                      data0 = file location (will be time stamped for name)   eg C:\Users\Joseph\Desktop\
        Flash,
    }

    public enum OpType
    {
        EQ,
        NE,
        GT,
        LT,
        GE,
        LE
    }

    public class Command
    {
        public CommandType commandType { get; set; }
        public string data0 { get; set; }
        public string data1 { get; set; }
        public string data2 { get; set; }
        public string data3 { get; set; }
        public string data4 { get; set; }       // Just in case for future
        public string comment { get; set; }

        public Command()
        {
            this.commandType = CommandType.Wait;
            data0 = "";
            data1 = "";
            data2 = "";
            data3 = "";
            data4 = "";
            comment = "";
        }

        public Command(CommandType type)
        {
            this.commandType = type;
        }

        public Command(CommandType type = CommandType.Wait, string data0 = "", string data1= "", string data2 = "", string data3 = "", string data4 = "", string comment = "")
        {
            this.commandType = type;
            this.data0 = data0;
            this.data1 = data1;
            this.data2 = data2;
            this.data3 = data3;
            this.data4 = data4;
            this.comment = comment;
        }
    }
}
