using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Discovers semantic templates by stable identifier without knowing concrete specification types.
    /// </summary>
    public interface IShapeTemplateCatalog
    {
        /// <summary>Gets registered templates in stable authoring order.</summary>
        IReadOnlyList<IShapeTemplate> Templates { get; }

        /// <summary>Attempts to resolve one exact template identifier.</summary>
        bool TryGet(string id, out IShapeTemplate template);
    }
}
