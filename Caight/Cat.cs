using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Caight
{
    public sealed class Cat
    {
        public const string JsonKeyId = "id";
        public const string JsonKeyColor = "color";
        public const string JsonKeyName = "name";
        public const string JsonKeyBirthday = "birthday";
        public const string JsonKeyGender = "gender";
        public const string JsonKeySpecies = "species";
        public const string JsonKeyWeight = "weights";
        public const string JsonKeyWeightWhen = "when";
        public const string JsonKeyWeightValue = "weight";

        public int Id { get; private set; } = -1;

        public int Color { get; private set; } = -1;

        public string Name { get; private set; } = null;

        public long Birthday { get; private set; } = -1;

        public short Gender { get; private set; } = -1;

        public int Species { get; private set; } = -1;

        public SortedDictionary<long, float> Weights { get; set; } = null;

        private Cat() { }

        public Cat(int id, int color, string name, long birthday, short gender, int species, SortedDictionary<long, float> weights)
        {
            Id = id;
            Color = color;
            Name = name;
            Birthday = birthday;
            Gender = gender;
            Species = species;
            Weights = weights;
        }

        public JObject ToJsonObject()
        {
            JArray weightArray = new JArray();
            foreach (var entry in Weights)
            {
                JObject weight = new JObject();
                weight.Add(JsonKeyWeightWhen, entry.Key);
                weight.Add(JsonKeyWeightValue, entry.Value);

                weightArray.Add(weight);
            }

            JObject json = new JObject()
            {
                { JsonKeyId, Id },
                { JsonKeyColor, Color },
                { JsonKeyName, Name },
                { JsonKeyBirthday, Birthday },
                { JsonKeyGender, Gender },
                { JsonKeySpecies, Species },
                { JsonKeyWeight, weightArray },
            };

            return json;
        }

        public static Cat ParseJson(JObject json)
        {
            JArray weightArray = json.GetValue(JsonKeyWeight).ToObject<JArray>();
            var weights = new SortedDictionary<long, float>();
            foreach (JObject weight in weightArray)
            {
                weights.Add(
                    weight.GetValue(JsonKeyWeightWhen).ToObject<long>(),
                    weight.GetValue(JsonKeyWeightValue).ToObject<float>());
            }

            Cat cat = new Cat();
            cat.Id = json.GetValue(JsonKeyId).ToObject<int>();
            cat.Color = unchecked((int)json.GetValue(JsonKeyColor).ToObject<long>());
            cat.Name = json.GetValue(JsonKeyName).ToObject<string>();
            cat.Birthday = json.GetValue(JsonKeyBirthday).ToObject<long>();
            cat.Gender = json.GetValue(JsonKeyGender).ToObject<short>();
            cat.Species = json.GetValue(JsonKeySpecies).ToObject<int>();
            cat.Weights = weights;

            return cat;
        }
    }
}
