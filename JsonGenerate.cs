using System.Text.Json;
using System.Text.Json.Nodes;
using Namotion.Reflection;
namespace Sandbox;
using KV_Pair = KeyValuePair<string, JsonNode?>;
public static class JsonDomExtentions
{

    public static bool ExtractJsonFromPath(this string sourceJsonObjectString,
                                                string sourceJsonPath,
                                                out JsonElement? outNode,
                                                bool isFlatten,
                                                int arrDepth = 0)
    {
        //This logic doesnt work when array is provided without key-value pair eg 
        //arr:[1,2,3]  //won't work
        // Also Array of arrays below won't work because AoA.<element> is ambigous
        // since no schema can be defined for this case 
        // "AoA":[
        //[{"q":1}],
        //[{"p":true}]
        string jsonPath = string.Empty;
        //var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        bool Isnodefound = false;
        outNode = null;
        if (String.IsNullOrEmpty(sourceJsonObjectString) || String.IsNullOrEmpty(sourceJsonPath)) return Isnodefound;
        string part;
        if ("$" == sourceJsonPath[..1])
        {
            jsonPath = sourceJsonPath[2..];
            part = sourceJsonPath.Split('.').Skip(1).First();// remove $ from JSON root path
        }
        else
        {
            jsonPath = sourceJsonPath.Substring(0, sourceJsonPath.Length);
            part = sourceJsonPath.Split('.').First();
        }
        JsonElement currentElement = sourceJsonObjectString.FromJsonString<JsonElement>();
        string pElement = string.Empty;
        if (currentElement.ValueKind.Equals(JsonValueKind.Array))
        {
            arrDepth++;
            JsonArray jArray = new JsonArray();
            foreach (Object elem in currentElement.EnumerateArray())
            {
                bool leafNode=false;
                if (((JsonElement)elem).TryGetProperty(part, out var nxtElement))
                {
                    if (sourceJsonPath.Split('.').Count() > 1)
                    {
                        currentElement = nxtElement;
                        var newPath = jsonPath[(jsonPath.IndexOf('.') + 1)..];
                        currentElement.ToJsonString().ExtractJsonFromPath(newPath, out JsonElement? outputNode, isFlatten, arrDepth);
                        currentElement = outputNode ?? default;
                        if(arrDepth != 1)
                        {
                            jArray.Add(currentElement);
                            currentElement = jArray.ToJsonString().FromJsonString<JsonElement>();
                        }
                    }
                    else if (((JsonElement)elem).TryGetProperty(part, out var nextElement))// last part of json path
                    {
                        leafNode = true;
                        currentElement = (new JsonObject{[jsonPath] = nextElement.ToJsonString().FromJsonString<JsonNode>()
                                                        }).ToJsonString().FromJsonString<JsonElement>();
                        jArray.Add(currentElement);
                        currentElement = jArray.ToJsonString().FromJsonString<JsonElement>();
                    }
                }
                if (arrDepth == 1 && !leafNode)
                {
                    if (isFlatten)
                    {
                        jArray = jArray.Concat(currentElement.ToJsonString().FromJsonString<JsonArray>()!)
                                                             .ToJsonString().FromJsonString<JsonArray>()!;
                    }
                    else
                    {
                        jArray.Add(currentElement);
                    }
                    currentElement = jArray.ToJsonString().FromJsonString<JsonElement>();
                }
            }
        }
        else // Non Array Case
        {
            if (sourceJsonPath.Split('.').Count() > 1)
            {
                var newPath = jsonPath[(jsonPath.IndexOf('.') + 1)..];
                if (currentElement.TryGetProperty(part, out var nextElement))
                {
                    currentElement = nextElement;
                }
                currentElement.ToJsonString().ExtractJsonFromPath(newPath, out JsonElement? outputNode, isFlatten, arrDepth);
                currentElement = outputNode ?? default;
            }
            else
            {
                if(!currentElement.ValueKind.Equals(JsonValueKind.Object))
                {
                    currentElement = (new JsonObject
                        {
                            [jsonPath] = currentElement.ToJsonString().FromJsonString<JsonNode>()
                        }).ToJsonString().FromJsonString<JsonElement>();
                }
                else
                {
                    if (currentElement.TryGetProperty(jsonPath, out var nextElement))
                    {
                        currentElement = (new JsonObject
                        {
                            [jsonPath] = nextElement.ToJsonString().FromJsonString<JsonNode>()
                        }).ToJsonString().FromJsonString<JsonElement>();
                    }
                }
            }
        }
        outNode = currentElement;
        return false;

    }
    public static bool GenerateTargetJson(this JsonNode? sourceJson, JsonNode? targetJson, string jsonTargetPath, bool isFlatten)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        bool Isnodefound = false;
        if (String.IsNullOrEmpty(targetJson!.ToJsonString()) || String.IsNullOrEmpty(jsonTargetPath)) return Isnodefound;
        string part;
        string jsonPath = string.Empty;
        if ("$" == jsonTargetPath[..1])
        {
            jsonPath = jsonTargetPath[2..];
            part = jsonTargetPath.Split('.').Skip(1).First();// remove $ from JSON root path
        }
        else
        {
            jsonPath = jsonTargetPath.Substring(0, jsonTargetPath.Length);
            part = jsonTargetPath.Split('.').First();
        }
        JsonNode currentTargetElement = targetJson??default!;
        string pElement = string.Empty;
        if (currentTargetElement!.GetType().Equals(JsonValueKind.Array)
            || currentTargetElement.GetType().Equals(typeof(System.Text.Json.Nodes.JsonArray)))
        {
            int sourceArrayElemIndx = 0;
            foreach(JsonNode? elem in (sourceJson as JsonArray)!)// Iteration is based on source json
            {
                string jPath = jsonPath;
                string jPart = part;
                jPart = (part.Contains("[]"))? part.Replace("[]", string.Empty) : part;
                
                var nestedElem = currentTargetElement;
                if (!((((JsonArray)currentTargetElement)).Count() > sourceArrayElemIndx))
                {
                    if((jPath.Split('.').Count() > 1))
                    {
                        ((JsonArray)nestedElem).Add(new JsonObject{[jPart]= (part.Contains("[]"))?new JsonArray{}:new JsonObject{}});
                        nestedElem=nestedElem[sourceArrayElemIndx]![jPart];
                    }
                }
                if (jPath.Split('.').Count() > 1)//non-leaf
                {
                    jPath = jPath[(jPath.IndexOf('.') + 1)..];
                    elem.GenerateTargetJson(nestedElem, jPath, isFlatten);
                    jPath = jPath[(jPath.IndexOf('.') + 1)..];
                }
                else //leaf
                {
                    if(elem!.GetType().Equals(typeof(System.Text.Json.Nodes.JsonArray)))
                    {
                        int sourceArrElemIndx=0;
                        foreach(var arrElem in elem as JsonArray)
                        {
                            ((JsonArray)nestedElem!).Add(new JsonObject{[jPart]=new JsonObject{}});
                            nestedElem=nestedElem[sourceArrElemIndx++]![jPart];
                        }
                    }
                    else
                    {
                        object sJsonVal = elem!.ToJsonString().FromJsonString<JsonElement>().GetProperty(jPath);// handle case sensitive property names
                        if (nestedElem!.GetType().Equals(typeof(System.Text.Json.Nodes.JsonArray)))
                            ((JsonArray)nestedElem!).Add(new JsonObject{[jPart]=JsonValue.Create(elem[jPath]!.GetValue<object>())!});
                        else
                            ((JsonObject)nestedElem!).Add(jPath, JsonValue.Create(sJsonVal));
                    }
                }
                sourceArrayElemIndx++;
            }
        }
        else // Non Array Case
        {
            bool isArray = false;
            string jPath = jsonPath;
            string jPart = part;
            if (part.Contains("[]"))
            {
                jPart = part.Replace("[]", string.Empty);
                isArray = true;
            }
            else
            {
                jPart = part;
            }
            var nestedElem = currentTargetElement;
            if (jPath.Split('.').Length > 1)//non-leaf
            {
                if ((nestedElem[jPart]) == null)// intermediate node NOT found in target json then create it
                {
                    if(isArray)
                        ((JsonObject)nestedElem).Add(jPart, new JsonArray{});
                    else
                        ((JsonObject)nestedElem).Add(jPart, new JsonObject { });
                }
                nestedElem = nestedElem[jPart]!;
                jPath = jPath[(jPath.IndexOf('.') + 1)..];
                jPart = jPath.Split('.').First();
                sourceJson.GenerateTargetJson(nestedElem, jPath, isFlatten);
            }
            else //leaf
            {
                object sJsonVal = sourceJson!.ToJsonString().FromJsonString<JsonElement>().GetProperty(jPath);// handle case sensitive property names
                ((JsonObject)nestedElem).Add(jPath, JsonValue.Create(sJsonVal));
            }
        }
        return true;
    }
    public static bool GenerateTargetProps(this string sourceJson, string targetJson, string jsonTargetPath, bool isPreserverStructure)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        bool Isnodefound = false;
        if (String.IsNullOrEmpty(targetJson.ToJsonString()) || String.IsNullOrEmpty(jsonTargetPath)) return Isnodefound;
        var parts = jsonTargetPath.Split('.').Skip(1); // remove $ from JSON root path
        JsonElement currentElement = targetJson.FromJsonString<JsonElement>();

        string pElement = string.Empty;
        parts.ToList().ForEach(pathElement =>
        {
            var pathSubstring = pathElement.Substring(pathElement.Length - 2); // Get last 2 elements ie [] if array
            if (pathSubstring == "[]")
            {
                pathElement = pathElement[..^2];
            }
            pElement = pathElement;

            if (currentElement.ValueKind.Equals(JsonValueKind.Array))
            {
                Isnodefound = true;
                currentElement = (currentElement.EnumerateArray()).Select(
                        jnode =>
                        {
                            if (jnode.ValueKind.Equals(JsonValueKind.Array))
                            {
                                JsonArray jArray = new JsonArray();
                                (jnode.EnumerateArray()).ToList().ForEach(node =>
                                {
                                    jArray.Add(new JsonObject
                                    {
                                        [pathElement] = JobjConverter<JsonNode>(pathElement, node)
                                    });
                                });
                                return (jArray != null) ? jArray : string.Empty;
                            }
                            else
                            {
                                var jVal = (JobjConverter<JsonElement>(pathElement, jnode));
                                if (jVal.ValueKind.Equals(JsonValueKind.Array) || jVal.ValueKind.Equals(JsonValueKind.Object))
                                {
                                    return jnode.ToJsonString().FromJsonString<JsonNode>()![pathElement];
                                }
                                else
                                {
                                    return new JsonObject
                                    {
                                        [pathElement] = JobjConverter<JsonNode>(pathElement, jnode)
                                    };
                                }
                            }
                        }
                ).ToJsonString().FromJsonString<JsonElement>();
            }
            else
            {
                if (currentElement.TryGetProperty(pathElement, out var nextElement))
                {
                    currentElement = nextElement;
                    Isnodefound = true;
                }
                else
                { Isnodefound = false; }
            }
        });
        JsonElement outNode = default;
        if (Isnodefound)
        {
            if (currentElement.ValueKind.Equals(JsonValueKind.Array) ||
               currentElement.ValueKind.Equals(JsonValueKind.Object))

            {
                outNode = currentElement;
            }
            else
                outNode = (new JsonObject
                {
                    [pElement] = currentElement.ToJsonString()
                                                                     .FromJsonString<JsonNode>()
                })
                                                                     .ToJsonString()
                                                                     .FromJsonString<JsonElement>();
        }
        return Isnodefound;
    }
    public static bool GetJsonElement(this string jsonObjectString, string jsonPath, out JsonElement? outNode)
    {
        //This logic doesnt work when array is provided without key-value pair eg 
        //arr:[1,2,3]  //won't work
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        bool Isnodefound = false;
        outNode = null;
        if (String.IsNullOrEmpty(jsonObjectString) || String.IsNullOrEmpty(jsonPath)) return Isnodefound;
        var parts = jsonPath.Split('.').Skip(1); // remove $ from JSON root path
        JsonElement currentElement = jsonObjectString.FromJsonString<JsonElement>();
        string pElement = string.Empty;
        parts.ToList().ForEach(pathElement =>
        {
            pElement = pathElement;
            if (currentElement.ValueKind.Equals(JsonValueKind.Array))
            {
                Isnodefound = true;
                currentElement = (currentElement.EnumerateArray()).Select(
                        jnode =>
                        {
                            if (jnode.ValueKind.Equals(JsonValueKind.Array))
                            {
                                JsonArray jArray = new JsonArray();
                                (jnode.EnumerateArray()).ToList().ForEach(node =>
                                {
                                    jArray.Add(new JsonObject
                                    {
                                        [pathElement] = JobjConverter<JsonNode>(pathElement, node)
                                    });
                                });
                                return (jArray != null) ? jArray : string.Empty;
                            }
                            else
                            {
                                var jVal = (JobjConverter<JsonElement>(pathElement, jnode));
                                if (jVal.ValueKind.Equals(JsonValueKind.Array) || jVal.ValueKind.Equals(JsonValueKind.Object))
                                {
                                    return jnode.ToJsonString().FromJsonString<JsonNode>()![pathElement];
                                }
                                else
                                {
                                    return new JsonObject
                                    {
                                        [pathElement] = JobjConverter<JsonNode>(pathElement, jnode)
                                    };
                                }
                            }
                        }
                ).ToJsonString().FromJsonString<JsonElement>();
            }
            else
            {
                if (currentElement.TryGetProperty(pathElement, out var nextElement))
                {
                    currentElement = nextElement;
                    Isnodefound = true;
                }
                else
                { Isnodefound = false; }
            }
        });
        if (Isnodefound)
        {
            if (currentElement.ValueKind.Equals(JsonValueKind.Array) ||
               currentElement.ValueKind.Equals(JsonValueKind.Object))

            {
                outNode = currentElement;
            }
            else
                outNode = (new JsonObject
                {
                    [pElement] = currentElement.ToJsonString()
                                                                     .FromJsonString<JsonNode>()
                })
                                                                     .ToJsonString()
                                                                     .FromJsonString<JsonElement>();
        }
        return Isnodefound;
    }

    public static bool GetJsonElementWithConditions(this string jsonObjectString,
                                                 string jsonPath,
                                                 out JsonElement? outNode,
                                                 bool preserveStructure)
    {
        //This logic doesnt work when array is provided without key-value pair eg 
        //arr:[1,2,3]  //won't work
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        bool Isnodefound = false;
        outNode = null;
        if (String.IsNullOrEmpty(jsonObjectString) || String.IsNullOrEmpty(jsonPath)) return Isnodefound;
        var parts = jsonPath.Split('.').Skip(1); // remove $ from JSON root path
        JsonElement currentElement = jsonObjectString.FromJsonString<JsonElement>();
        string pElement = string.Empty;
        parts.ToList().ForEach(pathElement =>
        {
            pElement = pathElement;
            if (currentElement.ValueKind.Equals(JsonValueKind.Array))
            {
                Isnodefound = true;
                if (!preserveStructure)
                {
                    IList<object> list = new List<object>();
                    currentElement = (currentElement.EnumerateArray()).Select(
                            jnode =>
                            {
                                if (jnode.ValueKind.Equals(JsonValueKind.Array))
                                {
                                    (jnode.EnumerateArray()).ToList().ForEach(node =>
                                    {
                                        list.Add(new JsonObject
                                        {
                                            [pathElement] = JobjConverter<JsonNode>(pathElement, node)
                                        });

                                    });
                                    return list;
                                }
                                else
                                {
                                    var jVal = (JobjConverter<JsonElement>(pathElement, jnode));
                                    if (jVal.ValueKind.Equals(JsonValueKind.Array) || jVal.ValueKind.Equals(JsonValueKind.Object))
                                    {
                                        return jnode.ToJsonString().FromJsonString<JsonNode>()![pathElement];
                                    }
                                    else
                                    {
                                        return new JsonObject
                                        {
                                            [pathElement] = JobjConverter<JsonNode>(pathElement, jnode)
                                        } as object;
                                    }
                                }
                            }
                    ).ToJsonString().FromJsonString<JsonElement>();
                    if (list.Count > 0)
                        currentElement = list.ToJsonString().FromJsonString<JsonElement>();
                }
                else
                {
                    currentElement = (currentElement.EnumerateArray()).Select(
                            jnode =>
                            {
                                if (jnode.ValueKind.Equals(JsonValueKind.Array))
                                {
                                    if (!preserveStructure)
                                    {
                                        IList<object> list = new List<object>();
                                        (jnode.EnumerateArray()).ToList().ForEach(node =>
                                        {

                                            list.Add(new JsonObject
                                            {
                                                [pathElement] = JobjConverter<JsonNode>(pathElement, node)
                                            });

                                        });
                                        return list;
                                    }
                                    else
                                    {
                                        JsonArray jArray = new JsonArray();
                                        (jnode.EnumerateArray()).ToList().ForEach(node =>
                                        {

                                            jArray.Add(new JsonObject
                                            {
                                                [pathElement] = JobjConverter<JsonNode>(pathElement, node)
                                            });

                                        });
                                        return (jArray != null) ? jArray : string.Empty;
                                    }
                                }
                                else
                                {
                                    var jVal = (JobjConverter<JsonElement>(pathElement, jnode));
                                    if (jVal.ValueKind.Equals(JsonValueKind.Array) || jVal.ValueKind.Equals(JsonValueKind.Object))
                                    {
                                        return jnode.ToJsonString().FromJsonString<JsonNode>()![pathElement];
                                    }
                                    else
                                    {
                                        if (!preserveStructure)
                                        {
                                            return jnode as Object;
                                        }
                                        else
                                        {
                                            return new JsonObject
                                            {
                                                [pathElement] = JobjConverter<JsonNode>(pathElement, jnode)
                                            };
                                        }
                                    }
                                }
                            }
                    ).ToJsonString().FromJsonString<JsonElement>();
                }
            }
            else
            {
                if (currentElement.TryGetProperty(pathElement, out var nextElement))
                {
                    currentElement = nextElement;
                    Isnodefound = true;
                }
                else
                { Isnodefound = false; }
            }
        });
        if (Isnodefound)
        {
            if (currentElement.ValueKind.Equals(JsonValueKind.Array) ||
               currentElement.ValueKind.Equals(JsonValueKind.Object))

            {
                outNode = currentElement;
            }
            else
                outNode = (new JsonObject
                {
                    [pElement] = currentElement.ToJsonString()
                                                                     .FromJsonString<JsonNode>()
                })
                                                                     .ToJsonString()
                                                                     .FromJsonString<JsonElement>();
        }
        return Isnodefound;
    }

    private static T JobjConverter<T>(string pathElement, JsonElement jnode)
    {
        return (jnode.ToJsonString()
                     .FromJsonString<JsonNode>()![pathElement]!)
                     .ToJsonString()
                     .FromJsonString<T>()!;
    }
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    //Json string to ms jsonobject
    public static T? FromJsonString<T>(this string json) =>
        JsonSerializer.Deserialize<T>(json, _jsonOptions);

    // ms jsonobject to Json string
    public static string ToJsonString<T>(this T obj) =>
        JsonSerializer.Serialize<T>(obj, _jsonOptions);


}