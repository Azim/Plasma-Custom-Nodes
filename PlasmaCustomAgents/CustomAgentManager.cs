using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Behavior;
using System.Linq;
using System.Collections.Generic;
using System;
using BepInEx;
using BepInEx.Logging;

namespace PlasmaCustomAgents
{
    public class LateGestaltRegistrationException : Exception { }
    public class InsufficientGestaltDataException : Exception
    {
        public InsufficientGestaltDataException(string message) : base(message) { }
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class CustomAgentManager: BaseUnityPlugin
    {
        internal static IEnumerable<AgentGestalt> agentGestalts = Enumerable.Empty<AgentGestalt>();
        internal static bool loadedResources = false;

        internal static Dictionary<string, AgentCategoryEnum> customNodeCategories = new Dictionary<string, AgentCategoryEnum>();
        private static int highestNodeCategoryId = 3;


        public static ManualLogSource mls;

        private void Awake()
        {
            mls = base.Logger;
            mls.LogInfo("Starting initialization of PlasmaCustomAgents");

            Harmony harmony = new Harmony("CustomAgentManager");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (Holder.agentGestalts != null)
            {
                loadedResources = true;
                mls.LogInfo("PlasmaCustomAgents initialized too late and will not work properly");
                this.enabled = false;
            }
            else
            {
                mls.LogInfo("PlasmaCustomAgents initialized successfully");
            }
        }

        public static void RegisterGestalt(AgentGestalt gestalt, string unique_name)
        {
            if (loadedResources)
                throw new LateGestaltRegistrationException();

            gestalt.id = (AgentGestaltEnum)unique_name.GetHashCode() + 1000;
            agentGestalts = agentGestalts.Concat(new[] { gestalt });
        }


        public static AgentGestalt CreateComponentGestalt(GameObject prefab, 
            string displayName, 
            string description = null, 
            AgentGestalt.ComponentCategories category = AgentGestalt.ComponentCategories.Decorative)
        {
            AgentGestalt gestalt = (AgentGestalt)ScriptableObject.CreateInstance(typeof(AgentGestalt));
            gestalt.componentCategory = category;
            gestalt.properties = new Dictionary<int, AgentGestalt.Property>();
            gestalt.ports = new Dictionary<int, AgentGestalt.Port>();
            gestalt.type = AgentGestalt.Types.Component;
            gestalt.componentPrefab = prefab;

            gestalt.agent = null;
            gestalt.displayName = displayName;
            gestalt.description = description??"";
            gestalt.nodeCategory = AgentCategoryEnum.Misc;

            gestalt.componentScaleXLimits = new FloatRange(1, 1);
            gestalt.componentScaleYLimits = new FloatRange(1, 1);
            gestalt.componentScaleZLimits = new FloatRange(1, 1);
            return gestalt;
        }

        public static AgentGestalt CreateNodeGestalt(Type agent, 
            string displayName, 
            string description = null, 
            AgentCategoryEnum category = AgentCategoryEnum.Misc)
        {
            AgentGestalt gestalt = (AgentGestalt)ScriptableObject.CreateInstance(typeof(AgentGestalt));
            gestalt.componentCategory = AgentGestalt.ComponentCategories.Behavior;
            gestalt.properties = new Dictionary<int, AgentGestalt.Property>();
            gestalt.ports = new Dictionary<int, AgentGestalt.Port>();
            gestalt.type = AgentGestalt.Types.Logic;

            gestalt.agent = agent;
            gestalt.displayName = displayName;
            gestalt.description = description??"";
            gestalt.nodeCategory = category;

            return gestalt;
        }
        private static AgentGestalt.Port CreateGenericPort(AgentGestalt gestalt, string name, string description, out int recent_port_dict_id)
        {
            AgentGestalt.Port port = new AgentGestalt.Port();
            int port_dict_id = 1;
            try
            {
                port_dict_id = GetHighestKey(gestalt.ports) + 1;
            }
            catch (Exception) { }

            int position = 1;
            try
            {
                position = gestalt.ports[port_dict_id - 1].position + 1;
            }
            catch (Exception) { }


            port.position = position;
            gestalt.ports.Add(port_dict_id, port);
            port.name = name;
            port.description = description;
            recent_port_dict_id = port_dict_id;
            return port;
        }

        public static AgentGestalt.Port CreateCommandPort(AgentGestalt gestalt, string name, string description, int operation)
        {
            AgentGestalt.Port port = CreateGenericPort(gestalt, name, description, out _);
            port.operation = operation;
            port.type = AgentGestalt.Port.Types.Command;
            return port;
        }

        public static AgentGestalt.Port CreatePropertyPort(AgentGestalt gestalt, string name, string description, Data.Types datatype = Data.Types.None, bool configurable = true, Data defaultData = null, string reference_name = null)
        {
            if (defaultData == null)
            {
                defaultData = new Data();
                defaultData.type = datatype;
            }
            AgentGestalt.Port port = CreateGenericPort(gestalt, name, description, out _);
            AgentGestalt.Property property = new AgentGestalt.Property();
            int property_dict_id = 1;
            try
            {
                property_dict_id = GetHighestKey(gestalt.ports) + 1;
            }
            catch (Exception) { }


            property.position = port.position;
            gestalt.properties.Add(property_dict_id, property);

            if (gestalt.agent.IsSubclassOf(typeof(CustomAgent)))
            {
                if (!CustomAgent.properties.ContainsKey(gestalt.agent))
                {
                    CustomAgent.properties.Add(gestalt.agent, new Dictionary<string, int>());

                }
                CustomAgent.properties[gestalt.agent].Add(reference_name ?? name, property_dict_id);
            }

            property.defaultData = defaultData;
            property.configurable = configurable;
            property.name = name;
            property.description = description;
            port.dataType = datatype;
            port.mappedProperty = property_dict_id;
            port.type = AgentGestalt.Port.Types.Property;
            port.expectsData = true;

            return port;
        }

        private static int GetHighestKey(Dictionary<int, AgentGestalt.Port> l)
        {
            return l.Keys.OrderBy(b => b).DefaultIfEmpty(1).LastOrDefault();
        }

        public static AgentGestalt.Port CreateOutputPort(AgentGestalt gestalt, string name, string description, Data.Types datatype = Data.Types.None, bool configurable = true, Data defaultData = null, string reference_name = null)
        {
            if (defaultData == null)
            {
                defaultData = new Data();
                defaultData.type = datatype;
            }
            AgentGestalt.Port port = CreateGenericPort(gestalt, name, description, out int recent_port_dict_id);
            AgentGestalt.Property property = new AgentGestalt.Property();
            int property_dict_id = 1;
            try
            {
                property_dict_id = GetHighestKey(gestalt.ports) + 1;
            }
            catch (Exception) { }

            property.position = port.position;
            gestalt.properties.Add(property_dict_id, property);
            if (gestalt.agent.IsSubclassOf(typeof(CustomAgent)))
            {
                if (!CustomAgent.outputs.ContainsKey(gestalt.agent))
                {
                    CustomAgent.outputs.Add(gestalt.agent, new Dictionary<string, int>());

                }
                CustomAgent.outputs[gestalt.agent].Add(reference_name ?? name, recent_port_dict_id);
            }
            property.defaultData = defaultData;
            property.configurable = configurable;
            property.name = name;
            property.description = description;
            port.dataType = datatype;
            port.injectedProperty = property_dict_id;
            port.type = AgentGestalt.Port.Types.Output;
            return port;
        }

        public static AgentCategoryEnum CustomNodeCategory(string name)
        {
            name = name.ToUpperInvariant();
            if (customNodeCategories.ContainsKey(name))
                return customNodeCategories[name];

            customNodeCategories.Add(name, (AgentCategoryEnum)(++highestNodeCategoryId));
            return customNodeCategories[name];
        }



        [HarmonyPatch(typeof(Resources), "LoadAll", new Type[] { typeof(string), typeof(Type) })]
        private class LoadResourcesPatch
        {
            public static void Postfix(string path, Type systemTypeInstance, ref UnityEngine.Object[] __result)
            {
                if (path == "Gestalts/Logic Agents" && systemTypeInstance == typeof(AgentGestalt) && !loadedResources)
                {   
                    int size = __result.Length;
                    int newSize = size + agentGestalts.Count();
                    UnityEngine.Object[] temp = new UnityEngine.Object[newSize];
                    __result.CopyTo(temp, 0);
                    agentGestalts.ToArray().CopyTo(temp, size);
                    __result = temp;
                    loadedResources = true;
                }
            }
        }

        [HarmonyPatch(typeof(Visor.ProcessorUICategoryItem), nameof(Visor.ProcessorUICategoryItem.Setup))]
        private class AddCategoryToDictPatch
        {
            static int applied = 0;
            public static void Prefix()
            {
                if (Holder.instance.agentCategories != null && applied < customNodeCategories.Count())
                    foreach (string categoryName in customNodeCategories.Keys)
                    {
                        if (!Holder.instance.agentCategories.ContainsKey(customNodeCategories[categoryName])){
                            applied++;
                            Holder.instance.agentCategories.Add(customNodeCategories[categoryName], categoryName);
                        }
                    }
            }
        }

        [HarmonyPatch(typeof(System.Enum), "GetNames")]
        public class EnumNamePatch
        {
            public static void Postfix(System.Type enumType, ref string[] __result)
            {
                if (enumType == typeof(AgentCategoryEnum))
                {
                    string[] tabs = customNodeCategories.Keys.ToArray();
                    string[] names = new string[__result.Length + tabs.Length];
                    __result.CopyTo(names, 0);
                    tabs.CopyTo(names, __result.Length);
                    __result = names;
                }
            }
        }

        [HarmonyPatch(typeof(System.Enum), "TryParseEnum")]
        public class EnumParsePatch
        {
            public static void Postfix(System.Type enumType, string value, ref object parseResult, ref bool __result)
            {
                if (!__result && enumType == typeof(AgentCategoryEnum) && customNodeCategories.ContainsKey(value))
                {
                    __result = true;
                    System.Type EnumResult = typeof(System.Enum).GetNestedType("EnumResult", BindingFlags.NonPublic);
                    MethodInfo Init = EnumResult.GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo parsedEnum = EnumResult.GetField("parsedEnum", BindingFlags.NonPublic | BindingFlags.Instance);
                    var presult = System.Convert.ChangeType(System.Activator.CreateInstance(EnumResult), EnumResult);
                    Init.Invoke(presult, new object[] { false });
                    parsedEnum.SetValue(presult, customNodeCategories[value]);
                    parseResult = presult;
                }
            }
        }
    }
}
