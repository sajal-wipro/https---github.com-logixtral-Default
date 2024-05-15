using System.Text.Json;
using System.Text.Json.Nodes;
using Sandbox_console;
namespace Sandbox;
internal class Program
{
    private static void Main(string[] args)
    {
        var sourceJson = JsonObject.Parse(FileLoader.GetFileByName("SourceJson.json").ToString()); 
        var targetJson = JsonObject.Parse(FileLoader.GetFileByName("TargetJson.json").ToString()); 
        var res = ((JsonObject)sourceJson!).ToString().ToString().ExtractJsonFromPath(
            //"$.family.SampleObj.ComplxType.TestArray.x"
            //"$.NestedObj.ComplxNestedType.CmplxNestedArray.a"
            //"$.family.SampleObj.ComplxType.NestedArray.a"
            //"$.family.SampleObj.SampleArrayNested.elem"
            //"$.family.SampleObj.SampleArrayNested.bool"
            //"$.family.SampleArray.num"
            //"$.family.SampleArray.letter"
            //"$.address.phone.Model"
            "$.age"    
            //"$.family.number"
            //"$.family.boolean"
            //"$.firstName"
            //"$.AoA.q" //This is illegal because AoA.element is ambigous without array index; and array with index in this case is not valid
            , out JsonElement? test, false);
            Console.WriteLine(test);
        Console.WriteLine(JsonNode.Parse(test.ToJsonString()).GenerateTargetJson(targetJson,
        //"$.address.phone.number"
        //"$.address.phone.Model"
        //"$.family[].SampleObj.ComplxType[].NestedArray[].a"
        //"$.NestedObj.ComplxNestedArrType[].CmplxNestedArray[].a"
        //"$.family[].SampleObj.SampleArrayNested[].elem"
        //"$.address.phone.Model"
        "$.age"
        , true));
        Console.WriteLine(targetJson);
    }
}