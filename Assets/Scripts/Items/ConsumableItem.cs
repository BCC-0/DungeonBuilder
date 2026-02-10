
/// <summary>
/// 
/// </summary>
public abstract class ConsumableItem : Item
{
    // Optional shared properties for all consumables can go here
    // e.g., public int maxStackSize = 1;

    // Subclasses have to implement their own use behavior
    public abstract override void Use();
}
