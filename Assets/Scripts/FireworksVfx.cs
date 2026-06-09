using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FireworksVfx : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private int burstCount = 7;
    [SerializeField] private int particlesPerBurst = 30;
    [SerializeField] private float burstRadius = 260f;
    [SerializeField] private float particleSize = 24f;
    [SerializeField] private float lifetime = 0.75f;
    [SerializeField] private float delayBetweenBursts = 0.18f;

    private readonly Color[] colors =
    {
        new Color(1f, 0.25f, 0.25f),
        new Color(1f, 0.85f, 0.15f),
        new Color(0.2f, 0.8f, 1f),
        new Color(0.45f, 1f, 0.35f),
        new Color(1f, 0.35f, 0.9f)
    };

    private readonly Vector2[] burstPoints =
    {
        new Vector2(0.15f, 0.80f),
        new Vector2(0.85f, 0.78f),
        new Vector2(0.50f, 0.60f),
        new Vector2(0.20f, 0.35f),
        new Vector2(0.80f, 0.32f),
        new Vector2(0.35f, 0.68f),
        new Vector2(0.65f, 0.48f)
    };

    private void Awake()
    {
        if (root == null)
            root = GetComponent<RectTransform>();
    }

    public void Play()
    {
        gameObject.SetActive(true);
        root.SetAsLastSibling();

        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        for (int i = 0; i < burstCount; i++)
        {
            Vector2 point = burstPoints[i % burstPoints.Length];
            Vector2 randomOffset = new Vector2(Random.Range(-0.08f, 0.08f), Random.Range(-0.06f, 0.06f));

            SpawnBurst(NormalizedToLocalPoint(point + randomOffset));

            yield return new WaitForSeconds(delayBetweenBursts);
        }
    }

    private Vector2 NormalizedToLocalPoint(Vector2 point)
    {
        float x = Mathf.Lerp(-root.rect.width * 0.5f, root.rect.width * 0.5f, point.x);
        float y = Mathf.Lerp(-root.rect.height * 0.5f, root.rect.height * 0.5f, point.y);

        return new Vector2(x, y);
    }

    private void SpawnBurst(Vector2 center)
    {
        for (int i = 0; i < particlesPerBurst; i++)
        {
            GameObject particle = new GameObject("FireworkParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            particle.transform.SetParent(root, false);

            RectTransform rect = particle.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = new Vector2(particleSize, particleSize);

            Image image = particle.GetComponent<Image>();
            image.color = colors[Random.Range(0, colors.Length)];
            image.raycastTarget = false;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(burstRadius * 0.45f, burstRadius);
            Vector2 target = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            StartCoroutine(AnimateParticle(rect, image, target));
        }
    }

    private IEnumerator AnimateParticle(RectTransform rect, Image image, Vector2 target)
    {
        Vector2 start = rect.anchoredPosition;
        Vector2 control = (start + target) * 0.5f + Vector2.up * Random.Range(40f, 120f);

        float timer = 0f;

        while (timer < lifetime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / lifetime);
            Vector2 a = Vector2.Lerp(start, control, t);
            Vector2 b = Vector2.Lerp(control, target, t);

            rect.anchoredPosition = Vector2.Lerp(a, b, t);
            rect.localRotation = Quaternion.Euler(0f, 0f, t * 360f);
            rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.25f, t);

            Color color = image.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            image.color = color;

            yield return null;
        }

        Destroy(rect.gameObject);
    }
}