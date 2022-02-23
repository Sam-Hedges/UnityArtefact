using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] private Shader outlineShader;
    private MeshRenderer meshRenderer;
    private int outlineMat;
    private float outlineSize;
    private static readonly int Size = Shader.PropertyToID("Size");

    void Awake() {
        // Set Mesh Renderer Component
        meshRenderer = GetComponent<MeshRenderer>();
        
        // For each material attached to renderer check if it is an outline
        // and save material index if so
        for (int i = 0; i < meshRenderer.materials.Length; i++) {
            if (meshRenderer.materials[i].shader == outlineShader) {
                outlineMat = i;
                outlineSize = meshRenderer.materials[i].GetFloat(Size);
                
                // Start the material unselected
                meshRenderer.materials[outlineMat].SetFloat(Size, 0);
            }
        }
    }

    public void ToggleOutline(bool b) {
        if (b) {
            meshRenderer.materials[outlineMat].SetFloat(Size, 0);
        }
        else {
            meshRenderer.materials[outlineMat].SetFloat(Size, outlineSize);
        }
    }
}
