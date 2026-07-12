using UnityEngine;

namespace ObliteratusAI.Interactions
{
    public sealed class PrototypeToggleInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Light targetLight;
        [SerializeField] private Color activeColor = new Color(1f, 0.28f, 0.45f);
        [SerializeField] private Color inactiveColor = new Color(0.12f, 0.16f, 0.2f);

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock propertyBlock;
        private bool isActive = true;

        public string InteractionPrompt => isActive ? "Switch light off" : "Switch light on";

        public void Configure(Renderer rendererToControl, Light lightToControl)
        {
            targetRenderer = rendererToControl;
            targetLight = lightToControl;
            ApplyState();
        }

        public void Interact(GameObject interactor)
        {
            isActive = !isActive;
            ApplyState();
        }

        private void Awake()
        {
            ApplyState();
        }

        private void ApplyState()
        {
            if (targetLight != null)
            {
                targetLight.enabled = isActive;
            }

            if (targetRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            Color color = isActive ? activeColor : inactiveColor;
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColor, color);
            propertyBlock.SetColor(EmissionColor, isActive ? color * 2f : Color.black);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
