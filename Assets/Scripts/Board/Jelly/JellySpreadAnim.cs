using DG.Tweening;
using System;
using UnityEngine;

public class JellySpreadAnim : MonoBehaviour
{
    [SerializeField] private GameObject morpVFX;

    public void Awake()
    {
          
    }
    public void MorphSingleToFull(JellySingleBlock sourceSingle, Action<JellyBlockBase> onComplete = null)
    {
        Vector3 centrerpos = Vector3.zero;

        Vector3 fullScale = Vector3.one;

        Sequence seq = DOTween.Sequence();

        seq.Join(sourceSingle.transform.DOMove(centrerpos, 0.18f).SetEase(Ease.InQuad));
        seq.Join(sourceSingle.transform.DOScale(fullScale,0.18f).SetEase(Ease.InQuad));


        seq.OnComplete(() =>
        {
            PlayMorphVFX(centrerpos);
        });

    }
    public void MorphSingleToDouble(
      JellySingleBlock oldSingle, JellyBlockBase newDouble, Vector2Int sourceCoord, Vector2Int targetCoord)
    {
        if (oldSingle == null) return;

        bool isHorizontal = (sourceCoord.x != targetCoord.x);

        // Tính scale dãn cho thằng cũ
        Vector3 stretchScale = oldSingle.transform.localScale;
        if (isHorizontal) stretchScale.x *= 2f; else stretchScale.y *= 2f;

        // Thằng cũ dãn ra rồi biến mất, nhường lại khối newDouble đã nằm sẵn ở đó
        oldSingle.transform.DOKill();
        oldSingle.transform.DOScale(stretchScale, 0.15f).OnComplete(() =>
        {
            Destroy(oldSingle.gameObject);
        });
    }
    public void MorphDoubleToFull(
    JellyDoubleBlock sourceDouble,
    Action<JellyBlockBase> onComplete = null)
    {
       
        Vector3 currentPos = sourceDouble.transform.localPosition;

        Vector3 targetScale = Vector3.one;
        Vector3 targetPos = Vector3.zero;

        Transform parentTransform = sourceDouble.transform.parent;

        // Chạy Tween phình to lấp đầy ô 1x1
        Sequence seq = DOTween.Sequence();
        seq.Join(sourceDouble.transform.DOScale(targetScale, 0.18f).SetEase(Ease.InQuad));
        seq.Join(sourceDouble.transform.DOMove(targetPos,0.18f).SetEase(Ease.InQuad));   
        seq.OnComplete(() =>
        {
            PlayMorphVFX(targetPos);
        });
    }

    public void PlayMorphVFX(Vector3 worldPosition)
    {
        if (morpVFX == null) return;

        // 1. Init VFX tại vị trí World
        GameObject vfxInstance = Instantiate(morpVFX, worldPosition, Quaternion.identity);

        // 2. Tính toán thời gian sống dựa trên Particle System (nếu có)
        float destroyDelay = 1.5f; // Thời gian mặc định an toàn

        if (vfxInstance.TryGetComponent<ParticleSystem>(out var ps))
        {
            // Thời gian = Độ dài animation + thời gian sống tối đa của hạt
            destroyDelay = ps.main.duration + ps.main.startLifetime.constantMax;
        }

        // 3. Hủy GameObject VFX sau khi chạy xong
        Destroy(vfxInstance, destroyDelay);
    }
}
