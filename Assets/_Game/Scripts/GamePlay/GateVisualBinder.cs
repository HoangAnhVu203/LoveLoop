using UnityEngine;

public class GateVisualBinder : MonoBehaviour
{
    [Header("Where to attach the 3D model")]
    [SerializeField] Transform visualRoot;

    [Header("Optional transform offset")]
    [SerializeField] Vector3 localPos;
    [SerializeField] Vector3 localEuler;
    [SerializeField] Vector3 localScale = Vector3.one;

    GameObject currentModel;

    void Awake()
    {
        if (visualRoot == null) visualRoot = transform;
    }

    public void AttachModel(GameObject modelPrefab)
    {
        if (modelPrefab == null) return;

        if (currentModel != null)
            Destroy(currentModel);

        currentModel = Instantiate(modelPrefab, visualRoot);
        currentModel.transform.localPosition = localPos;
        currentModel.transform.localRotation = Quaternion.Euler(localEuler);
        currentModel.transform.localScale = localScale;

        var animator = currentModel.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.enabled = true;
            animator.Play(0, 0, 0f);
            animator.Update(0f);
        }
    }
}
