using DG.Tweening;
using System;
using UnityEngine;

public class JellySpreadAnim : MonoBehaviour
{
   
    private float effectDuration = 0.3f;

    public void MorphSingleToDouble(
     JellySingleBlock oldSingle, JellyBlockBase newDouble, Vector2Int sourceCoord, Vector2Int targetCoord, Action onComplete)
    {
        if (oldSingle == null || oldSingle.gameObject == null)
        {
            onComplete?.Invoke();
            return;
        }

        SetBlockVisualVisible(newDouble, false);

        bool isHorizontal = (sourceCoord.x != targetCoord.x);

        Vector3 stretchScale = oldSingle.transform.localScale;
        if (isHorizontal) stretchScale.x *= 2f; else stretchScale.z *= 2f;

        // 1. Dừng mọi Tween cũ đang chạy trên oldSingle
        oldSingle.transform.DOKill();

        // 2. Chạy DOScale kèm SetLink -> Nếu oldSingle bị Destroy bất ngờ ở đâu đó, Tween tự ngắt ngay
        oldSingle.transform.DOScale(stretchScale, effectDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(oldSingle.gameObject, LinkBehaviour.KillOnDestroy)
            .OnComplete(() =>
            {
                SetBlockVisualVisible(newDouble, true);
                if (oldSingle != null && oldSingle.gameObject != null)
                {
                    Destroy(oldSingle.gameObject);
                }
                onComplete?.Invoke();
            });
    }

    public void MorphDoubleToFull(
        JellyDoubleBlock sourceDouble, JellyBlockBase newBlock, Action onComplete)
    {
        if (sourceDouble == null || sourceDouble.gameObject == null)
        {
            onComplete?.Invoke();
            return;
        }
        SetBlockVisualVisible (newBlock, false);

        sourceDouble.transform.DOKill();

        Vector3 targetScale = Vector3.one;
        Vector3 targetPos = Vector3.zero;
        Vector3 worldVfxPos = sourceDouble.transform.position;

        // Tạo các Tweener độc lập thay vì Sequence để SetLink hoạt động chính xác nhất
        Tweener scaleTween = sourceDouble.transform.DOScale(targetScale, effectDuration)
            .SetEase(Ease.InQuad)
            .SetLink(sourceDouble.gameObject, LinkBehaviour.KillOnDestroy);

        sourceDouble.transform.DOLocalMove(targetPos, effectDuration)
            .SetEase(Ease.InQuad)
            .SetLink(sourceDouble.gameObject, LinkBehaviour.KillOnDestroy);

        // Bắt sự kiện OnComplete từ scaleTween
        scaleTween.OnComplete(() =>
        {
           
            SetBlockVisualVisible(newBlock, true);

            if (sourceDouble != null && sourceDouble.gameObject != null)
            {
                Destroy(sourceDouble.gameObject);
            }
            onComplete?.Invoke();
        });
    }

   
    private void SetBlockVisualVisible(JellyBlockBase block, bool isVisible)
    {
        if (block == null || block.gameObject == null) return;

        
        Renderer[] renderers = block.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            rend.enabled = isVisible;
        }
    }
}