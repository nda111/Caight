using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Caight
{
    public sealed class CatGroup
    {
        public const string __JSON_KEY_GROUP_ID__ = "group_id";
        public const string __JSON_KEY_GROUP_NAME__ = "group_name";
        public const string __JSON_KEY_GROUP_OWNER__ = "group_owner";

        public int Id { get; private set; } = -1;

        public string Name { get; private set; } = null;

        public string Owner { get; private set; } = null;

        private CatGroup() { }

        public CatGroup(int id, string name, string owner)
        {
            Id = id;
            Name = name;
            Owner = owner;
        }

        public JObject ToJsonObject()
        {
            JObject json = new JObject();

            json.Add(__JSON_KEY_GROUP_ID__, Id);
            json.Add(__JSON_KEY_GROUP_NAME__, Name);
            json.Add(__JSON_KEY_GROUP_OWNER__, Owner);

            return json;
        }
        public static CatGroup parseJson(JObject json)
        {
            CatGroup group = new CatGroup();
            group.Id = json.GetValue(__JSON_KEY_GROUP_ID__).ToObject<int>();
            group.Name = json.GetValue(__JSON_KEY_GROUP_NAME__).ToObject<string>();
            group.Owner = json.GetValue(__JSON_KEY_GROUP_OWNER__).ToObject<string>();

            return group;
        }
    }
}
