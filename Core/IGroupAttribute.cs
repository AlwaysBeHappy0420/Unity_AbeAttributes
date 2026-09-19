using UnityEngine;

namespace AbeAttributes
{
    public interface IGroupAttribute
    : IAbeAttribute
    {
        string Name { get; }
    }
}
