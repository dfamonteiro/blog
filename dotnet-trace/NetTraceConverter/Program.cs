using System.CommandLine;
using System.CommandLine.Parsing;

Option<FileInfo> fileOption = new("--file")
{
    Description = "The file to read and display on the console"
};

RootCommand rootCommand = new("Sample app for System.CommandLine");
rootCommand.Options.Add(fileOption);

ParseResult parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count == 0 && parseResult.GetValue(fileOption) is FileInfo parsedFile)
{
    TraceFileFormatConverter.ConvertToFormat(Console.Out, Console.Out, TraceFileFormat.Chromium, parsedFile.FullName, "out.chromium.json");
}

return 0;