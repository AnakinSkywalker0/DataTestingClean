using System;
using UnityEngine;

public class NearestCarFinder : MonoBehaviour
{
    public float detectionRadius = 15f;
    public LayerMask carLayer;
    public Transform reference; // YOU (player/camera)

    void Update()
    {
        DetectCars();
    }

    string DetectCars()
    {
        Collider[] hits = Physics.OverlapSphere(
            reference.position,
            detectionRadius,
            carLayer
        );

        Transform closestCar = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Transform car = hit.transform;

            Vector3 dirToPlayer = (reference.position - car.position).normalized;
            float dot = Vector3.Dot(car.forward, dirToPlayer);

            if (dot <= 0)
                continue;

        
            Vector3 a = reference.position;
            Vector3 b = car.position;
            a.y = 0;
            b.y = 0;

            float distance = Vector3.Distance(a, b);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestCar = car;
            }
        }

        if (closestCar != null)
        {
            Vector3 pos = closestCar.position;

            string result = string.Format("{0},{1:F2},{2:F2},{3:F2},{4:F2}",
                closestCar.name,
                minDistance,
                pos.x,
                pos.y,
                pos.z
            );

            return result;
        }
        else
        {
            // ✅ Correct field count (5 values)
            return "None,0,0,0,0";
        }
    }

    void OnDrawGizmos()
    {
        if (reference == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(reference.position, detectionRadius);
    }
}