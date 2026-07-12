using UnityEngine;

namespace ObliteratusAI.Interactions
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(GameObject interactor);
    }
}
