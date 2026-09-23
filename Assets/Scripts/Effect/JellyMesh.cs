using UnityEngine;

public class JellyMesh : MonoBehaviour
{
    public float Intensity = 1f;
    public float Mass = 1f;
    public float stiffness = 1f;
    public float damping = 0.75f;

    private Mesh OriginalMesh, MeshClone;
    private MeshRenderer meshRenderer;
    private JellyVertex[] jv;
    private Vector3[] vertexArray;

    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        OriginalMesh = meshFilter.sharedMesh;

        // TẠO MESH MỚI HOÀN TOÀN ĐỂ MỞ QUYỀN GHI (READ/WRITE)
        MeshClone = new Mesh();
        MeshClone.name = OriginalMesh.name + "_JellyClone";

        // Sao chép toàn bộ dữ liệu từ OriginalMesh sang MeshClone
        MeshClone.vertices = OriginalMesh.vertices;
        MeshClone.triangles = OriginalMesh.triangles;
        MeshClone.uv = OriginalMesh.uv;
        MeshClone.normals = OriginalMesh.normals;
        MeshClone.tangents = OriginalMesh.tangents;

        // Gán Mesh Clone mới có quyền ghi vào MeshFilter
        meshFilter.mesh = MeshClone;
        meshRenderer = GetComponent<MeshRenderer>();

        // Khởi tạo các đỉnh biến dạng
        jv = new JellyVertex[MeshClone.vertices.Length];
        for (int i = 0; i < MeshClone.vertices.Length; i++)
        {
            jv[i] = new JellyVertex(i, transform.TransformPoint(MeshClone.vertices[i]));
        }
    }

    private void FixedUpdate()
    {
        if (OriginalMesh == null || jv == null) return;

        vertexArray = OriginalMesh.vertices;
        Bounds bounds = meshRenderer.bounds; // Lấy bounds hiện tại

        for (int i = 0; i < jv.Length; i++)
        {
            Vector3 target = transform.TransformPoint(vertexArray[jv[i].ID]);

            // Tính độ ảnh hưởng biến dạng dựa trên độ cao Y
            float intensity = (1f - (bounds.max.y - target.y) / bounds.size.y) * Intensity;

            // Cập nhật vị trí đỉnh theo vật lý lò xo
            jv[i].Shake(target, Mass, stiffness, damping);

            Vector3 localPos = transform.InverseTransformPoint(jv[i].Position);
            vertexArray[jv[i].ID] = Vector3.Lerp(vertexArray[jv[i].ID], localPos, intensity);
        }

        // Gán lại đỉnh cho Mesh
        MeshClone.vertices = vertexArray;

        // BẮT BUỘC: Tính lại ánh sáng và viền bóng cho các góc bo tròn khi rung rinh
        MeshClone.RecalculateNormals();
        MeshClone.RecalculateBounds();
    }

    public class JellyVertex
    {
        public int ID;
        public Vector3 Position;
        public Vector3 Velocity, Force;

        public JellyVertex(int _id, Vector3 _pos)
        {
            ID = _id;
            Position = _pos;
        }

        public void Shake(Vector3 target, float m, float s, float d)
        {
            Force = (target - Position) * s;
            Velocity = (Velocity + Force / m) * d;
            Position += Velocity;

            if ((Velocity + Force / m).magnitude < 0.001f)
            {
                Position = target;
            }
        }
    }
}