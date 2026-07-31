using UnityEngine;

/// <summary>
/// Swap 1 model Dungeon Mason vào modelSocket và điều khiển Animator của nó
/// bằng đúng tên state chuẩn (Die, WalkFWD, GetHit, IdleNormal...) - nên pack
/// mới sau này cũng chạy được luôn mà không cần sửa code.
/// </summary>
public class EnemyModel : MonoBehaviour
{
    [Tooltip("Empty child transform để chứa model quái được instantiate vào")]
    public Transform modelSocket;

    private Animator animator;
    private GameObject currentModel;

    public void SetModel(GameObject modelPrefab)
    {
        if (currentModel != null) Destroy(currentModel);
        currentModel = Instantiate(modelPrefab, modelSocket.position, modelSocket.rotation, modelSocket);
        animator = currentModel.GetComponentInChildren<Animator>();
    }

    public void PlayIdle() => PlayState("IdleNormal");
    public void PlayWalk() => PlayState("WalkFWD");
    public void PlayHit() => PlayState("GetHit");
    public void PlayDie() => PlayState("Die");

    private void PlayState(string stateName)
    {
        if (animator == null) return;
        int hash = Animator.StringToHash(stateName);
        if (animator.HasState(0, hash)) // an toàn nếu con nào đó thiếu state này
            animator.Play(hash);
    }
}