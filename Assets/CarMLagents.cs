using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using TMPro;

public class CarMLagents : Agent
{
    private Rigidbody rb;
    private float currentbreakForce;
    private bool isBreaking;
    private int currentCheckpointIndex = 0; // Checkpoint actual
    private List<Transform> checkpoints = new List<Transform>();
    private float lastDistanceToCheckpoint = 0f; // Guardar la distancia del frame anterior
    public RayPerceptionSensorComponent3D rayPerceptionSensor;

    // Settings
    [SerializeField] private float motorForce, breakForce, maxSteerAngle;
    [SerializeField] private WheelCollider frontLeftWheelCollider, frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider, rearRightWheelCollider;
    [SerializeField] private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform, rearRightWheelTransform;

    [SerializeField] public Transform checkpointsParent; // Asigna el padre de los checkpoints

    [SerializeField] private TextMeshProUGUI rewardText; // Arrástralo en el Inspector
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI lapTimeText;
    [SerializeField] private GameObject meta;

    private float episodeTimer = 0f; // Contador de tiempo en segundos
    private float lapStartTime; // Guarda cuándo comenzó la vuelta
    private float BueltaActual = 0f; // Tiempo de la última vuelta
    private float UltimaBuelta = float.MaxValue;
    private float mejorTiempoDeVuelta = 46;

    private const float maxEpisodeTime = 60; // Tiempo máximo antes de terminar el episodio
    private Vector3 startPosition;
    private Quaternion startRotation;
    private LapTimer ScriptTimer;

    public override void Initialize()
    {
        rayPerceptionSensor = GetComponent<RayPerceptionSensorComponent3D>();
        rb = GetComponent<Rigidbody>();

        // Bajar el centro de masa para mejorar estabilidad
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
        checkpoints.Clear();

        // Buscar el objeto "CheckPoints_Agente" dentro del mismo Agente
        Transform parentCheckpoints = transform.parent.Find("CheckPoints_" + transform.parent.name.Replace("(Clone)", ""));
        if (parentCheckpoints != null)
        {
            foreach (Transform checkpoint in parentCheckpoints)
            {
                checkpoints.Add(checkpoint);
            }
        }
        else
        {
            Debug.LogWarning("⚠ No se encontró el objeto de CheckPoints para: " + transform.parent.name);
        }

        // Ordenar los checkpoints numéricamente, dejando "Meta" al final
        checkpoints.Sort((a, b) =>
        {
            int numA = 50, numB = 50; // Usamos un número grande para la "Meta"
            if (System.Text.RegularExpressions.Regex.IsMatch(a.name, @"\d+"))
                numA = int.Parse(System.Text.RegularExpressions.Regex.Match(a.name, @"\d+").Value);
            if (System.Text.RegularExpressions.Regex.IsMatch(b.name, @"\d+"))
                numB = int.Parse(System.Text.RegularExpressions.Regex.Match(b.name, @"\d+").Value);
            return numA.CompareTo(numB);
        });

        int layer = LayerMask.NameToLayer("Agente");
        Physics.IgnoreLayerCollision(layer, layer, true);
        
        ScriptTimer = FindObjectOfType<LapTimer>();
    }

