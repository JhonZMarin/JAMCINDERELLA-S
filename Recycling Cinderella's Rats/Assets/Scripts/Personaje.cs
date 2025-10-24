using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput = Vector2.zero;
    private Animator animator;

    [Header("Interacción")]
    public GameObject interactionZone;

    [Header("UI Mensajes")]
    public GameObject speechBubble;
    public TextMeshProUGUI infoText;
    public Vector3 bubbleOffset = new Vector3(5f, 3.5f, 0f);

    private TrashItem currentTrash = null;
    private TrashItem nearbyTrash = null;

    // ⏳ Control de mensajes
    private float messageTimer = 0f;
    private string currentMessage = "";

    [Header("Temporizador")]
    public TextMeshProUGUI timerText;
    private float timeRemaining = 150f;
    private bool gameEnded = false;

    [Header("Audio General")]
    public AudioSource audioSource;
    public AudioClip penaltyClip;

    [Header("Audio - Pasos")]
    public AudioSource footstepsSource;
    public AudioClip footstepClip;
    public float stepInterval = 0.4f;
    private float stepTimer = 0f;

    [Header("Audio - Basura")]
    public AudioClip throwTrashClip;
    public AudioClip pickUpTrashClip;

    // cámara cacheada
    Camera mainCam;

    // ============================
    // NUEVO: Contador de desechos
    // ============================
    private int totalTrashCount;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb != null) rb.freezeRotation = true;

        mainCam = Camera.main;

        ShowMessage("", 0);

        if (speechBubble != null) speechBubble.SetActive(false);

        // Contar todos los desechos al inicio
        totalTrashCount = FindObjectsOfType<TrashItem>().Length;
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

        // --- Sonido de pasos ---
        HandleFootsteps();

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
            if (messageTimer <= 0) ShowMessage($" {currentTrash.itemName}", 0);

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

    // ===== Interacción por trigger =====
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

        if (audioSource != null && pickUpTrashClip != null)
        {
            audioSource.PlayOneShot(pickUpTrashClip);
        }
    }

    void TryThrowTrash()
    {
        if (currentTrash == null)
        {
            ShowMessage(" Acá No -_-", 2);
            return;
        }

        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D col in cols)
        {
            TrashBin bin = col.GetComponent<TrashBin>();
            if (bin != null)
            {
                if (bin.binType == currentTrash.type)
                {
                    ShowMessage(":) Correcto!", 2);
                }
                else
                {
                    ShowMessage("): CANECA EQUIVOCADA", 2);
                    timeRemaining -= 10f;
                    if (timeRemaining < 0) timeRemaining = 0;

                    if (audioSource != null && penaltyClip != null)
                    {
                        audioSource.PlayOneShot(penaltyClip);
                    }
                }

                if (audioSource != null && throwTrashClip != null)
                {
                    audioSource.PlayOneShot(throwTrashClip);
                }

                Destroy(currentTrash.gameObject);
                currentTrash = null;

                // ✅ Restar del contador de basura
                totalTrashCount--;
                if (totalTrashCount <= 0)
                {
                    WinGame();
                }

                return;
            }
        }

        ShowMessage("Acá No -_-", 2);
    }

    void HandleFootsteps()
    {
        bool isMoving = moveInput.magnitude > 0.1f;

        if (isMoving)
        {
            if (!footstepsSource.isPlaying)
            {
                footstepsSource.clip = footstepClip;
                footstepsSource.loop = true;
                footstepsSource.Play();
            }
        }
        else
        {
            if (footstepsSource.isPlaying)
            {
                footstepsSource.Stop();
            }
        }
    }

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

    void HideMessage()
    {
        if (speechBubble != null) speechBubble.SetActive(false);
        if (infoText != null) infoText.text = "";
    }

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
        ShowMessage("¡Se acabó el tiempo!", 3);
        SceneManager.LoadScene("Death_Menu");
    }

    // ✅ NUEVO: Victoria
    void WinGame()
    {
        gameEnded = true;
        ShowMessage("🎉 ¡HAS GANADO!", 5);
        SceneManager.LoadScene("Win_Menu");
    }

    void LateUpdate()
    {
        if (speechBubble == null) return;
        if (!speechBubble.activeSelf) return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 targetWorldPos = transform.position + bubbleOffset;

        if (mainCam.orthographic)
        {
            float halfHeight = mainCam.orthographicSize;
            float halfWidth = halfHeight * mainCam.aspect;

            RectTransform rt = speechBubble.GetComponent<RectTransform>();
            Vector2 rectSize = rt.rect.size;
            Vector3 lossy = rt.lossyScale;

            float bubbleWorldHalfWidth = (rectSize.x * Mathf.Abs(lossy.x)) / 2f;
            float bubbleWorldHalfHeight = (rectSize.y * Mathf.Abs(lossy.y)) / 2f;

            Vector3 camPos = mainCam.transform.position;
            float minX = camPos.x - halfWidth + bubbleWorldHalfWidth;
            float maxX = camPos.x + halfWidth - bubbleWorldHalfWidth;
            float minY = camPos.y - halfHeight + bubbleWorldHalfHeight;
            float maxY = camPos.y + halfHeight - bubbleWorldHalfHeight;

            Vector3 clamped = targetWorldPos;
            clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
            clamped.y = Mathf.Clamp(clamped.y, minY, maxY);
            clamped.z = speechBubble.transform.position.z;

            speechBubble.transform.position = clamped;
        }
        else
        {
            Vector3 viewport = mainCam.WorldToViewportPoint(targetWorldPos);
            float padding = 0.05f;
            viewport.x = Mathf.Clamp(viewport.x, padding, 1f - padding);
            viewport.y = Mathf.Clamp(viewport.y, padding, 1f - padding);

            Vector3 speechWorld = speechBubble.transform.position;
            float desiredZ = mainCam.WorldToViewportPoint(speechWorld).z;
            viewport.z = desiredZ;

            Vector3 newWorld = mainCam.ViewportToWorldPoint(viewport);
            speechBubble.transform.position = new Vector3(newWorld.x, newWorld.y, speechBubble.transform.position.z);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
    }
}