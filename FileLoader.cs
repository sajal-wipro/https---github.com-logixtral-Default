using System.Text.Json.Nodes;

namespace Sandbox_console;

public class FileLoader
{
    private static string GetFilePath(string fileName)
    {
        return Path.Combine((Directory.GetParent(
                             Directory.GetCurrentDirectory())!
                                        .Parent)!
                                        .Parent!.ToString(), fileName);
    }
    public static JsonNode GetFileByName(string fileName)
    {
        string strMapping = File.ReadAllText(fileName);
        return JsonNode.Parse(strMapping);
    }

   
}
