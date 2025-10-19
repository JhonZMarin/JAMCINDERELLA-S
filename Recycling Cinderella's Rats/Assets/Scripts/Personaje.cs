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
    public GameObject interactionZone;  // 👉 El hijo con el Collider2D Trigger

    [Header("UI Mensajes")]
    public GameObject speechBubble;        // 👉 Imagen PNG de la burbuja
    public TextMeshProUGUI infoText;       // 👉 Texto dentro de la burbuja

    private TrashItem currentTrash = null;
    private TrashItem nearbyTrash = null;

    // ⏳ Control de mensajes
    private float messageTimer = 0f;
    private string currentMessage = "";

    [Header("Temporizador")]
    public TextMeshProUGUI timerText;
    private float timeRemaining = 300f; // 5 minutos
    private bool gameEnded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb != null) rb.freezeRotation = true;

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

    // 👉 Trigger de interacción con desechos
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

        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 1.5f); // pequeño rango para detectar caneca
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
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void EndGame()
    {
        gameEnded = true;
        ShowMessage(" ¡Se acabó el tiempo!", 10);
    }
}
