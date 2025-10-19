using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput = Vector2.zero;
    private Animator animator;

    [Header("Interacción")]
    public GameObject interactionZone;  // si lo usas

    [Header("UI Mensajes")]
    public GameObject speechBubble;        // Imagen PNG de la burbuja (en Canvas World Space)
    public TextMeshProUGUI infoText;       // Texto dentro de la burbuja
    public Vector3 bubbleOffset = new Vector3(5f, 3.5f, 0f); // offset sobre la cabeza (ajusta)

    private TrashItem currentTrash = null;
    private TrashItem nearbyTrash = null;

    // ⏳ Control de mensajes
    private float messageTimer = 0f;
    private string currentMessage = "";

    [Header("Temporizador")]
    public TextMeshProUGUI timerText;
    private float timeRemaining = 300f; // 5 minutos
    private bool gameEnded = false;


    // cámara cacheada
    Camera mainCam;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb != null) rb.freezeRotation = true;

        mainCam = Camera.main;

        ShowMessage("", 0);

        if (speechBubble != null) speechBubble.SetActive(false);
    }

    void Update()
    {
        if (gameEnded) return;

        // --- Movimiento WASD + animaciones ---
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        if (animator != null)
        {
            animator.SetFloat("Horizontal", moveInput.x);
            animator.SetFloat("Vertical", moveInput.y);
            animator.SetFloat("Speed", moveInput.magnitude);
        }

        // --- Interacción ---
        if (nearbyTrash != null && currentTrash == null)
        {
            if (messageTimer <= 0) ShowMessage($"{nearbyTrash.itemName}", 0);

            if (Input.GetKeyDown(KeyCode.E))
            {
                PickUpTrash(nearbyTrash);
            }
        }
        else if (currentTrash != null)
        {
            if (messageTimer <= 0) ShowMessage($"Llevas: {currentTrash.itemName}", 0);

            if (Input.GetKeyDown(KeyCode.R))
            {
                TryThrowTrash();
            }
        }
        else
        {
            if (messageTimer <= 0) HideMessage();
        }

        // 🕒 Mensajes temporales
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0) HideMessage();
        }

        // 🕒 Actualizar temporizador
        UpdateTimer();
    }

    void FixedUpdate()
    {
        if (rb != null && !gameEnded)
        {
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // ===== Interacción por trigger (si usas collider trigger en InteractionZone hijo) =====
    void OnTriggerEnter2D(Collider2D other)
    {
        TrashItem trash = other.GetComponent<TrashItem>();
        if (trash != null && trash.gameObject.activeSelf)
        {
            nearbyTrash = trash;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        TrashItem trash = other.GetComponent<TrashItem>();
        if (trash != null && nearbyTrash == trash)
        {
            nearbyTrash = null;
        }
    }

    void PickUpTrash(TrashItem trash)
    {
        if (trash == null) return;
        currentTrash = trash;
        trash.gameObject.SetActive(false);
    }

    void TryThrowTrash()
    {
        if (currentTrash == null)
        {
            ShowMessage("No llevas basura", 3);
            return;
        }

        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 1.5f); // rango para detectar caneca
        foreach (Collider2D col in cols)
        {
            TrashBin bin = col.GetComponent<TrashBin>();
            if (bin != null)
            {
                if (bin.binType == currentTrash.type)
                {
                    ShowMessage(":D Correcto!", 3);
                }
                else
                {
                    ShowMessage("D: CANECA EQUIVOCADA (-10s)", 3);
                    timeRemaining -= 10f;
                    if (timeRemaining < 0) timeRemaining = 0;
                }

                Destroy(currentTrash.gameObject);
                currentTrash = null;
                return;
            }
        }

        ShowMessage("No estás cerca de ninguna caneca", 3);
    }

    // 📌 Mostrar mensaje
    void ShowMessage(string message, float duration)
    {
        if (infoText != null && speechBubble != null)
        {
            speechBubble.SetActive(true);
            infoText.text = message;
            currentMessage = message;
            messageTimer = duration;
        }
    }

    // 📌 Ocultar mensaje
    void HideMessage()
    {
        if (speechBubble != null) speechBubble.SetActive(false);
        if (infoText != null) infoText.text = "";
    }

    // 🕒 Actualizar temporizador
    void UpdateTimer()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0) timeRemaining = 0;
        }
        else
        {
            EndGame();
        }

        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        if (timerText != null) timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void EndGame()
    {
        gameEnded = true;
        ShowMessage(" ¡Se acabó el tiempo!", 10);
    }

    // ============================
    // Aquí hacemos el CLAMP de la burbuja para que no salga de la cámara
    // ============================
    void LateUpdate()
    {
        if (speechBubble == null) return;
        if (!speechBubble.activeSelf) return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // posición objetivo (mundo) sobre la cabeza del jugador + offset configurable
        Vector3 targetWorldPos = transform.position + bubbleOffset;

        // Si la cámara es ortográfica (2D típico) usamos world-clamp que es más preciso
        if (mainCam.orthographic)
        {
            // medio tamaño de la cámara en world units
            float halfHeight = mainCam.orthographicSize;
            float halfWidth = halfHeight * mainCam.aspect;

            // obtener tamaño de la burbuja en unidades mundo
            RectTransform rt = speechBubble.GetComponent<RectTransform>();
            Vector2 rectSize = rt.rect.size;
            Vector3 lossy = rt.lossyScale;

            float bubbleWorldHalfWidth = (rectSize.x * Mathf.Abs(lossy.x)) / 2f;
            float bubbleWorldHalfHeight = (rectSize.y * Mathf.Abs(lossy.y)) / 2f;

            // límites de la cámara en world coords
            Vector3 camPos = mainCam.transform.position;
            float minX = camPos.x - halfWidth + bubbleWorldHalfWidth;
            float maxX = camPos.x + halfWidth - bubbleWorldHalfWidth;
            float minY = camPos.y - halfHeight + bubbleWorldHalfHeight;
            float maxY = camPos.y + halfHeight - bubbleWorldHalfHeight;

            // clamp
            Vector3 clamped = targetWorldPos;
            clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
            clamped.y = Mathf.Clamp(clamped.y, minY, maxY);
            clamped.z = speechBubble.transform.position.z;

            speechBubble.transform.position = clamped;
        }
        else
        {
            // Fallback para cámara perspectiva: clamp en viewport
            Vector3 viewport = mainCam.WorldToViewportPoint(targetWorldPos);
            float padding = 0.05f; // 5% margen
            viewport.x = Mathf.Clamp(viewport.x, padding, 1f - padding);
            viewport.y = Mathf.Clamp(viewport.y, padding, 1f - padding);

            Vector3 speechWorld = speechBubble.transform.position;
            float desiredZ = mainCam.WorldToViewportPoint(speechWorld).z;
            viewport.z = desiredZ;

            Vector3 newWorld = mainCam.ViewportToWorldPoint(viewport);
            speechBubble.transform.position = new Vector3(newWorld.x, newWorld.y, speechBubble.transform.position.z);
        }
    }

    // opcional: para ver el radio de interacción en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // si usas interactionZone como trigger, no dibujamos; si quieres dibujar algo, agrega aquí
    }
}