    public override void OnEpisodeBegin()
    {
        lapStartTime = Time.time;
        BueltaActual = 0;
        // Resetear velocidad y rotación del coche
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Restaurar la posición inicial
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;

        currentCheckpointIndex = 0; // Reiniciar el contador de checkpoints
        episodeTimer = 0f; // Reiniciar el tiempo del episodio

        // Reiniciar la variable de distancia para el reward de progreso
        lastDistanceToCheckpoint = 0f;

        // Reactivar todos los checkpoints
        foreach (Transform checkpoint in checkpoints)
        {
            checkpoint.gameObject.SetActive(true);
        }

        // Desactivar la meta al inicio
        if (meta != null)
        {
            meta.SetActive(false);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Observaciones de la velocidad del coche (3 observaciones)
        sensor.AddObservation(rb.velocity.magnitude);
        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.z);

        // Observación de la rotación
        sensor.AddObservation(transform.rotation.eulerAngles.y / 360f);

        // Información del próximo checkpoint (dirección y distancia)
        if (checkpoints.Count > 0)
        {
            Vector3 directionToCheckpoint = (checkpoints[currentCheckpointIndex].position - transform.position).normalized;
            sensor.AddObservation(directionToCheckpoint);
            float distanceToCheckpoint = Vector3.Distance(transform.position, checkpoints[currentCheckpointIndex].position);
            sensor.AddObservation(distanceToCheckpoint / 100f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float horizontalInput = actions.ContinuousActions[0];
        float verticalInput = actions.ContinuousActions[1];
        bool isBreaking = actions.ContinuousActions[2] > 0.5f;

        HandleMotor(verticalInput, isBreaking);
        HandleSteering(horizontalInput);
        UpdateWheels();

        float speed = rb.velocity.magnitude * 3.6f; // Convertir de m/s a km/h

        // Penalización constante por tiempo (reducida para no forzar la prisa excesiva)
        AddReward(-0.0005f);

        // Recompensa incremental por acercarse al siguiente checkpoint
        if (currentCheckpointIndex < checkpoints.Count)
        {
            float currentDistanceToCheckpoint = Vector3.Distance(transform.position, checkpoints[currentCheckpointIndex].position);

            // Si es el primer frame o se acaba de reiniciar (después de pasar un checkpoint)
            if (lastDistanceToCheckpoint == 0f)
            {
                lastDistanceToCheckpoint = currentDistanceToCheckpoint;
            }
            else
            {
                float progress = lastDistanceToCheckpoint - currentDistanceToCheckpoint;
                // Recompensa (o penalización) por el progreso hacia el checkpoint
                AddReward(0.02f * progress);
                lastDistanceToCheckpoint = currentDistanceToCheckpoint;
            }
            
            // Recompensa por la correcta alineación del vehículo hacia el checkpoint
            Vector3 directionToCheckpoint = (checkpoints[currentCheckpointIndex].position - transform.position).normalized;
            float alignment = Vector3.Dot(transform.forward, directionToCheckpoint);
            // Se suma si la alineación es buena y se penaliza ligeramente si es mala
            AddReward(0.005f * (alignment - 0.5f));
        }

        rewardText.text = $"Reward: {GetCumulativeReward():F2}";
        speedText.text = $"Speed: {Mathf.Round(speed)} km/h";

        // Incrementar el temporizador del episodio
        episodeTimer += Time.deltaTime;
        if (episodeTimer >= maxEpisodeTime)
        {
            AddReward(-1.0f); // Penalización por no completar el circuito a tiempo
            ScriptTimer.StopTimer();
            BueltaActual = ScriptTimer.GetLapTime();
            lapTimeText.text = "LastLap: " + BueltaActual;
            UltimaBuelta = BueltaActual;
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CheckPoint"))
        {
            if (checkpoints.Contains(other.transform))
            {       
                int checkpointIndex = checkpoints.IndexOf(other.transform);
                if (checkpointIndex == currentCheckpointIndex)
                {
                    AddReward(2.0f); // Recompensa por pasar el checkpoint correcto
                    other.gameObject.SetActive(false);

                    // Reiniciar la medida de distancia para el siguiente checkpoint
                    lastDistanceToCheckpoint = 0f;

                    // Activar la meta si es el primer checkpoint
                    if (currentCheckpointIndex == 0 && meta != null)
                    {
                        meta.SetActive(true);
                    }
                    currentCheckpointIndex++;
                }
                else
                {
                    AddReward(-5.0f); // Penalización por intentar saltarse checkpoints
                }
            }
        }
        if (other.CompareTag("Meta"))
        {
            if (currentCheckpointIndex >= checkpoints.Count - 2)
            {
                float lapTime = Time.time - lapStartTime;
                lapStartTime = Time.time;
                BueltaActual = lapTime;
                lapTimeText.text = "Last Lap: " + BueltaActual.ToString("F2");

                // Recompensa base por finalizar la vuelta
                AddReward(100.0f);

                // Bono extra si se mejora el mejor tiempo
                if (BueltaActual < mejorTiempoDeVuelta)
                {
                    mejorTiempoDeVuelta = BueltaActual;
                    AddReward(20f);
                }
                EndEpisode();
            }
        }
        if (other.CompareTag("Obstacle"))
        {
            AddReward(-10f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-10f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void HandleMotor(float verticalInput, bool isBreaking)
    {
        float forwardInput = Mathf.Max(0, verticalInput);
        frontLeftWheelCollider.motorTorque = forwardInput * motorForce;
        frontRightWheelCollider.motorTorque = forwardInput * motorForce;
        currentbreakForce = isBreaking ? breakForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        frontRightWheelCollider.brakeTorque = currentbreakForce;
        frontLeftWheelCollider.brakeTorque = currentbreakForce;
        rearLeftWheelCollider.brakeTorque = currentbreakForce;
        rearRightWheelCollider.brakeTorque = currentbreakForce;
    }

    private void HandleSteering(float horizontalInput)
    {
        float currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    private void OnDrawGizmos()
    {
        if (checkpoints.Count > 0 && currentCheckpointIndex < checkpoints.Count)
        {
            Vector3 nextCheckpointPos = checkpoints[currentCheckpointIndex].position;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, nextCheckpointPos);
        }
    }
}
