using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoClicker
{
    public class CSVHandler
    {
        StreamReader reader;
        public string currentFileName = "";

        public void OpenFile(string fileName)
        {
            reader = new StreamReader(File.OpenRead(fileName));
            currentFileName = fileName;
        }

        public ObservableCollection<Command> ParseCurrentFile()
        {
            List<string> rows = new List<string>();
            ObservableCollection<Command> newCommandList = new ObservableCollection<Command>();

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] values = line.Split(',');

                Command newCommand = new Command();
                CommandType commandType = (CommandType)Enum.Parse(typeof(CommandType), values[0], true);
                newCommand.commandType = commandType;
                newCommand.data0 = values[1];
                newCommand.data1 = values[2];
                newCommand.data2 = values[3];
                newCommand.data3 = values[4];
                newCommand.data4 = values[5];

                newCommandList.Add(newCommand);
            }

            reader.Close();

            return newCommandList;
        }

        public void SaveCurrentCommands(ObservableCollection<Command> commandList)
        {
            SaveCurrentCommandsAs(currentFileName, commandList);
        }

        public void SaveCurrentCommandsAs(string fileName, ObservableCollection<Command> commandList)
        {
            Console.WriteLine("SAVING FILE AS " + fileName);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileName))
            {
                foreach (Command command in commandList)
                {
                    string line = command.commandType.ToString() + "," + command.data0 + "," + command.data1 + "," + command.data2 + "," + command.data3 + "," + command.data4;
                    file.WriteLine(line);
                }
            }
        }

    }
}
