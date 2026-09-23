using System;
using UnityEngine;

public class InputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask blockLayer; // Layer của các khối Block
    [SerializeField] private GridSystem gridSystem;



    // Event bắn lên khi người dùng chọn/kéo một Block cụ thể
    public static event Action<Block> OnBlockSelected;



    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Kiểm tra tương tác Click chuột hoặc Chạm màn hình (Touch)
        if (Input.GetMouseButtonDown(0))
        {
            ProcessTap(Input.mousePosition);
        }
    }

    private void ProcessTap(Vector3 screenPosition)
    {
       
    }

}