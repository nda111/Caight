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
        public const string JsonKeyGroupId = "group_id";
        public const string JsonKeyGroupName = "group_name";
        public const string JsonKeyGroupOwner = "group_owner";
        public const string JsonKeyGroupLocked = "group_locked";

        public int Id { get; private set; } = -1;

        public string Name { get; private set; } = null;

        public string Owner { get; private set; } = null;

        public bool Locked { get; private set; } = false;

        private CatGroup() { }

        public CatGroup(int id, string name, string owner, bool locked)
        {
            Id = id;
            Name = name;
            Owner = owner;
            Locked = locked;
        }

        public JObject ToJsonObject()
        {
            JObject json = new JObject();

            json.Add(JsonKeyGroupId, Id);
            json.Add(JsonKeyGroupName, Name);
            json.Add(JsonKeyGroupOwner, Owner);
            json.Add(JsonKeyGroupLocked, Locked);

            return json;
        }
        public static CatGroup parseJson(JObject json)
        {
            CatGroup group = new CatGroup();
            group.Id = json.GetValue(JsonKeyGroupId).ToObject<int>();
            group.Name = json.GetValue(JsonKeyGroupName).ToObject<string>();
            group.Owner = json.GetValue(JsonKeyGroupOwner).ToObject<string>();
            group.Locked = json.GetValue(JsonKeyGroupLocked).ToObject<bool>();

            return group;
        }
    }
}
