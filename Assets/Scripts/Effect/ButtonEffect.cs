using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Animated Button")]
public class ButtonEffect : Button
{
    [Header("Scale Click Effect")]
    [Tooltip("Tỉ lệ phóng to so với kích thước gốc khi vừa ấn (VD: 1.15 = phóng to 15%)")]
    [SerializeField] private float scaleUpMultiplier = 1.15f;

    [Tooltip("Thời gian phóng to (giây)")]
    [SerializeField] private float scaleUpDuration = 0.08f;

    [Tooltip("Thời gian thu nhỏ về lại kích thước gốc (giây)")]
    [SerializeField] private float scaleDownDuration = 0.12f;

    [SerializeField] private Ease scaleUpEase = Ease.OutQuad;
    [SerializeField] private Ease scaleDownEase = Ease.OutBack;

    [Tooltip("Cho phép hiệu ứng chạy kể cả khi Time.timeScale = 0 (VD: menu Pause)")]
    [SerializeField] private bool useUnscaledTime = true;

    private Vector3 originalScale;
    private Sequence clickSequence;


    protected override void Awake()
    {
        base.Awake();
        originalScale = transform.localScale;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!IsActive() || !IsInteractable()) return;

        PlayClickEffect();
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        if (!IsActive() || !IsInteractable()) return;

        PlayClickEffect();

        base.OnSubmit(eventData);
    }

    private void PlayClickEffect()
    {
        // Dừng Sequence cũ nếu đang chạy và trả về scale gốc
        if (clickSequence != null && clickSequence.IsActive())
        {
            clickSequence.Kill();
            transform.localScale = originalScale;
        }

       
        onClick.Invoke();

       
        if (!gameObject.activeInHierarchy)
        {
            return;
        }



        clickSequence = DOTween.Sequence()
            .Append(transform.DOScale(originalScale * scaleUpMultiplier, scaleUpDuration).SetEase(scaleUpEase))
            .Append(transform.DOScale(originalScale, scaleDownDuration).SetEase(scaleDownEase))
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject)
            .OnComplete(() =>
            {

            });
    }

    protected override void OnDisable()
    {
        
        clickSequence?.Kill();
        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
        }

        base.OnDisable();
    }
}
