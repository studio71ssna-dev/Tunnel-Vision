public static class ToolRules
{
    public static bool CanCollect(ElementType loot, ToolType tool)
    {
        return loot switch
        {
            ElementType.Fire => tool == ToolType.FireExtractor,
            ElementType.Water => tool == ToolType.WaterExtractor,
            ElementType.Poison => tool == ToolType.PoisonExtractor,
            _ => false
        };
    }
}