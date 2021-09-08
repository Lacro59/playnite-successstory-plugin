using CommonPlayniteShared.Manifests;
using CommonPluginsPlaynite.API;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace CommonPluginsPlaynite.API
{
    // TODO Used?
    //public class ScriptFunctionDescription
    //{
    //    public string Description { get; set; }
    //    public string FunctionName { get; set; }
    //}
    //
    //public class ScriptExtensionDescription : ExtensionManifest
    //{
    //    public List<ScriptFunctionDescription> Functions { get; set; }
    //
    //    public ScriptExtensionDescription()
    //    {
    //    }
    //
    //    public new static ScriptExtensionDescription FromFile(string descriptorPath)
    //    {
    //        var deserializer = new DeserializerBuilder().Build();
    //        var description = deserializer.Deserialize<ScriptExtensionDescription>(File.ReadAllText(descriptorPath));
    //        description.DescriptionPath = descriptorPath;
    //        description.DirectoryName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(descriptorPath));
    //        return description;
    //    }
    //}
}
