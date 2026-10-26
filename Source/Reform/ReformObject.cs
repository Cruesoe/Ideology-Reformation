using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace IdeologyReformation.Reform;

internal enum ReformObjectKind
{
    Meme,
    Structure,
    Precept,
    Ritual,
    Role,
    Relic,
    Building
}

internal sealed class ReformObject : IEquatable<ReformObject>
{
    public ReformObjectKind Kind { get; }
    public string Key { get; }
    public string Label { get; }

    private ReformObject(ReformObjectKind kind, string key, string label)
    {
        Kind = kind;
        Key = key;
        Label = label;
    }

    public static ReformObject ForPrecept(Precept precept)
    {
        if (precept is Precept_Ritual)
        {
            return new ReformObject(ReformObjectKind.Ritual, $"ritual:{precept.Id}",
                "IR_ObjectRitual".Translate(precept.LabelCap));
        }

        if (precept is Precept_Role)
        {
            return new ReformObject(ReformObjectKind.Role, $"role:{precept.def.defName}",
                "IR_ObjectRole".Translate(precept.LabelCap));
        }

        if (precept is Precept_Relic)
        {
            return new ReformObject(ReformObjectKind.Relic, $"relic:{precept.Id}",
                "IR_ObjectRelic".Translate(precept.LabelCap));
        }

        if (precept is Precept_Building)
        {
            return new ReformObject(ReformObjectKind.Building, $"building:{precept.Id}",
                "IR_ObjectBuilding".Translate(precept.LabelCap));
        }

        IssueDef issue = precept.def.issue;
        string key = issue != null ? issue.defName : precept.def.defName;
        string label = issue != null ? issue.LabelCap.ToString() : precept.UIInfoFirstLine;
        return new ReformObject(ReformObjectKind.Precept, $"precept:{key}",
            "IR_ObjectPrecept".Translate(label));
    }

    public static ReformObject? ForMemeChange(Ideo original, IReadOnlyCollection<MemeDef> proposed)
    {
        MemeDef? oldStructure = original.memes.FirstOrDefault(m => m.category == MemeCategory.Structure);
        MemeDef? newStructure = proposed.FirstOrDefault(m => m.category == MemeCategory.Structure);
        if (oldStructure != newStructure && newStructure != null)
        {
            return new ReformObject(ReformObjectKind.Structure, "structure",
                "IR_ObjectStructure".Translate(newStructure.LabelCap));
        }

        List<MemeDef> added = proposed.Where(m => m.category == MemeCategory.Normal && !original.memes.Contains(m)).ToList();
        List<MemeDef> removed = original.memes.Where(m => m.category == MemeCategory.Normal && !proposed.Contains(m)).ToList();
        if (added.Count + removed.Count != 1)
        {
            return null;
        }

        MemeDef meme = added.Count == 1 ? added[0] : removed[0];
        string action = added.Count == 1 ? "IR_ActionAdd".Translate() : "IR_ActionRemove".Translate();
        return new ReformObject(ReformObjectKind.Meme, $"meme:{meme.defName}",
            "IR_ObjectMeme".Translate(action, meme.LabelCap));
    }

    public bool Equals(ReformObject? other) => other != null && Kind == other.Kind && Key == other.Key;
    public override bool Equals(object? obj) => Equals(obj as ReformObject);
    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Kind * 397) ^ Key.GetHashCode();
        }
    }
}
