using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class AttributeValue
{
    public string Key { get; set; }
    public string Type { get; set; }

    private object value;

    [JsonProperty("value")]
    public object Value
    {
        get => Type == "AttributeSet" ? AttributeSetValue : value;
        set
        {
            if (Type == "AttributeSet" && value is JArray jArray)
            {
                // JSON配列をList<AttributeValue>に変換
                AttributeSetValue = jArray.ToObject<List<AttributeValue>>();
            }
            else
            {
                this.value = value;
            }
        }
    }

    // "type": "AttributeSet"の場合にデシリアライズされるプロパティ
    [JsonIgnore] // このプロパティはJSONに含めない
    public List<AttributeValue> AttributeSetValue { get; private set; }
}

public class RootObject
{
    public string GmlID { get; set; }
    public List<int> CityObjectIndex { get; set; }
    public string CityObjectType { get; set; }
    public List<AttributeValue> Attributes { get; set; }
}
