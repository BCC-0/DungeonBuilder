using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of all BuilderEntity instances in the builder.
/// Used to save/load builder maps without affecting runtime/player entities.
/// </summary>
public static class BuilderRegistry
{
    private static readonly Dictionary<string, BuilderEntity> Builders =
        new Dictionary<string, BuilderEntity>();

    /// <summary>
    /// Registers a builder entity.
    /// </summary>
    /// <param name="entity">The entity to register.</param>
    public static void Register(BuilderEntity entity)
    {
        if (entity == null || string.IsNullOrEmpty(entity.GetUniqueID()))
        {
            return;
        }

        if (!Builders.ContainsKey(entity.GetUniqueID()))
        {
            Builders.Add(entity.GetUniqueID(), entity);
        }
    }

    /// <summary>
    /// Unregisters a builder entity (e.g. on destroy).
    /// </summary>
    /// <param name="entity">The entity to unregister.</param>
    public static void Unregister(BuilderEntity entity)
    {
        if (entity == null || string.IsNullOrEmpty(entity.GetUniqueID()))
        {
            return;
        }

        Builders.Remove(entity.GetUniqueID());
    }

    /// <summary>
    /// Gets a builder entity by ID.
    /// </summary>
    /// <param name="id">The ID of entity this represents.</param>
    /// <returns>Returns the BuilderEntity.</returns>
    public static BuilderEntity GetByID(string id)
    {
        Builders.TryGetValue(id, out BuilderEntity entity);
        return entity;
    }

    /// <summary>
    /// Returns all registered builder entities that are not deleted.
    /// Entities that were deleted in the builder are only deactivated (so undo
    /// can bring them back) and are left out, so they are not hit, referenced
    /// or saved.
    /// </summary>
    /// <returns>The list of Builder entities this registry has registered.</returns>
    public static List<BuilderEntity> GetAll()
    {
        List<BuilderEntity> result = new List<BuilderEntity>(Builders.Count);

        foreach (BuilderEntity entity in Builders.Values)
        {
            // activeSelf, not activeInHierarchy: a hidden parent must not
            // make every entity look deleted.
            if (entity != null &&
                entity.gameObject.activeSelf)
            {
                result.Add(entity);
            }
        }

        return result;
    }

    /// <summary>
    /// Clears all builder entities.
    /// </summary>
    public static void Clear()
    {
        Builders.Clear();
    }
}
