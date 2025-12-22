using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIHeartParticle : MonoBehaviour
{
    [SerializeField] public Image img;
    public float moveY = 150f;
    public float duration = 0.6f;

    public void Play()
    {
        StartCoroutine(Anim());
    }

    IEnumerator Anim()
    {
        Vector3 start = transform.localPosition;
        Vector3 end = start + Vector3.up * moveY;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            transform.localPosition = Vector3.Lerp(start, end, t);

            img.color = new Color(1, 1, 1, 1 - t);
            transform.localScale = Vector3.one * (2 + t * 0.3f);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }

}
