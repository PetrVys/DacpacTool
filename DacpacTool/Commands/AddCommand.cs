using ManyConsole;
using System.IO;

namespace DacpacTool
{
    public class AddCommand : ConsoleCommand
    {
        public string DacpacFilePath { get; set; }
        public string AddFilePath { get; set; }

        public AddCommand() 
        {
            IsCommand("Add", "Add element(s) to a DACPAC model");
            HasRequiredOption("f|file=", "DACPAC file to modify", p=> DacpacFilePath = p);
            HasRequiredOption("a|add=", "Add file - file content is pasted before first element of an existing DACPAC", p => AddFilePath = p);
        }

        public override int Run(string[] remainingArguments)
        {
            Assert.FileExists(DacpacFilePath);
            Assert.FileExists(AddFilePath);

            var addContent = File.ReadAllText(AddFilePath);
            var elementAdded = false;

            ProcessDacpac.Process(DacpacFilePath, (element, elementStr, writer) =>
            {
                if (!elementAdded)
                {
                    writer.WriteRaw(addContent);
                    elementAdded = true;
                }
                writer.WriteRaw(elementStr);
            });

            return 0;
        }
    }
}
