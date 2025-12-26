using UnityEngine;
using UnityEngine.UI;

public class GateAvatarMarker : MonoBehaviour
{
    [Header("UI")]
    public Image avatarImage;
    public Transform billboardRoot;
    public Vector3 localOffset = new Vector3(0, 1.2f, 0);

    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (billboardRoot == null) billboardRoot = transform;
        transform.localPosition = localOffset;
    }

    public void SetAvatar(Sprite sprite)
    {
        if (avatarImage != null) avatarImage.sprite = sprite;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || billboardRoot == null) return;

        Vector3 dir = billboardRoot.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;

        billboardRoot.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
